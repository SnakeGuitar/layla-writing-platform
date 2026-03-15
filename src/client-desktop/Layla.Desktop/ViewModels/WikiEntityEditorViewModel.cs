using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace Layla.Desktop.ViewModels
{
    public partial class WikiEntityEditorViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _aliases = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _tags = string.Empty;

        public WikiEntityEditorViewModel()
        {
        }

        [RelayCommand]
        private void Save()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show("The Name field is required to save an entity.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Logic to save (currently local mock as per original code)
            MessageBox.Show($"Wiki Entity '{Name}' saved successfully (Local Mock)!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            
            ClearFields();
        }

        [RelayCommand]
        private void Cancel()
        {
            ClearFields();
        }

        private void ClearFields()
        {
            Name = string.Empty;
            Aliases = string.Empty;
            Description = string.Empty;
            Tags = string.Empty;
        }
    }
}
