package com.layla.android

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.google.gson.Gson
import com.layla.android.data.api.PresenceSignalRClient
import com.layla.android.data.api.RetrofitClient
import com.layla.android.data.local.SessionManager
import com.layla.android.data.model.ProjectDto
import com.layla.android.data.repository.AuthRepository
import com.layla.android.ui.auth.AuthViewModel
import com.layla.android.ui.auth.AuthViewModelFactory
import com.layla.android.ui.auth.LoginScreen
import com.layla.android.ui.auth.RegisterScreen
import com.layla.android.ui.feed.ProjectFeedScreen
import com.layla.android.ui.feed.ProjectFeedViewModel
import com.layla.android.ui.feed.ProjectFeedViewModelFactory
import com.layla.android.ui.projects.MyProjectsScreen
import com.layla.android.ui.projects.MyProjectsViewModel
import com.layla.android.ui.projects.MyProjectsViewModelFactory
import com.layla.android.ui.reader.ReaderWorkspaceScreen
import com.layla.android.ui.reader.ReaderWorkspaceViewModel
import com.layla.android.ui.reader.ReaderWorkspaceViewModelFactory
import com.layla.android.ui.theme.LaylaAndroidTheme
import com.layla.android.ui.voice.VoiceRoomScreen
import com.layla.android.ui.voice.VoiceViewModel
import com.layla.android.ui.workspace.WorkspaceScreen
import com.layla.android.ui.workspace.WorkspaceViewModel
import com.layla.android.ui.workspace.WorkspaceViewModelFactory
import java.net.URLDecoder
import java.net.URLEncoder
import java.nio.charset.StandardCharsets
import com.layla.android.data.api.NetworkConfig


// ─── Route constants ──────────────────────────────────────────────────────────

private object Routes {
    const val LOGIN = "login"
    const val REGISTER = "register"
    const val MY_PROJECTS = "myProjects"
    const val PUBLIC_FEED = "feed"
    const val WORKSPACE = "workspace/{projectJson}"
    const val READER_WORKSPACE = "readerWorkspace/{projectJson}"
    const val VOICE_ROOM = "voiceRoom/{projectId}"

    fun workspace(project: ProjectDto): String {
        val json = URLEncoder.encode(Gson().toJson(project), StandardCharsets.UTF_8.toString())
        return "workspace/$json"
    }

    fun readerWorkspace(project: ProjectDto): String {
        val json = URLEncoder.encode(Gson().toJson(project), StandardCharsets.UTF_8.toString())
        return "readerWorkspace/$json"
    }

    fun voiceRoom(projectId: String) = "voiceRoom/$projectId"
}

// Server URLs are now centralized in NetworkConfig


// ─── MainActivity ─────────────────────────────────────────────────────────────

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

// ─── Navigation Graph ─────────────────────────────────────────────────────────

