package com.layla.android.ui.workspace

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.layla.android.data.api.WikiApiService
import com.layla.android.data.model.*
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

// ─── State ────────────────────────────────────────────────────────────────────

data class WikiUiState(
    val entries: List<WikiEntryDto> = emptyList(),
    val isLoading: Boolean = false,
    val selectedEntry: WikiEntryDto? = null,
    val appearances: List<AppearanceRecordDto> = emptyList(),
    // Form
    val formName: String = "",
    val formEntityType: String = "Character",
    val formDescription: String = "",
    val formTags: String = "",
    val isSaving: Boolean = false,
    val error: String = ""
)

val EntityTypes = listOf("Character", "Location", "Event", "Object", "Concept")

// ─── ViewModel ────────────────────────────────────────────────────────────────

class WikiViewModel(
    private val api: WikiApiService,
    private val projectId: String
) : ViewModel() {

    private val _state = MutableStateFlow(WikiUiState(isLoading = true))
    val state: StateFlow<WikiUiState> = _state.asStateFlow()

    init {
        loadEntries()
    }

    fun loadEntries() {
        viewModelScope.launch {
            _state.value = _state.value.copy(isLoading = true, error = "")
            try {
                val response = api.getEntries(projectId)
                if (response.isSuccessful) {
                    val sorted = (response.body() ?: emptyList())
                        .sortedWith(compareBy({ it.entityType }, { it.name }))
                    _state.value = _state.value.copy(entries = sorted, isLoading = false)
                } else {
                    _state.value = _state.value.copy(isLoading = false, error = "Failed to load wiki")
                }
            } catch (e: Exception) {
                _state.value = _state.value.copy(isLoading = false, error = e.message ?: "Unknown error")
            }
        }
    }

    fun selectEntry(entry: WikiEntryDto?) {
        if (entry == null) {
            clearForm()
            return
        }

        viewModelScope.launch {
            _state.value = _state.value.copy(selectedEntry = entry, appearances = emptyList())
            try {
                // Fetch full entry
                val full = api.getEntry(projectId, entry.entityId).body() ?: entry
                // Fetch appearances
                val appearances = api.getAppearances(projectId, entry.entityId).body() ?: emptyList()
                _state.value = _state.value.copy(
                    selectedEntry = full,
                    formName = full.name,
                    formEntityType = full.entityType,
                    formDescription = full.description,
                    formTags = full.tags.joinToString(", "),
                    appearances = appearances
                )
            } catch (_: Exception) {}
        }
    }

    fun newEntry() = clearForm()

    fun updateForm(
        name: String? = null,
        entityType: String? = null,
        description: String? = null,
        tags: String? = null
    ) {
        _state.value = _state.value.copy(
            formName = name ?: _state.value.formName,
            formEntityType = entityType ?: _state.value.formEntityType,
            formDescription = description ?: _state.value.formDescription,
            formTags = tags ?: _state.value.formTags
        )
    }

    fun save() {
        val s = _state.value
        if (s.formName.isBlank()) {
            _state.value = s.copy(error = "Name is required.")
            return
        }

        val tagList = s.formTags
            .split(",")
            .map { it.trim() }
            .filter { it.isNotEmpty() }

        viewModelScope.launch {
            _state.value = _state.value.copy(isSaving = true, error = "")
            try {
                if (s.selectedEntry != null) {
                    // Update
                    val response = api.updateEntry(
                        projectId, s.selectedEntry.entityId,
                        UpdateWikiEntryRequest(s.formName, s.formEntityType, s.formDescription, tagList)
                    )
                    if (response.isSuccessful) {
                        val updated = response.body()!!
                        val entries = _state.value.entries.map {
                            if (it.entityId == updated.entityId) updated else it
                        }
                        _state.value = _state.value.copy(
                            entries = entries, selectedEntry = updated, isSaving = false
                        )
                    } else {
                        _state.value = _state.value.copy(isSaving = false, error = "Failed to update.")
                    }
                } else {
                    // Create
                    val response = api.createEntry(
                        projectId,
                        CreateWikiEntryRequest(s.formName, s.formEntityType, s.formDescription, tagList)
                    )
                    if (response.isSuccessful) {
                        val created = response.body()!!
                        _state.value = _state.value.copy(
                            entries = _state.value.entries + created,
                            selectedEntry = created,
                            isSaving = false
                        )
                    } else {
                        _state.value = _state.value.copy(isSaving = false, error = "Failed to create.")
                    }
                }
            } catch (e: Exception) {
                _state.value = _state.value.copy(isSaving = false, error = e.message ?: "Error")
            }
        }
    }

    fun delete() {
        val entry = _state.value.selectedEntry ?: return
        viewModelScope.launch {
            try {
                val response = api.deleteEntry(projectId, entry.entityId)
                if (response.isSuccessful) {
                    val entries = _state.value.entries.filter { it.entityId != entry.entityId }
                    _state.value = _state.value.copy(entries = entries)
                    clearForm()
                }
            } catch (_: Exception) {}
        }
    }

    private fun clearForm() {
        _state.value = _state.value.copy(
            selectedEntry = null,
            formName = "",
            formEntityType = "Character",
            formDescription = "",
            formTags = "",
            appearances = emptyList(),
            error = ""
        )
    }
}
