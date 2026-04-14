using System.Net.Http;
using Microsoft.AspNetCore.SignalR.Client;
using NAudio.Wave;
using System.Windows;

namespace Layla.Desktop.Services;

public class VoiceConnection : IAsyncDisposable
{
    private const string BaseUrl = "https://localhost:5288";
    private const int SampleRate = 16000;
    private const int Channels = 1;
    private const int BitsPerSample = 16;
    private const int FrameDurationMs = 20;

    private HubConnection? _hub;
    private WaveInEvent? _waveIn;
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _playbackBuffer;
    private Guid _currentProjectId;
    private bool _isSpeaking;

    public event Action<string, string, bool, string>? ParticipantJoined;   // userId, displayName, isSpeaking, role
    public event Action<string>? ParticipantLeft;                            // userId
    public event Action<string, string>? SpeakerStarted;                    // userId, displayName
    public event Action<string>? SpeakerStopped;                            // userId
    public event Action<List<ParticipantInfo>>? RoomStateReceived;
    public event Action<string>? ConnectionStateChanged;                     // "Connected", "Disconnected", "Reconnecting"

    public bool IsConnected => _hub?.State == HubConnectionState.Connected;

    public async Task ConnectAsync()
    {
        if (_hub != null) return;

        var token = SessionManager.CurrentToken;
        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("Not authenticated.");

        _hub = new HubConnectionBuilder()
            .WithUrl($"{BaseUrl}/hubs/voice", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                options.HttpMessageHandlerFactory = handler =>
                {
                    if (handler is HttpClientHandler clientHandler)
                        clientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                    return handler;
                };
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
        var format = new WaveFormat(SampleRate, BitsPerSample, Channels);
        _playbackBuffer = new BufferedWaveProvider(format)
        {
            BufferDuration = TimeSpan.FromSeconds(2),
            DiscardOnBufferOverflow = true
        };
        _waveOut = new WaveOutEvent();
        _waveOut.Init(_playbackBuffer);
        _waveOut.Play();
    }

    private void StartAudioCapture()
    {
        if (_waveIn != null) return;

        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(SampleRate, BitsPerSample, Channels),
            BufferMilliseconds = FrameDurationMs
        };

        _waveIn.DataAvailable += async (sender, e) =>
        {
            if (!_isSpeaking || _hub == null || _hub.State != HubConnectionState.Connected)
                return;

            try
            {
                var buffer = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, buffer, e.BytesRecorded);
                await _hub.InvokeAsync("SendAudio", _currentProjectId, buffer);
            }
            catch
            {
                // Ignore transient send failures during active speech
            }
        };

        _waveIn.StartRecording();
    }

    private void StopAudioCapture()
    {
        _waveIn?.StopRecording();
        _waveIn?.Dispose();
        _waveIn = null;
    }

    public async ValueTask DisposeAsync()
    {
        StopAudioCapture();
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
        _playbackBuffer = null;

        if (_hub != null)
        {
            await _hub.DisposeAsync();
            _hub = null;
        }
    }
}

public record ParticipantInfo(string UserId, string DisplayName, bool IsSpeaking, string Role, DateTime JoinedAt);
public record RoomState(Guid ProjectId, List<ParticipantInfo> Participants);
