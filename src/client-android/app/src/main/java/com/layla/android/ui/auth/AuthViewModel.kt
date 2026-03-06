package com.layla.android.ui.auth

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.layla.android.data.local.SessionManager
import com.layla.android.data.model.ApiError
import com.layla.android.data.model.LoginRequest
import com.layla.android.data.model.RegisterRequest
import com.layla.android.data.repository.AuthRepository
import com.google.gson.Gson
import kotlinx.coroutines.launch

sealed class AuthState {
    object Idle : AuthState()
    object Loading : AuthState()
    data class Success(val message: String) : AuthState()
    data class Error(val message: String) : AuthState()
}

class AuthViewModel(
    private val repository: AuthRepository,
    private val sessionManager: SessionManager
) : ViewModel() {

    var authState by mutableStateOf<AuthState>(AuthState.Idle)
        private set

    fun login(email: String, password: String) {
        viewModelScope.launch {
            authState = AuthState.Loading
            try {
                val response = repository.login(LoginRequest(email, password))
                if (response.isSuccessful) {
                    response.body()?.token?.let { token ->
                        sessionManager.saveAuthToken(token)
                    }
                    authState = AuthState.Success("Login Successful")
                } else {
                    val errorBody = response.errorBody()?.string()
                    val error = try {
                        Gson().fromJson(errorBody, ApiError::class.java)
                    } catch (e: Exception) {
                        null
                    }
                    authState = AuthState.Error(error?.message ?: "Login failed")
                }
            } catch (e: Exception) {
                authState = AuthState.Error(e.message ?: "An error occurred")
            }
        }
    }

    fun register(email: String, password: String, displayName: String?) {
        viewModelScope.launch {
            authState = AuthState.Loading
            try {
                val response = repository.register(RegisterRequest(email, password, displayName))
                if (response.isSuccessful) {
                    response.body()?.token?.let { token ->
                        sessionManager.saveAuthToken(token)
                    }
                    authState = AuthState.Success("Registration Successful")
                } else {
                    val errorBody = response.errorBody()?.string()
                    val error = try {
                        Gson().fromJson(errorBody, ApiError::class.java)
                    } catch (e: Exception) {
                        null
                    }
                    authState = AuthState.Error(error?.message ?: "Registration failed")
                }
            } catch (e: Exception) {
                authState = AuthState.Error(e.message ?: "An error occurred")
            }
        }
    }
    
    fun resetState() {
        authState = AuthState.Idle
    }
}
