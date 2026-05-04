package com.layla.android.ui.projects

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.layla.android.data.model.ProjectDto

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MyProjectsScreen(
    viewModel: MyProjectsViewModel,
    onOpenProject: (ProjectDto) -> Unit,
    onLogout: () -> Unit,
    onNavigateToPublicFeed: () -> Unit
) {
    val state by viewModel.state.collectAsState()
    val showCreate by viewModel.showCreateDialog.collectAsState()
    val showEdit by viewModel.showEditDialog.collectAsState()
    val createForm by viewModel.createForm.collectAsState()
    val editForm by viewModel.editForm.collectAsState()
    val sessionDisplaced by viewModel.sessionDisplaced.collectAsState()

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

    // ─── Create Dialog ──────────────────────────────────────────────────────
    if (showCreate) {
        ProjectFormDialog(
            title = "New Project",
            form = createForm,
            onDismiss = viewModel::closeCreateDialog,
            onFormChange = viewModel::updateCreateForm,
            onConfirm = viewModel::createProject
        )
    }

    // ─── Edit Dialog ────────────────────────────────────────────────────────
    if (showEdit) {
        ProjectFormDialog(
            title = "Edit Project",
            form = editForm,
            onDismiss = viewModel::closeEditDialog,
            onFormChange = viewModel::updateEditForm,
            onConfirm = viewModel::updateProject
        )
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        "My Projects",
                        fontWeight = FontWeight.Bold
                    )
                },
                actions = {
                    TextButton(onClick = onNavigateToPublicFeed) {
                        Text("Explore", color = Color(0xFFF59E0B))
                    }
                    TextButton(onClick = onLogout) {
                        Text("Logout", color = MaterialTheme.colorScheme.error)
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = Color(0xFF0C0A09),
                    titleContentColor = Color(0xFFF5F5F4)
                )
            )
        },
        floatingActionButton = {
            FloatingActionButton(
                onClick = viewModel::openCreateDialog,
                containerColor = Color(0xFFF59E0B),
                contentColor = Color(0xFF0C0A09)
            ) {
                Icon(Icons.Default.Add, contentDescription = "New Project")
            }
        },
        containerColor = Color(0xFF1C1917)
    ) { padding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            when (state) {
                is MyProjectsState.Loading -> {
                    CircularProgressIndicator(
                        modifier = Modifier.align(Alignment.Center),
                        color = Color(0xFFF59E0B)
                    )
                }

                is MyProjectsState.Error -> {
                    Column(
                        modifier = Modifier.align(Alignment.Center),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text(
                            text = (state as MyProjectsState.Error).message,
                            color = MaterialTheme.colorScheme.error
                        )
                        Spacer(modifier = Modifier.height(12.dp))
                        Button(onClick = viewModel::loadProjects) { Text("Retry") }
                    }
                }

                is MyProjectsState.Success -> {
                    val projects = (state as MyProjectsState.Success).projects

                    if (projects.isEmpty()) {
                        Column(
                            modifier = Modifier.align(Alignment.Center),
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Text(
                                "No projects yet.",
                                color = Color(0xFF78716C),
                                fontSize = 16.sp
                            )
                            Spacer(modifier = Modifier.height(8.dp))
                            Text(
                                "Tap + to create your first project.",
                                color = Color(0xFF57534E),
                                fontSize = 13.sp
                            )
                        }
                    } else {
                        LazyColumn(
                            modifier = Modifier.fillMaxSize(),
                            contentPadding = PaddingValues(16.dp),
                            verticalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            items(projects) { project ->
                                MyProjectCard(
                                    project = project,
                                    onOpen = { onOpenProject(project) },
                                    onEdit = { viewModel.openEditDialog(project) },
                                    onDelete = { viewModel.deleteProject(project) }
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

// ─── Project Card ────────────────────────────────────────────────────────────

@Composable
private fun MyProjectCard(
    project: ProjectDto,
    onOpen: () -> Unit,
    onEdit: () -> Unit,
    onDelete: () -> Unit
) {
    var showDeleteConfirm by remember { mutableStateOf(false) }

    if (showDeleteConfirm) {
        AlertDialog(
            onDismissRequest = { showDeleteConfirm = false },
            title = { Text("Delete Project") },
            text = { Text("Are you sure you want to delete '${project.title}'? This action cannot be undone.") },
            confirmButton = {
                TextButton(onClick = {
                    showDeleteConfirm = false
                    onDelete()
                }) {
                    Text("Delete", color = MaterialTheme.colorScheme.error)
                }
            },
            dismissButton = {
                TextButton(onClick = { showDeleteConfirm = false }) { Text("Cancel") }
            }
        )
    }

    Card(
        onClick = onOpen,
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = Color(0xFF0C0A09)),
        shape = MaterialTheme.shapes.extraSmall
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                // Genre badge
                Surface(
                    color = Color(0xFFF59E0B),
                    shape = MaterialTheme.shapes.extraSmall
                ) {
                    Text(
                        text = project.literaryGenre.uppercase(),
                        modifier = Modifier.padding(horizontal = 6.dp, vertical = 2.dp),
                        fontSize = 9.sp,
                        fontWeight = FontWeight.Bold,
                        color = Color(0xFF0C0A09),
                        letterSpacing = 1.5.sp
                    )
                }

                // Visibility badge
                Surface(
                    color = if (project.isPublic) Color(0xFF166534) else Color(0xFF292524),
                    shape = MaterialTheme.shapes.extraSmall
                ) {
                    Text(
                        text = if (project.isPublic) "PUBLIC" else "PRIVATE",
                        modifier = Modifier.padding(horizontal = 6.dp, vertical = 2.dp),
                        fontSize = 9.sp,
                        fontWeight = FontWeight.Bold,
                        color = if (project.isPublic) Color(0xFF4ADE80) else Color(0xFF78716C),
                        letterSpacing = 1.5.sp
                    )
                }
            }

            Spacer(modifier = Modifier.height(10.dp))

            Text(
                text = project.title,
                style = MaterialTheme.typography.titleMedium,
                color = Color(0xFFF5F5F4),
                fontWeight = FontWeight.Bold
            )

            Spacer(modifier = Modifier.height(4.dp))

            Text(
                text = project.synopsis,
                style = MaterialTheme.typography.bodySmall,
                color = Color(0xFF78716C),
                maxLines = 2
            )

            Spacer(modifier = Modifier.height(12.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.End,
                verticalAlignment = Alignment.CenterVertically
            ) {
                IconButton(onClick = onEdit, modifier = Modifier.size(36.dp)) {
                    Icon(
                        Icons.Default.Edit,
                        contentDescription = "Edit",
                        tint = Color(0xFF78716C),
                        modifier = Modifier.size(18.dp)
                    )
                }
                IconButton(
                    onClick = { showDeleteConfirm = true },
                    modifier = Modifier.size(36.dp)
                ) {
                    Icon(
                        Icons.Default.Delete,
                        contentDescription = "Delete",
                        tint = MaterialTheme.colorScheme.error.copy(alpha = 0.7f),
                        modifier = Modifier.size(18.dp)
                    )
                }
            }
        }
    }
}

// ─── Project Form Dialog ──────────────────────────────────────────────────────

@Composable
fun ProjectFormDialog(
    title: String,
    form: ProjectFormState,
    onDismiss: () -> Unit,
    onFormChange: (ProjectFormState) -> Unit,
    onConfirm: () -> Unit
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(title) },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(
                    value = form.title,
                    onValueChange = { onFormChange(form.copy(title = it)) },
                    label = { Text("Title") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
                OutlinedTextField(
                    value = form.genre,
                    onValueChange = { onFormChange(form.copy(genre = it)) },
                    label = { Text("Literary Genre") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
                OutlinedTextField(
                    value = form.synopsis,
                    onValueChange = { onFormChange(form.copy(synopsis = it)) },
                    label = { Text("Synopsis") },
                    modifier = Modifier.fillMaxWidth(),
                    minLines = 2,
                    maxLines = 4
                )
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Checkbox(
                        checked = form.isPublic,
                        onCheckedChange = { onFormChange(form.copy(isPublic = it)) }
                    )
                    Text("Make project public")
                }
                if (form.error.isNotBlank()) {
                    Text(form.error, color = MaterialTheme.colorScheme.error, fontSize = 12.sp)
                }
            }
        },
        confirmButton = {
            Button(
                onClick = onConfirm,
                enabled = !form.isSaving
            ) {
                if (form.isSaving) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(16.dp),
                        strokeWidth = 2.dp,
                        color = MaterialTheme.colorScheme.onPrimary
                    )
                } else {
                    Text("Save")
                }
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text("Cancel") }
        }
    )
}
