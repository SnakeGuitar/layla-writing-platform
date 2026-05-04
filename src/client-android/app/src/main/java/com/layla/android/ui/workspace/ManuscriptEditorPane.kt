package com.layla.android.ui.workspace

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.layla.android.data.model.ChapterDto
import com.layla.android.data.model.ManuscriptDto
import kotlinx.coroutines.delay

// ─── ManuscriptEditorPane ────────────────────────────────────────────────────

@Composable
fun ManuscriptEditorPane(viewModel: ManuscriptEditorViewModel) {
    val state by viewModel.state.collectAsState()

    when (state) {
        is ManuscriptState.Loading -> {
            Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                CircularProgressIndicator(color = Color(0xFFF59E0B))
            }
        }

        is ManuscriptState.Error -> {
            Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Text((state as ManuscriptState.Error).message, color = MaterialTheme.colorScheme.error)
                    Spacer(modifier = Modifier.height(12.dp))
                    Button(onClick = viewModel::loadManuscripts) { Text("Retry") }
                }
            }
        }

        is ManuscriptState.Ready -> {
            val ready = state as ManuscriptState.Ready
            ManuscriptReadyPane(ready = ready, viewModel = viewModel)
        }
    }
}

@Composable
private fun ManuscriptReadyPane(
    ready: ManuscriptState.Ready,
    viewModel: ManuscriptEditorViewModel
) {
    var currentContent by remember(ready.currentChapter?.chapterId) {
        mutableStateOf(ready.currentChapter?.content ?: "")
    }

    // Auto-save with 3-second debounce
    LaunchedEffect(currentContent) {
        if (currentContent.isBlank()) return@LaunchedEffect
        delay(3_000)
        viewModel.saveContent(currentContent)
    }

    Row(modifier = Modifier.fillMaxSize()) {

        // ─── Sidebar ──────────────────────────────────────────────────────────
        Column(
            modifier = Modifier
                .width(180.dp)
                .fillMaxHeight()
                .background(Color(0xFF0C0A09))
                .padding(vertical = 8.dp)
        ) {
            // Manuscript selector
            Text(
                "MANUSCRIPTS",
                fontSize = 9.sp,
                fontWeight = FontWeight.Bold,
                color = Color(0xFFF59E0B),
                letterSpacing = 2.sp,
                modifier = Modifier.padding(horizontal = 12.dp, vertical = 4.dp)
            )

            ready.manuscripts.forEach { ms ->
                val isSelected = ms.manuscriptId == ready.selectedManuscript?.manuscriptId
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clickable { viewModel.selectManuscript(ms) }
                        .background(if (isSelected) Color(0xFF1C1917) else Color.Transparent)
                        .padding(horizontal = 12.dp, vertical = 8.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        ms.title,
                        fontSize = 12.sp,
                        color = if (isSelected) Color(0xFFF5F5F4) else Color(0xFF78716C),
                        modifier = Modifier.weight(1f)
                    )
                    if (isSelected && ready.manuscripts.size > 1) {
                        IconButton(
                            onClick = { viewModel.deleteManuscript(ms) },
                            modifier = Modifier.size(20.dp)
                        ) {
                            Icon(Icons.Default.Delete, null, tint = Color(0xFF57534E), modifier = Modifier.size(14.dp))
                        }
                    }
                }
            }

            // Add manuscript
            TextButton(
                onClick = viewModel::addManuscript,
                contentPadding = PaddingValues(horizontal = 12.dp, vertical = 4.dp)
            ) {
                Icon(Icons.Default.Add, null, tint = Color(0xFF57534E), modifier = Modifier.size(14.dp))
                Spacer(modifier = Modifier.width(4.dp))
                Text("New", fontSize = 11.sp, color = Color(0xFF57534E))
            }

            HorizontalDivider(color = Color(0xFF292524), modifier = Modifier.padding(vertical = 8.dp))

            // Chapter list
            Text(
                "CHAPTERS",
                fontSize = 9.sp,
                fontWeight = FontWeight.Bold,
                color = Color(0xFFF59E0B),
                letterSpacing = 2.sp,
                modifier = Modifier.padding(horizontal = 12.dp, vertical = 4.dp)
            )

            LazyColumn(modifier = Modifier.weight(1f)) {
                items(ready.chapters) { chapter ->
                    val isActive = chapter.chapterId == ready.currentChapter?.chapterId
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .clickable { viewModel.selectChapter(chapter) }
                            .background(if (isActive) Color(0xFF1C1917) else Color.Transparent)
                            .padding(horizontal = 12.dp, vertical = 8.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text(
                            chapter.title,
                            fontSize = 12.sp,
                            color = if (isActive) Color(0xFFF5F5F4) else Color(0xFF78716C),
                            modifier = Modifier.weight(1f)
                        )
                        if (ready.chapters.size > 1) {
                            IconButton(
                                onClick = { viewModel.deleteChapter(chapter) },
                                modifier = Modifier.size(20.dp)
                            ) {
                                Icon(Icons.Default.Delete, null, tint = Color(0xFF57534E), modifier = Modifier.size(14.dp))
                            }
                        }
                    }
                }
            }

            // Add chapter
            TextButton(
                onClick = viewModel::addChapter,
                contentPadding = PaddingValues(horizontal = 12.dp, vertical = 4.dp)
            ) {
                Icon(Icons.Default.Add, null, tint = Color(0xFF57534E), modifier = Modifier.size(14.dp))
                Spacer(modifier = Modifier.width(4.dp))
                Text("New Chapter", fontSize = 11.sp, color = Color(0xFF57534E))
            }
        }

        // ─── Editor Area ──────────────────────────────────────────────────────
        Column(
            modifier = Modifier
                .weight(1f)
                .fillMaxHeight()
        ) {
            // Status bar
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(Color(0xFF0C0A09))
                    .padding(horizontal = 16.dp, vertical = 6.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    ready.currentChapter?.title ?: "Select a chapter",
                    fontSize = 12.sp,
                    color = Color(0xFF78716C)
                )
                Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    if (ready.hasOfflineChanges) {
                        Text("● Unsaved", fontSize = 11.sp, color = Color(0xFFF59E0B))
                    }
                    if (ready.isSaving) {
                        CircularProgressIndicator(modifier = Modifier.size(12.dp), strokeWidth = 1.5.dp, color = Color(0xFF78716C))
                    }
                }
            }

            // Text editor
            if (ready.currentChapter != null) {
                TextField(
                    value = currentContent,
                    onValueChange = { currentContent = it },
                    modifier = Modifier
                        .fillMaxSize()
                        .weight(1f),
                    placeholder = { Text("Start writing...", color = Color(0xFF57534E)) },
                    colors = TextFieldDefaults.colors(
                        focusedContainerColor = Color(0xFF1C1917),
                        unfocusedContainerColor = Color(0xFF1C1917),
                        focusedTextColor = Color(0xFFF5F5F4),
                        unfocusedTextColor = Color(0xFFF5F5F4),
                        focusedIndicatorColor = Color.Transparent,
                        unfocusedIndicatorColor = Color.Transparent
                    ),
                    textStyle = MaterialTheme.typography.bodyMedium.copy(lineHeight = 24.sp)
                )
            } else {
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    Text("Select a chapter to start editing", color = Color(0xFF57534E))
                }
            }
        }
    }
}
