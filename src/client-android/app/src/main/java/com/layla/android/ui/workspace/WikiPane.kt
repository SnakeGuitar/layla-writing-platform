package com.layla.android.ui.workspace

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Save
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.layla.android.data.model.WikiEntryDto

// ─── WikiPane ────────────────────────────────────────────────────────────────

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun WikiPane(viewModel: WikiViewModel) {
    val state by viewModel.state.collectAsState()

    Row(modifier = Modifier.fillMaxSize()) {

        // ─── Entity list sidebar ──────────────────────────────────────────────
        Column(
            modifier = Modifier
                .width(180.dp)
                .fillMaxHeight()
                .background(Color(0xFF0C0A09))
                .padding(vertical = 8.dp)
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 12.dp, vertical = 4.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    "ENTITIES",
                    fontSize = 9.sp,
                    fontWeight = FontWeight.Bold,
                    color = Color(0xFFF59E0B),
                    letterSpacing = 2.sp
                )
                IconButton(
                    onClick = viewModel::newEntry,
                    modifier = Modifier.size(24.dp)
                ) {
                    Icon(Icons.Default.Add, null, tint = Color(0xFFF59E0B), modifier = Modifier.size(16.dp))
                }
            }

            if (state.isLoading) {
                CircularProgressIndicator(
                    modifier = Modifier
                        .align(Alignment.CenterHorizontally)
                        .padding(top = 16.dp),
                    color = Color(0xFFF59E0B)
                )
            } else {
                // Group entries by entity type
                val grouped = state.entries.groupBy { it.entityType }
                LazyColumn {
                    grouped.forEach { (type, entries) ->
                        item {
                            Text(
                                type.uppercase(),
                                fontSize = 8.sp,
                                fontWeight = FontWeight.Bold,
                                color = Color(0xFF57534E),
                                letterSpacing = 1.sp,
                                modifier = Modifier.padding(start = 12.dp, top = 8.dp, bottom = 2.dp)
                            )
                        }
                        items(entries) { entry ->
                            WikiEntryRow(
                                entry = entry,
                                isSelected = entry.entityId == state.selectedEntry?.entityId,
                                onClick = { viewModel.selectEntry(entry) }
                            )
                        }
                    }
                }
            }
        }

        // ─── Editor pane ──────────────────────────────────────────────────────
        Column(
            modifier = Modifier
                .weight(1f)
                .fillMaxHeight()
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            // Title row
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    if (state.selectedEntry != null) "Edit Entity" else "New Entity",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = Color(0xFFF5F5F4)
                )
                Row(horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                    if (state.selectedEntry != null) {
                        IconButton(onClick = viewModel::delete) {
                            Icon(Icons.Default.Delete, null, tint = MaterialTheme.colorScheme.error)
                        }
                    }
                    IconButton(onClick = viewModel::save, enabled = !state.isSaving) {
                        if (state.isSaving) {
                            CircularProgressIndicator(modifier = Modifier.size(18.dp), strokeWidth = 2.dp, color = Color(0xFFF59E0B))
                        } else {
                            Icon(Icons.Default.Save, null, tint = Color(0xFFF59E0B))
                        }
                    }
                }
            }

            // Name
            OutlinedTextField(
                value = state.formName,
                onValueChange = { viewModel.updateForm(name = it) },
                label = { Text("Name") },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true
            )

            // Entity type dropdown
            var typeExpanded by remember { mutableStateOf(false) }
            ExposedDropdownMenuBox(
                expanded = typeExpanded,
                onExpandedChange = { typeExpanded = it }
            ) {
                OutlinedTextField(
                    value = state.formEntityType,
                    onValueChange = {},
                    readOnly = true,
                    label = { Text("Entity Type") },
                    trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = typeExpanded) },
                    modifier = Modifier
                        .menuAnchor()
                        .fillMaxWidth()
                )
                ExposedDropdownMenu(
                    expanded = typeExpanded,
                    onDismissRequest = { typeExpanded = false }
                ) {
                    EntityTypes.forEach { type ->
                        DropdownMenuItem(
                            text = { Text(type) },
                            onClick = {
                                viewModel.updateForm(entityType = type)
                                typeExpanded = false
                            }
                        )
                    }
                }
            }

            // Description
            OutlinedTextField(
                value = state.formDescription,
                onValueChange = { viewModel.updateForm(description = it) },
                label = { Text("Description") },
                modifier = Modifier.fillMaxWidth(),
                minLines = 3,
                maxLines = 6
            )

            // Tags
            OutlinedTextField(
                value = state.formTags,
                onValueChange = { viewModel.updateForm(tags = it) },
                label = { Text("Tags (comma separated)") },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true
            )

            // Error
            if (state.error.isNotBlank()) {
                Text(state.error, color = MaterialTheme.colorScheme.error, fontSize = 12.sp)
            }

            // Appearances
            if (state.appearances.isNotEmpty()) {
                HorizontalDivider(color = Color(0xFF292524))
                Text(
                    "APPEARANCES",
                    fontSize = 9.sp,
                    fontWeight = FontWeight.Bold,
                    color = Color(0xFFF59E0B),
                    letterSpacing = 2.sp
                )
                state.appearances.forEach { appearance ->
                    Text(
                        "• ${appearance.manuscriptTitle} › ${appearance.chapterTitle}",
                        fontSize = 12.sp,
                        color = Color(0xFF78716C)
                    )
                }
            }
        }
    }
}

// ─── Entry Row ────────────────────────────────────────────────────────────────

@Composable
private fun WikiEntryRow(
    entry: WikiEntryDto,
    isSelected: Boolean,
    onClick: () -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
            .background(if (isSelected) Color(0xFF1C1917) else Color.Transparent)
            .padding(horizontal = 12.dp, vertical = 8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            entry.name,
            fontSize = 12.sp,
            color = if (isSelected) Color(0xFFF5F5F4) else Color(0xFF78716C)
        )
    }
}
