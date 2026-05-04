package com.layla.android.data.api

import com.layla.android.data.model.*
import retrofit2.Response
import retrofit2.http.*

interface ProjectApiService {

    // ─── Public ─────────────────────────────────────────────────────────────

    @GET("api/projects/public")
    suspend fun getPublicProjects(): Response<List<ProjectDto>>

    // ─── My Projects ─────────────────────────────────────────────────────────

    @GET("api/projects")
    suspend fun getMyProjects(): Response<List<ProjectDto>>

    @GET("api/projects/{id}")
    suspend fun getProjectById(@Path("id") id: String): Response<ProjectDto>

    @POST("api/projects")
    suspend fun createProject(@Body request: CreateProjectRequest): Response<ProjectDto>

    @PUT("api/projects/{id}")
    suspend fun updateProject(
        @Path("id") id: String,
        @Body request: UpdateProjectRequest
    ): Response<ProjectDto>

    @DELETE("api/projects/{id}")
    suspend fun deleteProject(@Path("id") id: String): Response<Unit>

    // ─── Collaborators ───────────────────────────────────────────────────────

    @POST("api/projects/{id}/join")
    suspend fun joinProject(@Path("id") id: String): Response<CollaboratorDto>

    @GET("api/projects/{id}/collaborators")
    suspend fun getCollaborators(@Path("id") id: String): Response<List<CollaboratorDto>>

    @POST("api/projects/{id}/collaborators")
    suspend fun inviteCollaborator(
        @Path("id") id: String,
        @Body request: InviteCollaboratorRequest
    ): Response<CollaboratorDto>

    @DELETE("api/projects/{projectId}/collaborators/{userId}")
    suspend fun removeCollaborator(
        @Path("projectId") projectId: String,
        @Path("userId") userId: String
    ): Response<Unit>
}
