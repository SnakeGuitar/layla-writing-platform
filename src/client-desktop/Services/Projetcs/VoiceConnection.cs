using Microsoft.AspNetCore.SignalR.Client;
using NAudio.Wave;
using System.Threading.Channels;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace Layla.Desktop.Services.Projetcs;

public class VoiceConnection : IAsyncDisposable
{
    private const string BASE_URL = "https://localhost:5288";
    private const int SAMPLE_RATE = 16000;
    private const int CHANNELS = 1;
    private const int BITS_PER_SAMPLE = 16;
    private const int FRAME_DURATION_MS = 20;

    private readonly ILogger<VoiceConnection>? _logger = ServiceLocator.GetService<ILogger<VoiceConnection>>();
    private HubConnection? _hub;
    private WaveInEvent? _waveIn;
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _playbackBuffer;
    private Guid _currentProjectId;
    private bool _isSpeaking;
    // Drop-oldest channel decouples the 50 Hz mic callback from network RTT.
    // Without it, a transient network stall would queue unbounded async
    // hub invocations and blow the heap.
    private Channel<byte[]>? _sendChannel;
    private CancellationTokenSource? _sendCts;
    private Task? _sendPumpTask;

    public event Action<string, string, bool, string>? ParticipantJoined;       // userId, displayName, isSpeaking, role
    public event Action<string>? ParticipantLeft;                               // userId
    public event Action<string, string>? SpeakerStarted;                        // userId, displayName
    public event Action<string>? SpeakerStopped;                                // userId
    public event Action<List<ParticipantInfo>>? RoomStateReceived;              //
    public event Action<string>? ConnectionStateChanged;                        // "Connected", "Disconnected", "Reconnecting"

    public bool IsConnected => _hub?.State == HubConnectionState.Connected;

    public async Task ConnectAsync()
    {
        if (_hub != null) return;

        string? token = SessionManager.CurrentToken;
        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("Not authenticated.");

        _hub = new HubConnectionBuilder()
            .WithUrl($"{BASE_URL}/hubs/voice", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();

        _hub.Closed += _ =>
        {
            ConnectionStateChanged?.Invoke("Disconnected");
            return Task.CompletedTask;
        };
        _hub.Reconnecting += _ =>
        {
            ConnectionStateChanged?.Invoke("Reconnecting");
            return Task.CompletedTask;
        };
        _hub.Reconnected += _ =>
        {
            ConnectionStateChanged?.Invoke("Connected");
            return Task.CompletedTask;
        };

        await _hub.StartAsync();
        ConnectionStateChanged?.Invoke("Connected");

        InitializeAudioPlayback();
    }

    public async Task JoinRoomAsync(Guid projectId)
    {
        if (_hub == null || _hub.State != HubConnectionState.Connected)
            throw new InvalidOperationException("Not connected to voice server.");

        _currentProjectId = projectId;
        await _hub.InvokeAsync("JoinRoom", projectId);
    }

    public async Task LeaveRoomAsync()
    {
        if (_hub == null || _hub.State != HubConnectionState.Connected) return;

        if (_isSpeaking)
            await StopSpeakingAsync();

        await _hub.InvokeAsync("LeaveRoom", _currentProjectId);
        StopAudioCapture();
    }

    public async Task StartSpeakingAsync()
    {
        if (_hub == null || _isSpeaking) return;

        _isSpeaking = true;
        await _hub.InvokeAsync("StartSpeaking", _currentProjectId);
        StartAudioCapture();
    }

    public async Task StopSpeakingAsync()
    {
        if (_hub == null || !_isSpeaking) return;

        _isSpeaking = false;
        StopAudioCapture();
        await _hub.InvokeAsync("StopSpeaking", _currentProjectId);
    }

    private void RegisterHandlers()
    {
        if (_hub == null) return;

        _hub.On<ParticipantInfo>("UserJoined", participant =>
        {
            Application.Current.Dispatcher.Invoke(() =>
                ParticipantJoined?.Invoke(participant.UserId, participant.DisplayName, participant.IsSpeaking, participant.Role));
        });

        _hub.On<string>("UserLeft", userId =>
        {
            Application.Current.Dispatcher.Invoke(() =>
                ParticipantLeft?.Invoke(userId));
        });

        _hub.On<string, string>("UserStartedSpeaking", (userId, displayName) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
                SpeakerStarted?.Invoke(userId, displayName));
        });

