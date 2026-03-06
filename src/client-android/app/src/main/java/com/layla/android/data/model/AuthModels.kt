package com.layla.android.data.model

data class LoginRequest(
    val email: String,
    val password: String
)

data class RegisterRequest(
    val email: String,
    val password: String,
    val displayName: String? = null
)

data class AuthResponse(
    val token: String,
    val refreshToken: String? = null,
    val expiration: String? = null,
    val user: UserDto? = null
)

data class UserDto(
    val id: String,
    val email: String,
    val displayName: String? = null
)

data class ApiError(
    val message: String
)
