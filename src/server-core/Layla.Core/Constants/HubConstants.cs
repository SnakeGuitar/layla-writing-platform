namespace Layla.Core.Constants;

/// <summary>
/// SignalR hub method name constants. Keep in sync with client-side hub listeners.
/// </summary>
public static class HubConstants
{
    public static class Voice
    {
        public const string RoomState = "RoomState";
        public const string UserJoined = "UserJoined";
        public const string UserLeft = "UserLeft";
        public const string UserStartedSpeaking = "UserStartedSpeaking";
        public const string UserStoppedSpeaking = "UserStoppedSpeaking";
        public const string ReceiveAudio = "ReceiveAudio";

        public const string ParticipantRole = "Speaker";
    }

    public static class Presence
    {
        public const string ParticipantsUpdated = "ParticipantsUpdated";
        public const string AuthorStatusChanged = "AuthorStatusChanged";
        public const string MultipleSessionsDetected = "MultipleSessionsDetected";

        public const string RoleWatcher = "Watcher";
        public const string RoleAuthor = "Author";
    }
}
