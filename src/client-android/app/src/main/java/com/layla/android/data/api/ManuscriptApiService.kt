package com.layla.android.data.api

import com.layla.android.data.model.*
import retrofit2.Response
import retrofit2.http.*

interface ManuscriptApiService {

    // ─── Manuscripts ─────────────────────────────────────────────────────────

    @GET("api/manuscripts/{projectId}")
    suspend fun getManuscripts(@Path("projectId") projectId: String): Response<List<ManuscriptDto>>

    @GET("api/manuscripts/{projectId}/{manuscriptId}")
    suspend fun getManuscript(
        @Path("projectId") projectId: String,
        @Path("manuscriptId") manuscriptId: String
    ): Response<ManuscriptDto>

    @POST("api/manuscripts/{projectId}")
    suspend fun createManuscript(
        @Path("projectId") projectId: String,
        @Body body: Map<String, Any>
    ): Response<ManuscriptDto>

    @PUT("api/manuscripts/{projectId}/{manuscriptId}")
    suspend fun updateManuscript(
        @Path("projectId") projectId: String,
        @Path("manuscriptId") manuscriptId: String,
        @Body body: Map<String, Any?>
    ): Response<ManuscriptDto>

    @DELETE("api/manuscripts/{projectId}/{manuscriptId}")
    suspend fun deleteManuscript(
        @Path("projectId") projectId: String,
        @Path("manuscriptId") manuscriptId: String
    ): Response<Unit>

    // ─── Chapters ────────────────────────────────────────────────────────────

    @GET("api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}")
    suspend fun getChapter(
        @Path("projectId") projectId: String,
        @Path("manuscriptId") manuscriptId: String,
        @Path("chapterId") chapterId: String
    ): Response<ChapterDto>

    @POST("api/manuscripts/{projectId}/{manuscriptId}/chapters")
    suspend fun createChapter(
        @Path("projectId") projectId: String,
        @Path("manuscriptId") manuscriptId: String,
        @Body body: Map<String, Any>
    ): Response<ChapterDto>

    @PUT("api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}")
    suspend fun updateChapter(
        @Path("projectId") projectId: String,
        @Path("manuscriptId") manuscriptId: String,
        @Path("chapterId") chapterId: String,
        @Body body: Map<String, Any?>
    ): Response<ChapterDto>

    @DELETE("api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}")
    suspend fun deleteChapter(
        @Path("projectId") projectId: String,
        @Path("manuscriptId") manuscriptId: String,
        @Path("chapterId") chapterId: String
    ): Response<Unit>
}
