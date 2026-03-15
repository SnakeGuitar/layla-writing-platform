using CommunityToolkit.Mvvm.ComponentModel;

namespace Layla.Desktop.ViewModels
{
    public partial class VoiceParticipantViewModel : ObservableObject
    {
        public string UserId { get; }
        public string DisplayName { get; }
        public string Role { get; }
        public string RoleLabel => Role == "Reader" ? "(Listener)" : "";

        [ObservableProperty]
        private bool _isSpeaking;

        public VoiceParticipantViewModel(string userId, string displayName, bool isSpeaking, string role)
        {
            UserId = userId;
            DisplayName = displayName;
            IsSpeaking = isSpeaking;
            Role = role;
        }
    }
}