        _hub.On<string>("UserStoppedSpeaking", userId =>
        {
            Application.Current.Dispatcher.Invoke(() =>
                SpeakerStopped?.Invoke(userId));
        });

        _hub.On<string, byte[]>("ReceiveAudio", (senderId, audioData) =>
        {
            _playbackBuffer?.AddSamples(audioData, 0, audioData.Length);
        });

        _hub.On<RoomState>("RoomState", state =>
        {
            Application.Current.Dispatcher.Invoke(() =>
                RoomStateReceived?.Invoke(state.Participants));
        });
    }

    private void InitializeAudioPlayback()
    {
        WaveFormat? format = new(SAMPLE_RATE, BITS_PER_SAMPLE, CHANNELS);
        _playbackBuffer = new BufferedWaveProvider(format)
        {
            BufferDuration = TimeSpan.FromSeconds(2),
            DiscardOnBufferOverflow = true
        };
        _waveOut = new();
        _waveOut.Init(_playbackBuffer);
        _waveOut.Play();
    }

    private void StartAudioCapture()
    {
        if (_waveIn != null) return;

        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(SAMPLE_RATE, BITS_PER_SAMPLE, CHANNELS),
            BufferMilliseconds = FRAME_DURATION_MS
        };

        _sendChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(8)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true,
        });
        _sendCts = new CancellationTokenSource();
        _sendPumpTask = Task.Run(() => PumpSendChannelAsync(_sendChannel.Reader, _sendCts.Token));
        _waveIn.DataAvailable += OnWaveInDataAvailable;
        _waveIn.StartRecording();
    }

    private void OnWaveInDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (!_isSpeaking || _hub == null || _hub.State != HubConnectionState.Connected)
            return;

        var buffer = new byte[e.BytesRecorded];
        Array.Copy(e.Buffer, buffer, e.BytesRecorded);
        // TryWrite is fire-and-forget — drop-oldest semantics handle overflow.
        _sendChannel?.Writer.TryWrite(buffer);
    }

    private async Task PumpSendChannelAsync(ChannelReader<byte[]> reader, CancellationToken ct)
    {
        try
        {
            await foreach (var frame in reader.ReadAllAsync(ct))
            {
                var hub = _hub;
                if (hub == null || hub.State != HubConnectionState.Connected) continue;
                try
                {
                    await hub.InvokeAsync("SendAudio", _currentProjectId, frame, ct);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Audio send failed");
                }
            }
        }
        catch (OperationCanceledException) { /* shutdown */ }
    }

    private void StopAudioCapture()
    {
        if (_waveIn != null)
        {
            _waveIn.DataAvailable -= OnWaveInDataAvailable;
            _waveIn.StopRecording();
            _waveIn.Dispose();
            _waveIn = null;
        }
        _sendChannel?.Writer.TryComplete();
        _sendCts?.Cancel();
        try { _sendPumpTask?.Wait(TimeSpan.FromMilliseconds(200)); } catch { }
        _sendCts?.Dispose();
        _sendCts = null;
        _sendChannel = null;
        _sendPumpTask = null;
    }

    public async ValueTask DisposeAsync()
    {
        // Order matters: dispose the SignalR hub FIRST so its callbacks
        // (ReceiveAudio in particular) stop firing before we null out the
        // playback buffer they touch. Reversing this risks a race where
        // ReceiveAudio runs against a half-disposed BufferedWaveProvider.
        StopAudioCapture();

        if (_hub != null)
        {
            try { await _hub.DisposeAsync(); } catch { }
            _hub = null;
        }

        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
        _playbackBuffer = null;
    }
}

public record ParticipantInfo(string UserId, string DisplayName, bool IsSpeaking, string Role, DateTime JoinedAt);
public record RoomState(Guid ProjectId, List<ParticipantInfo> Participants);
