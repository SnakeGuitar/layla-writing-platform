using client_web.Services.Voice.SignalR;

namespace client_web.Services;

public record VoiceParticipant(string UserId, string DisplayName, bool IsSpeaking, string Role);
public record VoiceRoomState(Guid ProjectId, List<VoiceParticipant> Participants);

public class VoiceService : IVoiceService
{
    private readonly ISignalRClient _client;
    private readonly string _voiceHubBaseUrl;
    private bool _handlersRegistered;

    public VoiceService(ISignalRClient client, IConfiguration configuration)
    {
        _client = client;
        _voiceHubBaseUrl = configuration["ApiUrls:BackendURL"]!;
        _client.OnConnectionChanged += state => ConnectionChanged?.Invoke(this, state);
    }

    private void RegisterHandlers()
    {
        if (_handlersRegistered) return;

        _client.On<VoiceRoomState>("RoomState", state =>
            RoomStateChanged?.Invoke(this, state.Participants));

        _client.On<VoiceParticipant>("UserJoined", participant =>
            UserJoined?.Invoke(this, participant));

        _client.On<string>("UserLeft", userId =>
            UserLeft?.Invoke(this, userId));

        _client.On<string, string>("UserStartedSpeaking", (userId, displayName) =>
            SpeakerStarted?.Invoke(this, (userId, displayName)));

        _client.On<string>("UserStoppedSpeaking", userId =>
            SpeakerStopped?.Invoke(this, userId));

        _client.On<string, byte[]>("ReceiveAudio", (senderId, audioData) =>
            AudioReceived?.Invoke(this, (senderId, audioData)));

        _handlersRegistered = true;
    }

    private async Task InvokeSafeAsync(string method, params object[] args)
    {
        if (!IsConnected) return;

        try
        {
            await _client.InvokeAsync(method, args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error calling {method}: {ex.Message}");
        }
    }

    // IVoiceConnectionService ----------------------------------------------------------
    public bool IsConnected => _client.IsConnected;
    public event EventHandler<string>? ConnectionChanged;

    public Task ConnectAsync(string token) =>
        _client.ConnectAsync($"{_voiceHubBaseUrl}/hubs/voice", token);

    public Task DisconnectAsync() => _client.DisconnectAsync();

    public async ValueTask DisposeAsync() => await _client.DisposeAsync();

    // IVoiceRoomService -----------------------------------------------------------
    public event EventHandler<List<VoiceParticipant>>? RoomStateChanged;
    public event EventHandler<VoiceParticipant>? UserJoined;
    public event EventHandler<string>? UserLeft;
    public event EventHandler<(string userId, string displayName)>? SpeakerStarted;
    public event EventHandler<string>? SpeakerStopped;

    public async Task JoinRoomAsync(Guid projectId)
    {
        RegisterHandlers();
        await InvokeSafeAsync("JoinRoom", projectId);
    }

    public async Task LeaveRoomAsync(Guid projectId) =>
        await InvokeSafeAsync("LeaveRoom", projectId);

    // IVoiceAudioService -----------------------------------------------------------
    public event EventHandler<(string senderId, byte[] audio)>? AudioReceived;

    public async Task StartSpeakingAsync(Guid projectId) =>
        await InvokeSafeAsync("StartSpeaking", projectId);

    public async Task StopSpeakingAsync(Guid projectId) =>
        await InvokeSafeAsync("StopSpeaking", projectId);

    public async Task SendAudioAsync(Guid projectId, byte[] audioData) =>
        await InvokeSafeAsync("SendAudio", projectId, audioData);
}
