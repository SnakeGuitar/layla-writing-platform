package com.layla.android.ui.reader

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.tween

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReaderWorkspaceScreen(
    viewModel: ReaderWorkspaceViewModel,
    onBack: () -> Unit,
    onLogout: () -> Unit,
    onOpenVoiceRoom: (String) -> Unit
) {
    val uiState by viewModel.uiState.collectAsState()

    val indicatorColor by animateColorAsState(
        targetValue = if (uiState.isAuthorActive) Color(0xFF34D399) else Color(0xFF44403C),
        animationSpec = tween(800),
        label = "authorPresence"
    )

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text(
                            viewModel.project.title,
                            fontWeight = FontWeight.Bold,
                            color = Color(0xFFF5F5F4)
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
        containerColor = Color(0xFF1C1917)
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(24.dp),
            verticalArrangement = Arrangement.spacedBy(24.dp)
        ) {

            // Author presence card
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(containerColor = Color(0xFF0C0A09))
            ) {
                Row(
                    modifier = Modifier.padding(16.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    Box(
                        modifier = Modifier
                            .size(12.dp)
                            .clip(CircleShape)
                            .background(indicatorColor)
                    )
                    Column {
                        Text(
                            text = uiState.authorStatusText,
                            color = if (uiState.isAuthorActive) Color(0xFF34D399) else Color(0xFF78716C),
                            fontWeight = FontWeight.Medium,
                            fontSize = 14.sp
                        )
                        Text(
                            text = "You are reading as a collaborator",
                            fontSize = 11.sp,
                            color = Color(0xFF57534E)
                        )
                    }
                }
            }

            // Synopsis card
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(containerColor = Color(0xFF0C0A09))
            ) {
                Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text(
                        "SYNOPSIS",
                        fontSize = 9.sp,
                        fontWeight = FontWeight.Bold,
                        color = Color(0xFFF59E0B),
                        letterSpacing = 2.sp
                    )
                    Text(
                        viewModel.project.synopsis,
                        color = Color(0xFF78716C),
                        lineHeight = 20.sp
                    )
                }
            }

            // Voice room entry
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(containerColor = Color(0xFF0C0A09))
            ) {
                Column(
                    modifier = Modifier.padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    Text(
                        "VOICE ROOM",
                        fontSize = 9.sp,
                        fontWeight = FontWeight.Bold,
                        color = Color(0xFFF59E0B),
                        letterSpacing = 2.sp
                    )
                    Text(
                        "Join the live voice channel to discuss this project in real-time.",
                        fontSize = 13.sp,
                        color = Color(0xFF78716C)
                    )
                    Button(
                        onClick = { onOpenVoiceRoom(viewModel.project.id) },
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
    }
}
