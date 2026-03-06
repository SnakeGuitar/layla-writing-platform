package com.layla.android

import android.os.Bundle
import android.widget.Toast
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.layla.android.data.api.RetrofitClient
import com.layla.android.data.local.SessionManager
import com.layla.android.data.repository.AuthRepository
import com.layla.android.ui.auth.AuthViewModel
import com.layla.android.ui.auth.AuthViewModelFactory
import com.layla.android.ui.auth.LoginScreen
import com.layla.android.ui.auth.RegisterScreen
import com.layla.android.ui.theme.LaylaAndroidTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        
        val sessionManager = SessionManager(this)
        val repository = AuthRepository(RetrofitClient.authApiService)
        val viewModelFactory = AuthViewModelFactory(repository, sessionManager)

        setContent {
            LaylaAndroidTheme {
                val navController = rememberNavController()
                val authViewModel: AuthViewModel = viewModel(factory = viewModelFactory)

                NavHost(navController = navController, startDestination = "login") {
                    composable("login") {
                        LoginScreen(
                            viewModel = authViewModel,
                            onNavigateToRegister = { navController.navigate("register") },
                            onLoginSuccess = {
                                val token = sessionManager.fetchAuthToken()
                                Toast.makeText(this@MainActivity, "Login Successful! Token saved.", Toast.LENGTH_SHORT).show()
                                // Navigate to Home/Dashboard when implemented
                            }
                        )
                    }
                    composable("register") {
                        RegisterScreen(
                            viewModel = authViewModel,
                            onNavigateToLogin = { navController.popBackStack() },
                            onRegisterSuccess = {
                                Toast.makeText(this@MainActivity, "Registration Successful! Please login.", Toast.LENGTH_SHORT).show()
                                navController.navigate("login") {
                                    popUpTo("register") { inclusive = true }
                                }
                            }
                        )
                    }
                }
            }
        }
    }
}
