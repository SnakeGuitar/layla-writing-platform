package com.layla.android.ui.voice

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.layla.android.data.api.ConnectionState
import com.layla.android.data.api.NetworkConfig
import com.layla.android.data.api.ParticipantInfo
import com.layla.android.data.api.VoiceSignalRClient
import com.layla.android.data.audio.AudioCaptureManager
import com.layla.android.data.audio.AudioPlaybackManager
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class VoiceUiState(
    val connectionState: ConnectionState = ConnectionState.DISCONNECTED,
    val isSpeaking: Boolean = false,
    val participants: List<ParticipantInfo> = emptyList(),
    val error: String? = null,
    val hasAudioPermission: Boolean = false
)

class VoiceViewModel(
    private val token: String,
    private val baseUrl: String = NetworkConfig.VOICE_HUB_BASE_URL
) : ViewModel() {

    private val signalRClient = VoiceSignalRClient(baseUrl)

    // Scope independent of viewModelScope so cleanup work in onCleared() can
    // actually run — viewModelScope is already cancelled by the time onCleared
    // fires, so any coroutine launched there is a no-op.
    private val cleanupScope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    private val audioCaptureManager = AudioCaptureManager()
    private val audioPlaybackManager = AudioPlaybackManager()

    private val _uiState = MutableStateFlow(VoiceUiState())
    val uiState: StateFlow<VoiceUiState> = _uiState.asStateFlow()

    private var currentProjectId: String? = null

    init {
        viewModelScope.launch {
            signalRClient.connectionState.collect { state ->
                _uiState.value = _uiState.value.copy(connectionState = state)
            }
        }
        viewModelScope.launch {
            signalRClient.participants.collect { participants ->
                _uiState.value = _uiState.value.copy(participants = participants)
            }
        }
        signalRClient.onAudioReceived = { _, audioData ->
            audioPlaybackManager.playAudio(audioData)
        }
    }

    fun setAudioPermission(granted: Boolean) {
        _uiState.value = _uiState.value.copy(hasAudioPermission = granted)
    }

    fun connect(projectId: String) {
        currentProjectId = projectId
        viewModelScope.launch(Dispatchers.IO) {
            try {
                signalRClient.connect(token)
                audioPlaybackManager.start()
                signalRClient.joinRoom(projectId)
                _uiState.value = _uiState.value.copy(error = null)
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(error = "Connection failed: ${e.message}")
            }
        }
    }

    fun leaveRoom() {
        viewModelScope.launch(Dispatchers.IO) {
            try {
                if (_uiState.value.isSpeaking) stopSpeaking()
                currentProjectId?.let { signalRClient.leaveRoom(it) }
                audioPlaybackManager.stop()
                signalRClient.disconnect()
            } catch (_: Exception) {}
        }
    }

    fun startSpeaking() {
        if (!_uiState.value.hasAudioPermission) return
        val projectId = currentProjectId ?: return

        viewModelScope.launch(Dispatchers.IO) {
            try {
                signalRClient.startSpeaking(projectId)
                _uiState.value = _uiState.value.copy(isSpeaking = true)
                audioCaptureManager.startCapture { audioData ->
                    signalRClient.sendAudio(projectId, audioData)
                }
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(error = e.message)
            }
        }
    }

    fun stopSpeaking() {
        val projectId = currentProjectId ?: return
        audioCaptureManager.stopCapture()
        _uiState.value = _uiState.value.copy(isSpeaking = false)

        viewModelScope.launch(Dispatchers.IO) {
            try {
                signalRClient.stopSpeaking(projectId)
            } catch (_: Exception) {}
        }
    }

    override fun onCleared() {
        super.onCleared()
        audioCaptureManager.stopCapture()
        audioPlaybackManager.stop()
        cleanupScope.launch {
            try { signalRClient.disconnect() } catch (_: Exception) {}
        }
    }
}
