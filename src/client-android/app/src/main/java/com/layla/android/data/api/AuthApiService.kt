package com.layla.android.data.api

import com.layla.android.data.model.AuthResponse
import com.layla.android.data.model.LoginRequest
import com.layla.android.data.model.RegisterRequest
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST

interface AuthApiService {
    @POST("api/Users")
    suspend fun register(@Body request: RegisterRequest): Response<AuthResponse>

    @POST("api/Tokens")
    suspend fun login(@Body request: LoginRequest): Response<AuthResponse>
}
