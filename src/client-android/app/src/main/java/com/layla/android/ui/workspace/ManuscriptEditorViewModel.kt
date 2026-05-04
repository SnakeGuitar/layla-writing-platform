package com.layla.android.ui.workspace

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.layla.android.data.api.ManuscriptApiService
import com.layla.android.data.model.ChapterDto
import com.layla.android.data.model.ManuscriptDto
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

// ─── State ────────────────────────────────────────────────────────────────────

sealed class ManuscriptState {
    object Loading : ManuscriptState()
    data class Ready(
        val manuscripts: List<ManuscriptDto>,
        val selectedManuscript: ManuscriptDto?,
        val chapters: List<ChapterDto>,
        val currentChapter: ChapterDto?,
        val isSaving: Boolean = false,
        val hasOfflineChanges: Boolean = false
    ) : ManuscriptState()
    data class Error(val message: String) : ManuscriptState()
}

// ─── ViewModel ────────────────────────────────────────────────────────────────

class ManuscriptEditorViewModel(
    private val api: ManuscriptApiService,
    private val projectId: String
) : ViewModel() {

    private val _state = MutableStateFlow<ManuscriptState>(ManuscriptState.Loading)
    val state: StateFlow<ManuscriptState> = _state.asStateFlow()

    init {
        loadManuscripts()
    }

    // ─── Load ─────────────────────────────────────────────────────────────────

    fun loadManuscripts() {
        viewModelScope.launch {
            _state.value = ManuscriptState.Loading
            try {
                val response = api.getManuscripts(projectId)
                if (response.isSuccessful) {
                    val manuscripts = response.body()?.sortedBy { it.order } ?: emptyList()
                    if (manuscripts.isEmpty()) {
                        // Auto-create default manuscript + chapter
                        val newMs = createManuscriptInternal("Manuscript 1", 0)
                        if (newMs != null) {
                            val firstChapter = createChapterInternal(newMs.manuscriptId, "Chapter 1", 0)
                            val ms = if (firstChapter != null) newMs.copy(chapters = listOf(firstChapter)) else newMs
                            val chapters = ms.chapters
                            _state.value = ManuscriptState.Ready(
                                manuscripts = listOf(ms),
                                selectedManuscript = ms,
                                chapters = chapters,
                                currentChapter = chapters.firstOrNull()
                            )
                            chapters.firstOrNull()?.let { loadChapterContent(ms, it) }
                        } else {
                            _state.value = ManuscriptState.Error("Could not initialize manuscript")
                        }
                    } else {
                        val first = manuscripts.first()
                        val chapters = first.chapters.sortedBy { it.order }
                        _state.value = ManuscriptState.Ready(
                            manuscripts = manuscripts,
                            selectedManuscript = first,
                            chapters = chapters,
                            currentChapter = null
                        )
                        chapters.firstOrNull()?.let { loadChapterContent(first, it) }
                    }
                } else {
                    _state.value = ManuscriptState.Error("Failed to load manuscripts (${response.code()})")
                }
            } catch (e: Exception) {
                _state.value = ManuscriptState.Error(e.message ?: "Unknown error")
            }
        }
    }

    // ─── Chapter content ──────────────────────────────────────────────────────

    private fun loadChapterContent(manuscript: ManuscriptDto, chapter: ChapterDto) {
        viewModelScope.launch {
            try {
                val response = api.getChapter(projectId, manuscript.manuscriptId, chapter.chapterId)
                if (response.isSuccessful) {
                    val full = response.body() ?: chapter
                    val ready = _state.value as? ManuscriptState.Ready ?: return@launch
                    _state.value = ready.copy(currentChapter = full)
                }
            } catch (_: Exception) {}
        }
    }

    fun selectManuscript(manuscript: ManuscriptDto) {
        val ready = _state.value as? ManuscriptState.Ready ?: return
        if (manuscript.manuscriptId == ready.selectedManuscript?.manuscriptId) return

        val chapters = manuscript.chapters.sortedBy { it.order }
        _state.value = ready.copy(
            selectedManuscript = manuscript,
            chapters = chapters,
            currentChapter = null
        )
        chapters.firstOrNull()?.let { loadChapterContent(manuscript, it) }
    }

    fun selectChapter(chapter: ChapterDto) {
        val ready = _state.value as? ManuscriptState.Ready ?: return
        if (chapter.chapterId == ready.currentChapter?.chapterId) return
        val manuscript = ready.selectedManuscript ?: return
        loadChapterContent(manuscript, chapter)
    }

    // ─── Save chapter ─────────────────────────────────────────────────────────

    fun saveContent(content: String) {
        val ready = _state.value as? ManuscriptState.Ready ?: return
        val manuscript = ready.selectedManuscript ?: return
        val chapter = ready.currentChapter ?: return
        if (ready.isSaving) return

        viewModelScope.launch {
            _state.value = ready.copy(isSaving = true)
            try {
                val response = api.updateChapter(
                    projectId,
                    manuscript.manuscriptId,
                    chapter.chapterId,
                    mapOf(
                        "title" to chapter.title,
                        "content" to content,
                        "order" to chapter.order
                    )
                )
                val currentReady = _state.value as? ManuscriptState.Ready ?: return@launch
                if (response.isSuccessful) {
                    _state.value = currentReady.copy(
                        currentChapter = response.body() ?: chapter.copy(content = content),
                        isSaving = false,
                        hasOfflineChanges = false
                    )
                } else {
                    val errorBody = response.errorBody()?.string()
                    android.util.Log.e("ManuscriptEditor", "Save failed (${response.code()}): $errorBody")
                    _state.value = currentReady.copy(
                        isSaving = false,
                        hasOfflineChanges = true
                    )
                }
            } catch (e: Exception) {
                android.util.Log.e("ManuscriptEditor", "Save exception: ${e.message}", e)
                val currentReady = _state.value as? ManuscriptState.Ready ?: return@launch
                _state.value = currentReady.copy(isSaving = false, hasOfflineChanges = true)
            }
        }
    }

    // ─── Add manuscript ───────────────────────────────────────────────────────

    fun addManuscript() {
        val ready = _state.value as? ManuscriptState.Ready ?: return
        val order = ready.manuscripts.size

        viewModelScope.launch {
            val newMs = createManuscriptInternal("Manuscript ${order + 1}", order) ?: return@launch
            val chapter = createChapterInternal(newMs.manuscriptId, "Chapter 1", 0) ?: return@launch
            val ms = newMs.copy(chapters = listOf(chapter))
            val currentReady = _state.value as? ManuscriptState.Ready ?: return@launch
            val manuscripts = currentReady.manuscripts + ms
            _state.value = currentReady.copy(
                manuscripts = manuscripts,
                selectedManuscript = ms,
                chapters = listOf(chapter),
                currentChapter = chapter
            )
        }
    }

    // ─── Add chapter ──────────────────────────────────────────────────────────

    fun addChapter() {
        val ready = _state.value as? ManuscriptState.Ready ?: return
        val manuscript = ready.selectedManuscript ?: return
        val order = ready.chapters.size

        viewModelScope.launch {
            val chapter = createChapterInternal(manuscript.manuscriptId, "Chapter ${order + 1}", order) ?: return@launch
            val currentReady = _state.value as? ManuscriptState.Ready ?: return@launch
            val chapters = currentReady.chapters + chapter
            _state.value = currentReady.copy(chapters = chapters, currentChapter = chapter)
        }
    }

    // ─── Delete manuscript ────────────────────────────────────────────────────

    fun deleteManuscript(manuscript: ManuscriptDto) {
        val ready = _state.value as? ManuscriptState.Ready ?: return
        if (ready.manuscripts.size <= 1) return

        viewModelScope.launch {
            try {
                val response = api.deleteManuscript(projectId, manuscript.manuscriptId)
                if (response.isSuccessful) {
                    val currentReady = _state.value as? ManuscriptState.Ready ?: return@launch
                    val remaining = currentReady.manuscripts.filter { it.manuscriptId != manuscript.manuscriptId }
                    val first = remaining.first()
                    val chapters = first.chapters.sortedBy { it.order }
                    _state.value = currentReady.copy(
                        manuscripts = remaining,
                        selectedManuscript = first,
                        chapters = chapters,
                        currentChapter = null
                    )
                    chapters.firstOrNull()?.let { loadChapterContent(first, it) }
                }
            } catch (_: Exception) {}
        }
    }

    // ─── Delete chapter ───────────────────────────────────────────────────────

    fun deleteChapter(chapter: ChapterDto) {
        val ready = _state.value as? ManuscriptState.Ready ?: return
        val manuscript = ready.selectedManuscript ?: return
        if (ready.chapters.size <= 1) return

        viewModelScope.launch {
            try {
                val response = api.deleteChapter(projectId, manuscript.manuscriptId, chapter.chapterId)
                if (response.isSuccessful) {
                    val currentReady = _state.value as? ManuscriptState.Ready ?: return@launch
                    val chapters = currentReady.chapters.filter { it.chapterId != chapter.chapterId }
                    _state.value = currentReady.copy(chapters = chapters, currentChapter = null)
                    chapters.firstOrNull()?.let { loadChapterContent(manuscript, it) }
                }
            } catch (_: Exception) {}
        }
    }

    // ─── Internal helpers ─────────────────────────────────────────────────────

    private suspend fun createManuscriptInternal(title: String, order: Int): ManuscriptDto? {
        return try {
            val response = api.createManuscript(
                projectId,
                mapOf("title" to title, "order" to order)
            )
            if (response.isSuccessful) {
                response.body()
            } else {
                android.util.Log.e("ManuscriptEditor", "Create manuscript failed (${response.code()}): ${response.errorBody()?.string()}")
                null
            }
        } catch (e: Exception) {
            android.util.Log.e("ManuscriptEditor", "Create manuscript exception: ${e.message}", e)
            null
        }
    }

    private suspend fun createChapterInternal(manuscriptId: String, title: String, order: Int): ChapterDto? {
        return try {
            val response = api.createChapter(
                projectId, manuscriptId,
                mapOf("title" to title, "content" to "", "order" to order)
            )
            if (response.isSuccessful) {
                response.body()
            } else {
                android.util.Log.e("ManuscriptEditor", "Create chapter failed (${response.code()}): ${response.errorBody()?.string()}")
                null
            }
        } catch (e: Exception) {
            android.util.Log.e("ManuscriptEditor", "Create chapter exception: ${e.message}", e)
            null
        }
    }
}
