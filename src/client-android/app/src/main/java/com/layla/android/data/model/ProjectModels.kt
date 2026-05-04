package com.layla.android.data.model

// ─── Project ────────────────────────────────────────────────────────────────

data class ProjectDto(
    val id: String,
    val title: String,
    val synopsis: String,
    val literaryGenre: String,
    val coverImageUrl: String?,
    val updatedAt: String,
    val isPublic: Boolean,
    val isAuthorActive: Boolean = false,
    val userRole: String = ""
)

data class CreateProjectRequest(
    val title: String,
    val literaryGenre: String,
    val synopsis: String,
    val isPublic: Boolean
)

data class UpdateProjectRequest(
    val title: String,
    val literaryGenre: String,
    val synopsis: String,
    val isPublic: Boolean
)

// ─── Collaborators ──────────────────────────────────────────────────────────

data class CollaboratorDto(
    val userId: String,
    val displayName: String?,
    val email: String?,
    val role: String,
    val assignedAt: String?
)

data class InviteCollaboratorRequest(
    val email: String,
    val role: String = "READER"
)

// ─── Manuscripts & Chapters ─────────────────────────────────────────────────

data class ManuscriptDto(
    val manuscriptId: String,
    val projectId: String,
    val title: String,
    val order: Int,
    val chapters: List<ChapterDto> = emptyList()
)

data class ChapterDto(
    val chapterId: String,
    val manuscriptId: String,
    val title: String,
    val content: String = "",
    val order: Int,
    val updatedAt: String? = null,
    val mentions: List<MentionDto> = emptyList()
)

data class MentionDto(
    val entityId: String,
    val entityName: String,
    val entityType: String
)

// ─── Wiki ────────────────────────────────────────────────────────────────────

data class WikiEntryDto(
    val entityId: String,
    val projectId: String,
    val name: String,
    val entityType: String,
    val description: String = "",
    val tags: List<String> = emptyList()
)

data class CreateWikiEntryRequest(
    val name: String,
    val entityType: String,
    val description: String?,
    val tags: List<String>?
)

data class UpdateWikiEntryRequest(
    val name: String?,
    val entityType: String?,
    val description: String?,
    val tags: List<String>?
)

data class AppearanceRecordDto(
    val manuscriptId: String,
    val manuscriptTitle: String,
    val chapterId: String,
    val chapterTitle: String,
    val chapterOrder: Int
)
