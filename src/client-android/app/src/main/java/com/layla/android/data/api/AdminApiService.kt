package com.layla.android.data.api

import com.layla.android.data.model.SystemReportDto
import retrofit2.Response
import retrofit2.http.GET

/**
 * Core API endpoints used by the reduced Android client.
 *
 * Android intentionally exposes administration and metrics only; manuscript,
 * wiki, graph, reader, and voice features live in the desktop and web clients.
 */
interface AdminApiService {
    /**
     * Returns the aggregate system report used by the mobile statistics panel.
     * The server requires an admin JWT and returns 403 for non-admin users.
     */
    @GET("api/admin/reports/system")
    suspend fun getSystemReport(): Response<SystemReportDto>
}
