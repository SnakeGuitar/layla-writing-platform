using client_web.Application.Config.SignalR;
using client_web.Helpers;
using Microsoft.AspNetCore.SignalR.Client;

namespace client_web.Application.Services.Voice;

public record VoiceParticipant(string UserId, string DisplayName, bool IsSpeaking, string Role);
public record VoiceRoomState(Guid ProjectId, List<VoiceParticipant> Participants);

public class VoiceService : IVoiceService
{
    private readonly ISignalRClient _client;
    private readonly string _baseUrl;
    private readonly ILogger<VoiceService> _logger;
    private Guid? _joinedProjectId;

    public VoiceService(ISignalRClient client, IConfiguration configuration, ILogger<VoiceService> logger)
    {
        _client = client;
        _client.OnConnectionChanged += (sender, state) => Notify(state);
        _baseUrl = configuration["ApiUrls:SignalRHubURL:VoiceServiceHub"]!;
        _logger = logger;
    }

    private bool _handlersRegistered;

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

    // ISignalRClient -------------------------------------------------------------------
    public HubConnectionState State => _client.Hub?.State ?? HubConnectionState.Disconnected;

    public bool IsConnected => _client.IsConnected;

    private void Notify(HubConnectionState state)
    {
        OnConnectionChanged?.Invoke(this, state);
    }

    private async Task InvokeSafeAsync(Enum method, params object[] args)
    {
        try
        {
            string methodName = FormatData.EnumToMethodName(method);
            await _client.InvokeSafeAsync(methodName, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling {Method}", method);
            throw;
        }
    }

    private static void ThrowIfEmptyProjectId(Guid projectId)
    {
        if (projectId == Guid.Empty)
            throw new InvalidOperationException("A valid project ID is required to use the voice room.");
    }

    private void ThrowIfNotInRoom(Guid projectId)
    {
        if (_joinedProjectId != projectId)
            throw new InvalidOperationException("Join the voice room before sending voice events.");
    }

    // IVoiceConnectionService ----------------------------------------------------------
    public event EventHandler<HubConnectionState>? OnConnectionChanged;

    public async Task ConnectAsync(string token)
    {
        await _client.ConnectAsync(_baseUrl, token);
        RegisterHandlers();
    }

    public async Task DisconnectAsync()
    {
        _joinedProjectId = null;
        await _client.DisconnectAsync();
        _handlersRegistered = false;
    }

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
        ThrowIfEmptyProjectId(projectId);
        await InvokeSafeAsync(RoomAccessState.JoinRoom, projectId);
        _joinedProjectId = projectId;
    }

    public async Task LeaveRoomAsync(Guid projectId)
    {
        ThrowIfEmptyProjectId(projectId);
        if (_joinedProjectId != projectId)
            return;

        await InvokeSafeAsync(RoomAccessState.LeaveRoom, projectId);
        _joinedProjectId = null;
    }

    // IVoiceAudioService -----------------------------------------------------------
    public event EventHandler<(string senderId, byte[] audio)>? OnAudioReceived;

    public async Task StartSpeakingAsync(Guid projectId)
    {
        ThrowIfEmptyProjectId(projectId);
        ThrowIfNotInRoom(projectId);
        await InvokeSafeAsync(AudioState.StartSpeaking, projectId);
    }

    public async Task StopSpeakingAsync(Guid projectId)
    {
        ThrowIfEmptyProjectId(projectId);
        ThrowIfNotInRoom(projectId);
        await InvokeSafeAsync(AudioState.StopSpeaking, projectId);
    }

    public async Task SendAudioAsync(Guid projectId, byte[] audioData)
    {
        ThrowIfEmptyProjectId(projectId);
        ThrowIfNotInRoom(projectId);
        await InvokeSafeAsync(AudioState.SendAudio, projectId, audioData);
    }
}
