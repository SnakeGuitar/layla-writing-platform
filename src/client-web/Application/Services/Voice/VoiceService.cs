using System.Data;
using client_web.Application.Config.SignalR;
using client_web.Helpers;

namespace client_web.Application.Services.Voice;

public record VoiceParticipant(string UserId, string DisplayName, bool IsSpeaking, string Role);
public record VoiceRoomState(Guid ProjectId, List<VoiceParticipant> Participants);

public class VoiceService : IVoiceService
{
    private readonly ISignalRClient _client;
    private readonly string _baseUrl;
    private bool _handlersRegistered;

    public VoiceService(ISignalRClient client, IConfiguration configuration)
    {
        _client = client;
        _baseUrl = configuration["ApiUrls:BackendURL"]!;
        _client.OnConnectionChanged += (sender, state) =>
            OnConnectionChanged?.Invoke(this, state);
    }

    private void RegisterHandlers()
    {
        if (_handlersRegistered) return;

        _client.On<VoiceRoomState>("RoomState", state =>
            OnRoomStateChanged?.Invoke(this, state.Participants));

        _client.On<VoiceParticipant>("UserJoined", participant =>
            OnUserJoined?.Invoke(this, participant));

        _client.On<string>("UserLeft", userId =>
            OnUserLeft?.Invoke(this, userId));

        _client.On<string, string>("UserStartedSpeaking", (userId, displayName) =>
            OnSpeakerStarted?.Invoke(this, (userId, displayName)));

        _client.On<string>("UserStoppedSpeaking", userId =>
            OnSpeakerStopped?.Invoke(this, userId));

        _client.On<string, byte[]>("ReceiveAudio", (senderId, audioData) =>
            OnAudioReceived?.Invoke(this, (senderId, audioData)));

        _handlersRegistered = true;
    }

    private async Task InvokeSafeAsync(Enum method, params object[] args)
    {
        string methodName = FormatData.EnumToMethodName(method);
        await _client.InvokeSafeAsync(methodName, args).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Console.Error.WriteLine($"Error calling {method}: {task.Exception?.GetBaseException().Message}");
            }
        });
    }

    // IVoiceConnectionService ----------------------------------------------------------
    public bool IsConnected => _client.IsConnected;
    public event EventHandler<ConnectionState>? OnConnectionChanged;

    public Task ConnectAsync(string token) =>
        _client.ConnectAsync($"{_baseUrl}/hubs/voice", token);

    public Task DisconnectAsync() =>
        _client.DisconnectAsync();

    public async ValueTask DisposeAsync() =>
        await _client.DisposeAsync();

    // IVoiceRoomService -----------------------------------------------------------
    public event EventHandler<List<VoiceParticipant>>? OnRoomStateChanged;
    public event EventHandler<VoiceParticipant>? OnUserJoined;
    public event EventHandler<string>? OnUserLeft;
    public event EventHandler<(string userId, string displayName)>? OnSpeakerStarted;
    public event EventHandler<string>? OnSpeakerStopped;

    public async Task JoinRoomAsync(Guid projectId)
    {
        await InvokeSafeAsync(RoomAccessState.JoinRoom, projectId);
    }

    public async Task LeaveRoomAsync(Guid projectId) =>
        await InvokeSafeAsync(RoomAccessState.LeaveRoom, projectId);

    // IVoiceAudioService -----------------------------------------------------------
    public event EventHandler<(string senderId, byte[] audio)>? OnAudioReceived;

    public async Task StartSpeakingAsync(Guid projectId) =>
        await InvokeSafeAsync(AudioState.StartSpeaking, projectId);

    public async Task StopSpeakingAsync(Guid projectId) =>
        await InvokeSafeAsync(AudioState.StopSpeaking, projectId);

    public async Task SendAudioAsync(Guid projectId, byte[] audioData) =>
        await InvokeSafeAsync(AudioState.SendingAudio, projectId, audioData);
}
