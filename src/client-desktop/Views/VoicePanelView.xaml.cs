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
        private bool _spaceHeld;

        public VoicePanelView(Guid projectId)
        {
            InitializeComponent();
            _viewModel = ServiceLocator.GetService<VoicePanelViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
            DataContext = _viewModel;
            _viewModel.Initialize(projectId);

            // Keyboard PTT: hold Space to talk
            this.Focusable = true;
            this.PreviewKeyDown += OnKeyDown;
            this.PreviewKeyUp += OnKeyUp;
            this.Loaded += (_, _) => this.Focus();
        }

        private async void PttButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await _viewModel.StartSpeakingCommand.ExecuteAsync(null);
        }

        private async void PttButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            await _viewModel.StopSpeakingCommand.ExecuteAsync(null);
        }

        private async void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && !_spaceHeld && _viewModel.IsPttEnabled)
            {
                _spaceHeld = true;
                e.Handled = true;
                await _viewModel.StartSpeakingCommand.ExecuteAsync(null);
            }
        }

        private async void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && _spaceHeld)
            {
                _spaceHeld = false;
                e.Handled = true;
                await _viewModel.StopSpeakingCommand.ExecuteAsync(null);
            }
        }
    }
}
