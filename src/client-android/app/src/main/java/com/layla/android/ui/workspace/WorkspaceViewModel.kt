package com.layla.android.ui.workspace

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.layla.android.data.api.ManuscriptApiService
import com.layla.android.data.api.PresenceSignalRClient
import com.layla.android.data.api.ProjectApiService
import com.layla.android.data.api.WikiApiService
import com.layla.android.data.model.*
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch

// ─── Collaborators state ──────────────────────────────────────────────────────

data class CollaboratorsUiState(
    val collaborators: List<CollaboratorDto> = emptyList(),
    val isLoading: Boolean = false,
    val isVisible: Boolean = false,
    val inviteEmail: String = "",
    val inviteError: String = "",
    val isInviting: Boolean = false
)

// ─── ViewModel ────────────────────────────────────────────────────────────────

class WorkspaceViewModel(
    val project: ProjectDto,
    private val token: String,
    private val baseUrl: String,
    private val projectApiService: ProjectApiService,
    private val manuscriptApiService: ManuscriptApiService,
    private val wikiApiService: WikiApiService
) : ViewModel() {

    // ─── Presence (heartbeat) ─────────────────────────────────────────────────
    private val presenceClient = PresenceSignalRClient(baseUrl)
    private var heartbeatJob: Job? = null

    // ─── Collaborators ────────────────────────────────────────────────────────
    private val _collab = MutableStateFlow(CollaboratorsUiState())
    val collaboratorsState: StateFlow<CollaboratorsUiState> = _collab.asStateFlow()

    // ─── Session displaced ────────────────────────────────────────────────────
    private val _sessionDisplaced = MutableStateFlow(false)
    val sessionDisplaced: StateFlow<Boolean> = _sessionDisplaced.asStateFlow()

    // ─── Manuscript editor (passed down to ManuscriptViewModel) ───────────────
    val manuscriptViewModel = ManuscriptEditorViewModel(manuscriptApiService, project.id)

    // ─── Wiki ─────────────────────────────────────────────────────────────────
    val wikiViewModel = WikiViewModel(wikiApiService, project.id)

    init {
        startHeartbeat()
    }

    // ─── Heartbeat ────────────────────────────────────────────────────────────

    private fun startHeartbeat() {
        heartbeatJob = viewModelScope.launch(Dispatchers.IO) {
            // Connect once
            try {
                presenceClient.connect(token)
            } catch (_: Exception) {}

            // Send heartbeat every 30 s
            while (isActive) {
                try {
                    presenceClient.sendHeartbeat(project.id)
                } catch (_: Exception) {}
                delay(30_000)
            }
        }
    }

    // ─── Collaborators ────────────────────────────────────────────────────────

    fun openCollaborators() {
        _collab.value = _collab.value.copy(isVisible = true, inviteEmail = "", inviteError = "")
        loadCollaborators()
    }

    fun closeCollaborators() {
        _collab.value = _collab.value.copy(isVisible = false)
    }

    fun setInviteEmail(email: String) {
        _collab.value = _collab.value.copy(inviteEmail = email)
    }

    private fun loadCollaborators() {
        viewModelScope.launch {
            _collab.value = _collab.value.copy(isLoading = true)
            try {
                val response = projectApiService.getCollaborators(project.id)
                if (response.isSuccessful) {
                    _collab.value = _collab.value.copy(
                        collaborators = response.body() ?: emptyList(),
                        isLoading = false
                    )
                } else {
                    _collab.value = _collab.value.copy(isLoading = false)
                }
            } catch (_: Exception) {
                _collab.value = _collab.value.copy(isLoading = false)
            }
        }
    }

    fun inviteCollaborator() {
        val email = _collab.value.inviteEmail.trim()
        if (email.isBlank()) {
            _collab.value = _collab.value.copy(inviteError = "Please enter an email address.")
            return
        }

        viewModelScope.launch {
            _collab.value = _collab.value.copy(isInviting = true, inviteError = "")
            try {
                val response = projectApiService.inviteCollaborator(
                    project.id,
                    InviteCollaboratorRequest(email = email)
                )
                if (response.isSuccessful) {
                    _collab.value = _collab.value.copy(inviteEmail = "", isInviting = false)
                    loadCollaborators()
                } else {
                    _collab.value = _collab.value.copy(
                        isInviting = false,
                        inviteError = "Could not invite user. Check the email and try again."
                    )
                }
            } catch (e: Exception) {
                _collab.value = _collab.value.copy(
                    isInviting = false,
                    inviteError = e.message ?: "Error inviting collaborator"
                )
            }
        }
    }

    fun removeCollaborator(collaborator: CollaboratorDto) {
        viewModelScope.launch {
            try {
                val response = projectApiService.removeCollaborator(project.id, collaborator.userId)
                if (response.isSuccessful) loadCollaborators()
            } catch (_: Exception) {}
        }
    }

    // ─── Cleanup ──────────────────────────────────────────────────────────────

    override fun onCleared() {
        super.onCleared()
        heartbeatJob?.cancel()
        viewModelScope.launch(Dispatchers.IO) {
            try { presenceClient.disconnect() } catch (_: Exception) {}
        }
    }
}

// ─── Factory ─────────────────────────────────────────────────────────────────

class WorkspaceViewModelFactory(
    private val project: ProjectDto,
    private val token: String,
    private val baseUrl: String,
    private val projectApiService: ProjectApiService,
    private val manuscriptApiService: ManuscriptApiService,
    private val wikiApiService: WikiApiService
) : ViewModelProvider.Factory {
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        @Suppress("UNCHECKED_CAST")
        return WorkspaceViewModel(
            project, token, baseUrl, projectApiService, manuscriptApiService, wikiApiService
        ) as T
    }
}
