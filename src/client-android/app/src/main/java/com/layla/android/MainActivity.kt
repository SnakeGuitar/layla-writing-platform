package com.layla.android

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
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
import com.layla.android.ui.projects.MyProjectsScreen
import com.layla.android.ui.projects.MyProjectsViewModel
import com.layla.android.ui.projects.MyProjectsViewModelFactory
import com.layla.android.ui.theme.LaylaAndroidTheme

private object Routes {
    const val LOGIN = "login"
    const val REGISTER = "register"
    const val MY_PROJECTS = "myProjects"
}

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        val sessionManager = SessionManager(this)

        setContent {
            LaylaAndroidTheme {
                LaylaNavGraph(sessionManager = sessionManager)
            }
        }
    }
}

@Composable
private fun LaylaNavGraph(sessionManager: SessionManager) {
    val navController = rememberNavController()

    val authRepository = remember { AuthRepository(RetrofitClient.authApiService) }
    val authVmFactory = remember { AuthViewModelFactory(authRepository, sessionManager) }
    val authViewModel: AuthViewModel = viewModel(factory = authVmFactory)

    val doLogout = {
        sessionManager.clearSession()
        RetrofitClient.setToken(null)
        navController.navigate(Routes.LOGIN) {
            popUpTo(0) { inclusive = true }
        }
    }

    NavHost(navController = navController, startDestination = Routes.LOGIN) {
        composable(Routes.LOGIN) {
            LoginScreen(
                viewModel = authViewModel,
                onNavigateToRegister = { navController.navigate(Routes.REGISTER) },
                onLoginSuccess = {
                    RetrofitClient.setToken(sessionManager.fetchAuthToken())
                    navController.navigate(Routes.MY_PROJECTS) {
                        popUpTo(Routes.LOGIN) { inclusive = true }
                    }
                }
            )
        }

        composable(Routes.REGISTER) {
            RegisterScreen(
                viewModel = authViewModel,
                onNavigateToLogin = { navController.popBackStack() },
                onRegisterSuccess = {
                    RetrofitClient.setToken(sessionManager.fetchAuthToken())
                    navController.navigate(Routes.MY_PROJECTS) {
                        popUpTo(Routes.REGISTER) { inclusive = true }
                    }
                }
            )
        }

        composable(Routes.MY_PROJECTS) {
            RetrofitClient.setToken(sessionManager.fetchAuthToken())

            val factory = remember {
                MyProjectsViewModelFactory(
                    projectApiService = RetrofitClient.projectApiService,
                    adminApiService = RetrofitClient.adminApiService
                )
            }
            val vm: MyProjectsViewModel = viewModel(factory = factory)

            MyProjectsScreen(
                viewModel = vm,
                onLogout = doLogout
            )
        }
    }
}
