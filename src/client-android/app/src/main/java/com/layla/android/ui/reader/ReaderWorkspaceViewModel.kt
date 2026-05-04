package com.layla.android.ui.reader

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.layla.android.data.api.PresenceSignalRClient
import com.layla.android.data.model.ProjectDto
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class ReaderUiState(
    val isAuthorActive: Boolean = false,
    val authorStatusText: String = "Author is offline"
)

class ReaderWorkspaceViewModel(
    val project: ProjectDto,
    private val token: String?,
    private val baseUrl: String
) : ViewModel() {

    private val presenceClient = PresenceSignalRClient(baseUrl)

    private val _uiState = MutableStateFlow(
        ReaderUiState(
            isAuthorActive = project.isAuthorActive,
            authorStatusText = if (project.isAuthorActive) "Author is active · live changes" else "Author is offline"
        )
    )
    val uiState: StateFlow<ReaderUiState> = _uiState.asStateFlow()

    init {
        connectAndWatch()
    }

    private fun connectAndWatch() {
        viewModelScope.launch(Dispatchers.IO) {
            try {
                presenceClient.connect(token)
                presenceClient.watchProject(project.id)
            } catch (_: Exception) {}

            // Observe presence updates
            presenceClient.presenceUpdates.collect { update ->
                update ?: return@collect
                if (update.projectId == project.id) {
                    _uiState.value = ReaderUiState(
                        isAuthorActive = update.isActive,
                        authorStatusText = if (update.isActive) "Author is active · live changes" else "Author is offline"
                    )
                }
            }
        }
    }

    override fun onCleared() {
        super.onCleared()
        viewModelScope.launch(Dispatchers.IO) {
            try { presenceClient.disconnect() } catch (_: Exception) {}
        }
    }
}

class ReaderWorkspaceViewModelFactory(
    private val project: ProjectDto,
    private val token: String?,
    private val baseUrl: String
) : ViewModelProvider.Factory {
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        @Suppress("UNCHECKED_CAST")
        return ReaderWorkspaceViewModel(project, token, baseUrl) as T
    }
}
