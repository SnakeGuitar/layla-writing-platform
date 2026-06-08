package com.layla.android.data.model

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

/**
 * Mirrors `Layla.Core.Contracts.Admin.SystemReportDto`.
 *
 * Used by Android's mobile administration dashboard. Worldbuilding-specific
 * writing metrics are intentionally absent from Android.
 */
data class SystemReportDto(
    val generatedAt: String = "",
    val totalUsers: Int = 0,
    val newUsersThisMonth: Int = 0,
    val bannedUsers: Int = 0,
    val totalProjects: Int = 0,
    val projectsModifiedToday: Int = 0,
    val publicProjects: Int = 0,
    val newUsersPerMonth: List<Int> = emptyList()
)
