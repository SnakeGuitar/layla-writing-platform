package com.layla.android.data.api

import com.layla.android.data.model.*
import retrofit2.Response
import retrofit2.http.*

interface WikiApiService {

    @GET("api/wiki/{projectId}/entries")
    suspend fun getEntries(
        @Path("projectId") projectId: String,
        @Query("entityType") entityType: String? = null
    ): Response<List<WikiEntryDto>>

    @GET("api/wiki/{projectId}/entries/{entityId}")
    suspend fun getEntry(
        @Path("projectId") projectId: String,
        @Path("entityId") entityId: String
    ): Response<WikiEntryDto>

    @POST("api/wiki/{projectId}/entries")
    suspend fun createEntry(
        @Path("projectId") projectId: String,
        @Body request: CreateWikiEntryRequest
    ): Response<WikiEntryDto>

    @PUT("api/wiki/{projectId}/entries/{entityId}")
    suspend fun updateEntry(
        @Path("projectId") projectId: String,
        @Path("entityId") entityId: String,
        @Body request: UpdateWikiEntryRequest
    ): Response<WikiEntryDto>

    @DELETE("api/wiki/{projectId}/entries/{entityId}")
    suspend fun deleteEntry(
        @Path("projectId") projectId: String,
        @Path("entityId") entityId: String
    ): Response<Unit>

    @GET("api/wiki/{projectId}/entries/{entityId}/appearances")
    suspend fun getAppearances(
        @Path("projectId") projectId: String,
        @Path("entityId") entityId: String
    ): Response<List<AppearanceRecordDto>>
}
