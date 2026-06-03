package com.layla.android.ui.reader

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.layla.android.data.api.ManuscriptApiService
import com.layla.android.data.api.PresenceSignalRClient
import com.layla.android.data.api.RetrofitClient
import com.layla.android.data.model.ManuscriptDto
import com.layla.android.data.model.ProjectDto
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class ReaderUiState(
    val isAuthorActive: Boolean = false,
    val authorStatusText: String = "Author is offline",
    val isStoryLoading: Boolean = true,
    val story: List<ManuscriptDto> = emptyList(),
    val storyError: String? = null
)

class ReaderWorkspaceViewModel(
    val project: ProjectDto,
    private val token: String?,
    private val baseUrl: String,
    private val manuscriptApi: ManuscriptApiService = RetrofitClient.manuscriptApiService
) : ViewModel() {

    private val presenceClient = PresenceSignalRClient(baseUrl)

    // Independent scope for cleanup work (see VoiceViewModel for rationale).
    private val cleanupScope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    private val _uiState = MutableStateFlow(
        ReaderUiState(
            isAuthorActive = project.isAuthorActive,
            authorStatusText = if (project.isAuthorActive) "Author is active · live changes" else "Author is offline"
        )
    )
    val uiState: StateFlow<ReaderUiState> = _uiState.asStateFlow()

    init {
        connectAndWatch()
        loadFullStory()
    }

    private fun connectAndWatch() {
        viewModelScope.launch(Dispatchers.IO) {
            val connected = try {
                presenceClient.connect(token)
                presenceClient.watchProject(project.id)
                true
            } catch (_: Exception) {
                false
            }
            if (!connected) return@launch

            // Observe presence updates
            presenceClient.presenceUpdates.collect { update ->
                update ?: return@collect
                if (update.projectId == project.id) {
                    _uiState.value = _uiState.value.copy(
                        isAuthorActive = update.isActive,
                        authorStatusText = if (update.isActive) "Author is active · live changes" else "Author is offline"
                    )
                }
            }
        }
    }

    private fun loadFullStory() {
        viewModelScope.launch(Dispatchers.IO) {
            _uiState.value = _uiState.value.copy(isStoryLoading = true, storyError = null)

            try {
                val response = manuscriptApi.getFullStory(project.id)
                _uiState.value = if (response.isSuccessful) {
                    _uiState.value.copy(
                        isStoryLoading = false,
                        story = response.body()?.sortedBy { it.order } ?: emptyList(),
                        storyError = null
                    )
                } else {
                    _uiState.value.copy(
                        isStoryLoading = false,
                        storyError = "Could not load story (${response.code()})"
                    )
                }
            } catch (ex: Exception) {
                _uiState.value = _uiState.value.copy(
                    isStoryLoading = false,
                    storyError = ex.message ?: "Could not load story"
                )
            }
        }
    }

    override fun onCleared() {
        super.onCleared()
        cleanupScope.launch {
            try { presenceClient.disconnect() } catch (_: Exception) {}
        }
    }
}

class ReaderWorkspaceViewModelFactory(
    private val project: ProjectDto,
    private val token: String?,
    private val baseUrl: String,
    private val manuscriptApi: ManuscriptApiService = RetrofitClient.manuscriptApiService
) : ViewModelProvider.Factory {
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        @Suppress("UNCHECKED_CAST")
        return ReaderWorkspaceViewModel(project, token, baseUrl, manuscriptApi) as T
    }
}
