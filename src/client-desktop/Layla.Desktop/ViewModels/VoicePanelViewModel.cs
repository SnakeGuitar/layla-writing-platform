using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Models;
using Layla.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Layla.Desktop.ViewModels
{
    public partial class VoicePanelViewModel : ObservableObject
    {
        private Guid _projectId;
        private VoiceConnection? _voice;

        [ObservableProperty]
        private ObservableCollection<VoiceParticipantViewModel> _participants = new();

        [ObservableProperty]
        private string _statusText = "Disconnected";

        [ObservableProperty]
        private Brush _statusIndicatorFill = Brushes.Gray;

        [ObservableProperty]
        private bool _isConnectVisible = true;

        [ObservableProperty]
        private bool _isLeaveVisible = false;

        [ObservableProperty]
        private bool _isPttEnabled = false;

        [ObservableProperty]
        private string _pttStatusText = string.Empty;

        [ObservableProperty]
        private bool _isConnecting;

        [ObservableProperty]
        private bool _isEmptyRoomVisible = true;

        [ObservableProperty]
        private string _connectButtonContent = "Connect";

        public VoicePanelViewModel()
        {
        }

        [ObservableProperty]
        private ObservableCollection<ParticipantPresence> _onlineParticipants = new();

        [ObservableProperty]
        private bool _isOnlineParticipantsVisible = false;

        public void Initialize(Guid projectId)
        {
            _projectId = projectId;
            _ = StartWatchingPresenceAsync();
        }

        private async Task StartWatchingPresenceAsync()
        {
            try
            {
                var api = ServiceLocator.GetService<IProjectApiService>();
                if (api == null) return;

                api.ParticipantsUpdated += OnParticipantsUpdated;

                await api.ConnectPresenceHubAsync();
                await api.WatchProjectAsync(_projectId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start watching presence: {ex.Message}");
            }
        }

        private void OnParticipantsUpdated(Guid projectId, IEnumerable<ParticipantPresence> participants)
        {
            if (projectId == _projectId)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    OnlineParticipants.Clear();
                    foreach (var p in participants)
                        OnlineParticipants.Add(p);
                    IsOnlineParticipantsVisible = OnlineParticipants.Count > 0;
                });
            }
        }

        [RelayCommand]
        private async Task ConnectAsync()
        {
            try
            {
                IsConnecting = true;
                ConnectButtonContent = "Connecting...";

                _voice = new VoiceConnection();
                WireEvents();

                await _voice.ConnectAsync();
                await _voice.JoinRoomAsync(_projectId);

                IsConnectVisible = false;
                IsLeaveVisible = true;
                IsPttEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect: {ex.Message}", "Voice Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ConnectButtonContent = "Connect";
            }
            finally
            {
                IsConnecting = false;
            }
        }

        [RelayCommand]
        private async Task LeaveAsync()
        {
            try
            {
                if (_voice != null)
                    await _voice.LeaveRoomAsync();

                await CleanupAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error leaving room: {ex.Message}", "Voice Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private async Task StartSpeakingAsync()
        {
            if (_voice == null || !_voice.IsConnected) return;

            var localUser = Participants.FirstOrDefault(p => p.UserId == Services.SessionManager.CurrentUserId);
            if (localUser?.Role == "Reader")
            {
                PttStatusText = "Listen only mode";
                return;
            }

            try
            {
                await _voice.StartSpeakingAsync();
                PttStatusText = "Transmitting...";
            }
            catch (Exception)
            {
                PttStatusText = "Cannot transmit";
            }
        }

        [RelayCommand]
        private async Task StopSpeakingAsync()
        {
            if (_voice == null || !_voice.IsConnected) return;

            try
            {
                await _voice.StopSpeakingAsync();
                PttStatusText = string.Empty;
            }
            catch { }
        }

        private void WireEvents()
        {
            if (_voice == null) return;

            _voice.ConnectionStateChanged += state =>
            {
                StatusText = state;
                StatusIndicatorFill = state switch
                {
                    "Connected" => Brushes.LimeGreen,
                    "Reconnecting" => Brushes.Orange,
                    _ => Brushes.Gray
                };
            };

            _voice.RoomStateReceived += participants =>
            {
                Participants.Clear();
                foreach (var p in participants)
                    Participants.Add(new VoiceParticipantViewModel(p.UserId, p.DisplayName, p.IsSpeaking, p.Role));

                IsEmptyRoomVisible = Participants.Count == 0;
            };

            _voice.ParticipantJoined += (userId, displayName, isSpeaking, role) =>
            {
                if (Participants.All(p => p.UserId != userId))
                    Participants.Add(new VoiceParticipantViewModel(userId, displayName, isSpeaking, role));

                IsEmptyRoomVisible = false;
            };

            _voice.ParticipantLeft += userId =>
            {
                var p = Participants.FirstOrDefault(x => x.UserId == userId);
                if (p != null) Participants.Remove(p);

                IsEmptyRoomVisible = Participants.Count == 0;
            };

            _voice.SpeakerStarted += (userId, _) =>
            {
                var p = Participants.FirstOrDefault(x => x.UserId == userId);
                if (p != null) p.IsSpeaking = true;
            };

            _voice.SpeakerStopped += userId =>
            {
                var p = Participants.FirstOrDefault(x => x.UserId == userId);
                if (p != null) p.IsSpeaking = false;
            };
        }

        private async Task CleanupAsync()
        {
            IsPttEnabled = false;
            IsLeaveVisible = false;
            IsConnectVisible = true;
            ConnectButtonContent = "Connect";
            Participants.Clear();
            IsEmptyRoomVisible = true;
            StatusText = "Disconnected";
            StatusIndicatorFill = Brushes.Gray;
            PttStatusText = string.Empty;

            if (_voice != null)
            {
                await _voice.DisposeAsync();
                _voice = null;
            }
        }
    }
}
