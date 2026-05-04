package com.layla.android.ui.workspace

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.People
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.layla.android.data.model.CollaboratorDto

// ─── Tabs definition ──────────────────────────────────────────────────────────

private enum class WorkspaceTab(val label: String) {
    MANUSCRIPT("Manuscript"),
    WIKI("Wiki"),
    VOICE("Voice")
}

// ─── WorkspaceScreen ─────────────────────────────────────────────────────────

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun WorkspaceScreen(
    viewModel: WorkspaceViewModel,
    onBack: () -> Unit,
    onLogout: () -> Unit,
    onOpenVoiceRoom: (String) -> Unit
) {
    val collabState by viewModel.collaboratorsState.collectAsState()
    val sessionDisplaced by viewModel.sessionDisplaced.collectAsState()
    var selectedTab by remember { mutableStateOf(WorkspaceTab.MANUSCRIPT) }

    // Session displaced dialog
    if (sessionDisplaced) {
        AlertDialog(
            onDismissRequest = {},
            title = { Text("Session Ended") },
            text = { Text("Your session was terminated because the account was logged in on another device.") },
            confirmButton = {
                TextButton(onClick = onLogout) { Text("OK") }
            }
        )
    }

    // Collaborators bottom sheet
    if (collabState.isVisible) {
        CollaboratorsSheet(
            state = collabState,
            onClose = viewModel::closeCollaborators,
            onInviteEmailChange = viewModel::setInviteEmail,
            onInvite = viewModel::inviteCollaborator,
            onRemove = viewModel::removeCollaborator
        )
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text(
                            viewModel.project.title,
                            fontWeight = FontWeight.Bold,
                            maxLines = 1,
                            overflow = TextOverflow.Ellipsis
                        )
                        Text(
                            viewModel.project.literaryGenre,
                            fontSize = 11.sp,
                            color = Color(0xFFF59E0B)
                        )
                    }
                },
                navigationIcon = {
                    TextButton(onClick = onBack) { Text("← Back") }
                },
                actions = {
                    IconButton(onClick = viewModel::openCollaborators) {
                        Icon(Icons.Default.People, contentDescription = "Collaborators", tint = Color(0xFFF59E0B))
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = Color(0xFF0C0A09),
                    titleContentColor = Color(0xFFF5F5F4)
                )
            )
        },
        containerColor = Color(0xFF1C1917)
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            // ─── Tab Row ──────────────────────────────────────────────────────
            TabRow(
                selectedTabIndex = selectedTab.ordinal,
                containerColor = Color(0xFF0C0A09),
                contentColor = Color(0xFFF59E0B)
            ) {
                WorkspaceTab.entries.forEach { tab ->
                    Tab(
                        selected = selectedTab == tab,
                        onClick = { selectedTab = tab },
                        text = {
                            Text(
                                tab.label,
                                fontSize = 13.sp,
                                fontWeight = if (selectedTab == tab) FontWeight.Bold else FontWeight.Normal
                            )
                        }
                    )
                }
            }

            // ─── Tab Content ──────────────────────────────────────────────────
            when (selectedTab) {
                WorkspaceTab.MANUSCRIPT -> ManuscriptEditorPane(
                    viewModel = viewModel.manuscriptViewModel
                )
                WorkspaceTab.WIKI -> WikiPane(
                    viewModel = viewModel.wikiViewModel
                )
                WorkspaceTab.VOICE -> VoiceTabPane(
                    projectId = viewModel.project.id,
                    onOpenVoiceRoom = onOpenVoiceRoom
                )
            }
        }
    }
}

// ─── Collaborators Bottom Sheet ───────────────────────────────────────────────

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun CollaboratorsSheet(
    state: CollaboratorsUiState,
    onClose: () -> Unit,
    onInviteEmailChange: (String) -> Unit,
    onInvite: () -> Unit,
    onRemove: (CollaboratorDto) -> Unit
) {
    ModalBottomSheet(onDismissRequest = onClose) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 20.dp)
                .padding(bottom = 32.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text("Collaborators", style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold)

            // Invite row
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                OutlinedTextField(
                    value = state.inviteEmail,
                    onValueChange = onInviteEmailChange,
                    label = { Text("Email") },
                    modifier = Modifier.weight(1f),
                    singleLine = true
                )
                Button(
                    onClick = onInvite,
                    enabled = !state.isInviting,
                    colors = ButtonDefaults.buttonColors(containerColor = Color(0xFFF59E0B), contentColor = Color(0xFF0C0A09))
                ) {
                    if (state.isInviting) {
                        CircularProgressIndicator(modifier = Modifier.size(16.dp), strokeWidth = 2.dp)
                    } else {
                        Icon(Icons.Default.Add, null, modifier = Modifier.size(18.dp))
                    }
                }
            }

            if (state.inviteError.isNotBlank()) {
                Text(state.inviteError, color = MaterialTheme.colorScheme.error, fontSize = 12.sp)
            }

            HorizontalDivider()

            if (state.isLoading) {
                CircularProgressIndicator(
                    modifier = Modifier.align(Alignment.CenterHorizontally),
                    color = Color(0xFFF59E0B)
                )
            } else {
                state.collaborators.forEach { collab ->
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column(modifier = Modifier.weight(1f)) {
                            Text(
                                collab.displayName ?: collab.email ?: collab.userId,
                                style = MaterialTheme.typography.bodyMedium,
                                fontWeight = FontWeight.Medium
                            )
                            Text(
                                collab.role,
                                fontSize = 11.sp,
                                color = Color(0xFF78716C)
                            )
                        }
                        IconButton(onClick = { onRemove(collab) }) {
                            Icon(Icons.Default.Delete, null, tint = MaterialTheme.colorScheme.error, modifier = Modifier.size(18.dp))
                        }
                    }
                }
            }
        }
    }
}

// ─── Voice Tab Pane ───────────────────────────────────────────────────────────

@Composable
private fun VoiceTabPane(
    projectId: String,
    onOpenVoiceRoom: (String) -> Unit
) {
    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        Column(horizontalAlignment = Alignment.CenterHorizontally, verticalArrangement = Arrangement.spacedBy(16.dp)) {
            Text(
                "Voice Room",
                style = MaterialTheme.typography.headlineSmall,
                color = Color(0xFFF5F5F4),
                fontWeight = FontWeight.Bold
            )
            Text(
                "Join the live voice channel for this project.",
                color = Color(0xFF78716C),
                fontSize = 14.sp
            )
            Button(
                onClick = { onOpenVoiceRoom(projectId) },
                colors = ButtonDefaults.buttonColors(
                    containerColor = Color(0xFFF59E0B),
                    contentColor = Color(0xFF0C0A09)
                )
            ) {
                Text("Join Voice Room", fontWeight = FontWeight.Bold)
            }
        }
    }
}
