using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Layla.Desktop.ViewModels;
using Layla.Desktop.Services;

namespace Layla.Desktop.Views
{
    public partial class VoicePanelView : Page
    {
        private readonly VoicePanelViewModel _viewModel;

        public VoicePanelView(Guid projectId)
        {
            InitializeComponent();
            _viewModel = ServiceLocator.GetService<VoicePanelViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
            DataContext = _viewModel;
            _viewModel.Initialize(projectId);
        }

        private async void PttButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await _viewModel.StartSpeakingCommand.ExecuteAsync(null);
        }

        private async void PttButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            await _viewModel.StopSpeakingCommand.ExecuteAsync(null);
        }
    }
}
