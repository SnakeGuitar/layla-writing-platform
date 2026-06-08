package com.layla.android.ui.projects

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.layla.android.data.api.AdminApiService
import com.layla.android.data.api.ProjectApiService
import com.layla.android.data.model.CreateProjectRequest
import com.layla.android.data.model.ProjectDto
import com.layla.android.data.model.SystemReportDto
import com.layla.android.data.model.UpdateProjectRequest
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

sealed class MyProjectsState {
    object Loading : MyProjectsState()
    data class Success(val projects: List<ProjectDto>) : MyProjectsState()
    data class Error(val message: String) : MyProjectsState()
}

data class ProjectFormState(
    val title: String = "",
    val genre: String = "",
    val synopsis: String = "",
    val isPublic: Boolean = false,
    val error: String = "",
    val isSaving: Boolean = false
)

/**
 * View model for Android's final-scope surface: project administration and
 * system statistics. Creative-writing workflows are intentionally excluded
 * from the mobile client and remain in desktop/web.
 */
class MyProjectsViewModel(
    private val projectApiService: ProjectApiService,
    private val adminApiService: AdminApiService
) : ViewModel() {

    private val _state = MutableStateFlow<MyProjectsState>(MyProjectsState.Loading)
    val state: StateFlow<MyProjectsState> = _state.asStateFlow()

    private val _createForm = MutableStateFlow(ProjectFormState())
    val createForm: StateFlow<ProjectFormState> = _createForm.asStateFlow()

    private val _showCreateDialog = MutableStateFlow(false)
    val showCreateDialog: StateFlow<Boolean> = _showCreateDialog.asStateFlow()

    private val _editForm = MutableStateFlow(ProjectFormState())
    val editForm: StateFlow<ProjectFormState> = _editForm.asStateFlow()

    private val _showEditDialog = MutableStateFlow(false)
    val showEditDialog: StateFlow<Boolean> = _showEditDialog.asStateFlow()

    private var editingProjectId: String = ""

    private val _sessionDisplaced = MutableStateFlow(false)
    val sessionDisplaced: StateFlow<Boolean> = _sessionDisplaced.asStateFlow()

    private val _stats = MutableStateFlow<SystemReportDto?>(null)
    val stats: StateFlow<SystemReportDto?> = _stats.asStateFlow()

    private val _statsError = MutableStateFlow<String?>(null)
    val statsError: StateFlow<String?> = _statsError.asStateFlow()

    init {
        loadProjects()
        loadStats()
    }

    fun loadProjects() {
        viewModelScope.launch {
            _state.value = MyProjectsState.Loading
            try {
                val response = projectApiService.getMyProjects()
                _state.value = when {
                    response.isSuccessful -> MyProjectsState.Success(response.body() ?: emptyList())
                    response.code() == 401 -> {
                        _sessionDisplaced.value = true
                        MyProjectsState.Error("Session expired.")
                    }
                    else -> MyProjectsState.Error("Failed to load projects (${response.code()})")
                }
            } catch (e: Exception) {
                _state.value = MyProjectsState.Error(e.message ?: "Unknown error")
            }
        }
    }

    fun loadStats() {
        viewModelScope.launch {
            _statsError.value = null
            try {
                val response = adminApiService.getSystemReport()
                when {
                    response.isSuccessful -> _stats.value = response.body()
                    response.code() == 401 -> _sessionDisplaced.value = true
                    response.code() == 403 -> _statsError.value = "Admin role required to view statistics."
                    else -> _statsError.value = "Failed to load statistics (${response.code()})"
                }
            } catch (e: Exception) {
                _statsError.value = e.message ?: "Unknown statistics error"
            }
        }
    }

    fun openCreateDialog() {
        _createForm.value = ProjectFormState()
        _showCreateDialog.value = true
    }

    fun closeCreateDialog() {
        _showCreateDialog.value = false
    }

    fun updateCreateForm(form: ProjectFormState) {
        _createForm.value = form
    }

    fun createProject() {
        val form = _createForm.value
        if (form.title.isBlank() || form.genre.isBlank() || form.synopsis.isBlank()) {
            _createForm.value = form.copy(error = "Please fill in all fields.")
            return
        }

        viewModelScope.launch {
            _createForm.value = form.copy(isSaving = true, error = "")
            try {
                val response = projectApiService.createProject(
                    CreateProjectRequest(
                        title = form.title,
                        literaryGenre = form.genre,
                        synopsis = form.synopsis,
                        isPublic = form.isPublic
                    )
                )
                if (response.isSuccessful) {
                    _showCreateDialog.value = false
                    loadProjects()
                    loadStats()
                } else {
                    _createForm.value = _createForm.value.copy(
                        isSaving = false,
                        error = "Failed to create project."
                    )
                }
            } catch (e: Exception) {
                _createForm.value = _createForm.value.copy(
                    isSaving = false,
                    error = e.message ?: "Error creating project"
                )
            }
        }
    }

    fun openEditDialog(project: ProjectDto) {
        editingProjectId = project.id
        _editForm.value = ProjectFormState(
            title = project.title,
            genre = project.literaryGenre,
            synopsis = project.synopsis,
            isPublic = project.isPublic
        )
        _showEditDialog.value = true
    }

    fun closeEditDialog() {
        _showEditDialog.value = false
    }

    fun updateEditForm(form: ProjectFormState) {
        _editForm.value = form
    }

    fun updateProject() {
        val form = _editForm.value
        if (form.title.isBlank() || form.genre.isBlank() || form.synopsis.isBlank()) {
            _editForm.value = form.copy(error = "Please fill in all fields.")
            return
        }

        viewModelScope.launch {
            _editForm.value = form.copy(isSaving = true, error = "")
            try {
                val response = projectApiService.updateProject(
                    editingProjectId,
                    UpdateProjectRequest(
                        title = form.title,
                        literaryGenre = form.genre,
                        synopsis = form.synopsis,
                        isPublic = form.isPublic
                    )
                )
                if (response.isSuccessful) {
                    _showEditDialog.value = false
                    loadProjects()
                    loadStats()
                } else {
                    _editForm.value = _editForm.value.copy(
                        isSaving = false,
                        error = "Failed to update project."
                    )
                }
            } catch (e: Exception) {
                _editForm.value = _editForm.value.copy(
                    isSaving = false,
                    error = e.message ?: "Error updating project"
                )
            }
        }
    }

    fun deleteProject(project: ProjectDto) {
        viewModelScope.launch {
            try {
                val response = projectApiService.deleteProject(project.id)
                if (response.isSuccessful) {
                    loadProjects()
                    loadStats()
                }
            } catch (_: Exception) {
            }
        }
    }
}

class MyProjectsViewModelFactory(
    private val projectApiService: ProjectApiService,
    private val adminApiService: AdminApiService
) : ViewModelProvider.Factory {
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        @Suppress("UNCHECKED_CAST")
        return MyProjectsViewModel(projectApiService, adminApiService) as T
    }
}
