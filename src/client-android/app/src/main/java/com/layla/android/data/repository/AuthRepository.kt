package com.layla.android.data.repository

import com.layla.android.data.api.AuthApiService
import com.layla.android.data.model.AuthResponse
import com.layla.android.data.model.LoginRequest
import com.layla.android.data.model.RegisterRequest
import retrofit2.Response

class AuthRepository(private val apiService: AuthApiService) {
    suspend fun login(request: LoginRequest): Response<AuthResponse> {
        return apiService.login(request)
    }

    suspend fun register(request: RegisterRequest): Response<AuthResponse> {
        return apiService.register(request)
    }
}
