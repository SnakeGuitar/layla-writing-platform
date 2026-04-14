using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Models;
using Layla.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Layla.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel for the wiki entity manager.
    /// Manages the full list of entities for a project, provides CRUD operations,
    /// and displays the narrative arc (chapters where an entity appears).
    /// </summary>
    public partial class WikiEntityEditorViewModel : ObservableObject
    {
        private readonly IWikiApiService _wikiApi;
        private Guid _projectId;

        /// <summary><c>true</c> while loading entries from the API.</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>Name field in the editor form.</summary>
        [ObservableProperty]
        private string _name = string.Empty;

        /// <summary>Entity type selected in the editor form.</summary>
        [ObservableProperty]
        private string _entityType = "Character";

        /// <summary>Description field in the editor form.</summary>
        [ObservableProperty]
        private string _description = string.Empty;

        /// <summary>Comma-separated tags in the editor form.</summary>
        [ObservableProperty]
        private string _tags = string.Empty;

        /// <summary>Currently selected entry from the list (null = creating new).</summary>
        [ObservableProperty]
        private WikiEntry? _selectedEntry;

        /// <summary>All wiki entries for the project.</summary>
        public ObservableCollection<WikiEntry> Entries { get; } = new();

        /// <summary>Chapters where <see cref="SelectedEntry"/> appears.</summary>
        public ObservableCollection<AppearanceRecord> Appearances { get; } = new();

        /// <summary>Available entity types for the ComboBox.</summary>
        public string[] EntityTypes { get; } = { "Character", "Location", "Event", "Object" };

        public WikiEntityEditorViewModel(IWikiApiService wikiApi)
        {
            _wikiApi = wikiApi;
        }

        /// <summary>Sets the project context.</summary>
        public void Initialize(Guid projectId)
        {
            _projectId = projectId;
        }

        /// <summary>Fetches all wiki entries for the project.</summary>
        [RelayCommand]
        public async Task LoadEntriesAsync()
        {
            IsLoading = true;
            try
            {
                var entries = await _wikiApi.GetEntriesAsync(_projectId);
                Entries.Clear();
                if (entries != null)
                {
                    foreach (var e in entries.OrderBy(e => e.EntityType).ThenBy(e => e.Name))
                        Entries.Add(e);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>Selects an entry and loads its data into the form and its appearances.</summary>
        [RelayCommand]
        public async Task SelectEntryAsync(WikiEntry? entry)
        {
            SelectedEntry = entry;
            Appearances.Clear();

            if (entry == null)
            {
                ClearForm();
                return;
            }

            var full = await _wikiApi.GetEntryAsync(_projectId, entry.EntityId);
            if (full != null)
            {
                Name = full.Name;
                EntityType = full.EntityType;
                Description = full.Description;
                Tags = string.Join(", ", full.Tags);
            }

            var appearances = await _wikiApi.GetEntityAppearancesAsync(_projectId, entry.EntityId);
            if (appearances != null)
            {
                foreach (var a in appearances)
                    Appearances.Add(a);
            }
        }

        /// <summary>Creates a new entry or updates the selected one.</summary>
        [RelayCommand]
        public async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Name)) return;

            var tagList = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                              .ToList();

            if (SelectedEntry != null)
            {
                var updated = await _wikiApi.UpdateEntryAsync(
                    _projectId, SelectedEntry.EntityId, Name, EntityType, Description, tagList);
                if (updated != null)
                {
                    var index = Entries.IndexOf(SelectedEntry);
                    if (index >= 0) Entries[index] = updated;
                    SelectedEntry = updated;
                }
            }
            else
            {
                var created = await _wikiApi.CreateEntryAsync(
                    _projectId, Name, EntityType, Description, tagList);
                if (created != null)
                {
                    Entries.Add(created);
                    SelectedEntry = created;
                }
            }
        }

        /// <summary>Deletes the selected entry.</summary>
        [RelayCommand]
        public async Task DeleteAsync()
        {
            if (SelectedEntry == null) return;

            var deleted = await _wikiApi.DeleteEntryAsync(_projectId, SelectedEntry.EntityId);
            if (deleted)
            {
                Entries.Remove(SelectedEntry);
                SelectedEntry = null;
                ClearForm();
                Appearances.Clear();
            }
        }

        /// <summary>Clears the form for creating a new entry.</summary>
        [RelayCommand]
        public void NewEntry()
        {
            SelectedEntry = null;
            ClearForm();
            Appearances.Clear();
        }

        private void ClearForm()
        {
            Name = string.Empty;
            EntityType = "Character";
            Description = string.Empty;
            Tags = string.Empty;
        }
    }
}