@Composable
private fun LaylaNavGraph(sessionManager: SessionManager) {
    val navController = rememberNavController()

    // Always start on Login — avoids issues with stale tokens
    val startDestination = Routes.LOGIN

    // Shared auth view model
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

    NavHost(navController = navController, startDestination = startDestination) {

        // ── Login ─────────────────────────────────────────────────────────────
        composable(Routes.LOGIN) {
            LoginScreen(
                viewModel = authViewModel,
                onNavigateToRegister = { navController.navigate(Routes.REGISTER) },
                onLoginSuccess = {
                    val token = sessionManager.fetchAuthToken()
                    RetrofitClient.setToken(token)
                    navController.navigate(Routes.MY_PROJECTS) {
                        popUpTo(Routes.LOGIN) { inclusive = true }
                    }
                }
            )
        }

        // ── Register ──────────────────────────────────────────────────────────
        composable(Routes.REGISTER) {
            RegisterScreen(
                viewModel = authViewModel,
                onNavigateToLogin = { navController.popBackStack() },
                onRegisterSuccess = {
                    val token = sessionManager.fetchAuthToken()
                    RetrofitClient.setToken(token)
                    navController.navigate(Routes.MY_PROJECTS) {
                        popUpTo(Routes.REGISTER) { inclusive = true }
                    }
                }
            )
        }

        // ── My Projects ───────────────────────────────────────────────────────
        composable(Routes.MY_PROJECTS) {
            val token = sessionManager.fetchAuthToken()
            RetrofitClient.setToken(token)

            val factory = remember { MyProjectsViewModelFactory(RetrofitClient.projectApiService) }
            val vm: MyProjectsViewModel = viewModel(factory = factory)

            MyProjectsScreen(
                viewModel = vm,
                onOpenProject = { project ->
                    // Author opens their own project → Workspace
                    navController.navigate(Routes.workspace(project))
                },
                onLogout = doLogout,
                onNavigateToPublicFeed = { navController.navigate(Routes.PUBLIC_FEED) }
            )
        }

        // ── Public Feed ───────────────────────────────────────────────────────
        composable(Routes.PUBLIC_FEED) {
            val token = sessionManager.fetchAuthToken()
            val factory = remember {
                ProjectFeedViewModelFactory(
                    projectApiService = RetrofitClient.projectApiService,
                    presenceClient = PresenceSignalRClient(NetworkConfig.BASE_URL_CORE),
                    token = token
                )
            }
            val vm: ProjectFeedViewModel = viewModel(factory = factory)

            ProjectFeedScreen(
                viewModel = vm,
                onProjectClick = { projectId ->
                    // Look up project from feed state and navigate to reader workspace
                    val feedState = vm.feedState.value
                    if (feedState is com.layla.android.ui.feed.FeedState.Success) {
                        val fp = feedState.projects.firstOrNull { it.dto.id == projectId }
                        fp?.let { navController.navigate(Routes.readerWorkspace(it.dto)) }
                    }
                }
            )
        }

        // ── Author Workspace ──────────────────────────────────────────────────
        composable(
            route = Routes.WORKSPACE,
            arguments = listOf(navArgument("projectJson") { type = NavType.StringType })
        ) { backStackEntry ->
            val json = URLDecoder.decode(
                backStackEntry.arguments?.getString("projectJson") ?: "",
                StandardCharsets.UTF_8.toString()
            )
            val project = Gson().fromJson(json, ProjectDto::class.java)
            val token = sessionManager.fetchAuthToken() ?: ""

            val factory = remember(project.id) {
                WorkspaceViewModelFactory(
                    project = project,
                    token = token,
                    baseUrl = NetworkConfig.BASE_URL_CORE,
                    projectApiService = RetrofitClient.projectApiService,
                    manuscriptApiService = RetrofitClient.manuscriptApiService,
                    wikiApiService = RetrofitClient.wikiApiService
                )
            }
            val vm: WorkspaceViewModel = viewModel(factory = factory)

            WorkspaceScreen(
                viewModel = vm,
                onBack = { navController.popBackStack() },
                onLogout = doLogout,
                onOpenVoiceRoom = { pid -> navController.navigate(Routes.voiceRoom(pid)) }
            )
        }

        // ── Reader Workspace ──────────────────────────────────────────────────
        composable(
            route = Routes.READER_WORKSPACE,
            arguments = listOf(navArgument("projectJson") { type = NavType.StringType })
        ) { backStackEntry ->
            val json = URLDecoder.decode(
                backStackEntry.arguments?.getString("projectJson") ?: "",
                StandardCharsets.UTF_8.toString()
            )
            val project = Gson().fromJson(json, ProjectDto::class.java)
            val token = sessionManager.fetchAuthToken()

            val factory = remember(project.id) {
                ReaderWorkspaceViewModelFactory(project, token, NetworkConfig.BASE_URL_CORE)
            }
            val vm: ReaderWorkspaceViewModel = viewModel(factory = factory)

            ReaderWorkspaceScreen(
                viewModel = vm,
                onBack = { navController.popBackStack() },
                onLogout = doLogout,
                onOpenVoiceRoom = { pid -> navController.navigate(Routes.voiceRoom(pid)) }
            )
        }

        // ── Voice Room ────────────────────────────────────────────────────────
        composable(
            route = Routes.VOICE_ROOM,
            arguments = listOf(navArgument("projectId") { type = NavType.StringType })
        ) { backStackEntry ->
            val projectId = backStackEntry.arguments?.getString("projectId") ?: ""
            val token = sessionManager.fetchAuthToken() ?: ""

            val vm: VoiceViewModel = viewModel(
                factory = object : androidx.lifecycle.ViewModelProvider.Factory {
                    override fun <T : androidx.lifecycle.ViewModel> create(modelClass: Class<T>): T {
                        @Suppress("UNCHECKED_CAST")
                        return VoiceViewModel(token, NetworkConfig.BASE_URL_CORE) as T
                    }
                }
            )

            VoiceRoomScreen(
                projectId = projectId,
                viewModel = vm,
                onBack = { navController.popBackStack() }
            )
        }
    }
}
