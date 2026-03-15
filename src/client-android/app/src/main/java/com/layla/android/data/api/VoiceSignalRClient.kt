package com.layla.android.data.api

import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.microsoft.signalr.HubConnectionState
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

data class ParticipantInfo(
    val userId: String,
    val displayName: String,
    val isSpeaking: Boolean,
    val role: String
)

enum class ConnectionState { DISCONNECTED, CONNECTING, CONNECTED }

class VoiceSignalRClient(private val baseUrl: String) {

    private var hubConnection: HubConnection? = null

    private val _connectionState = MutableStateFlow(ConnectionState.DISCONNECTED)
    val connectionState: StateFlow<ConnectionState> = _connectionState.asStateFlow()

    private val _participants = MutableStateFlow<List<ParticipantInfo>>(emptyList())
    val participants: StateFlow<List<ParticipantInfo>> = _participants.asStateFlow()

    var onAudioReceived: ((String, ByteArray) -> Unit)? = null

    fun connect(token: String) {
        if (hubConnection != null) return

        _connectionState.value = ConnectionState.CONNECTING

        hubConnection = HubConnectionBuilder
            .create("$baseUrl/hubs/voice?access_token=$token")
            .build()

        registerHandlers()

        hubConnection?.onClosed {
            _connectionState.value = ConnectionState.DISCONNECTED
        }

        hubConnection?.start()?.blockingAwait()
        _connectionState.value = ConnectionState.CONNECTED
    }

    fun joinRoom(projectId: String) {
        hubConnection?.invoke("JoinRoom", projectId)
    }

    fun leaveRoom(projectId: String) {
        hubConnection?.invoke("LeaveRoom", projectId)
        _participants.value = emptyList()
    }

    fun startSpeaking(projectId: String) {
        hubConnection?.invoke("StartSpeaking", projectId)
    }

    fun stopSpeaking(projectId: String) {
        hubConnection?.invoke("StopSpeaking", projectId)
    }

    fun sendAudio(projectId: String, audioData: ByteArray) {
        if (hubConnection?.connectionState == HubConnectionState.CONNECTED) {
            hubConnection?.invoke("SendAudio", projectId, audioData)
        }
    }

    fun disconnect() {
        hubConnection?.stop()?.blockingAwait()
        hubConnection = null
        _connectionState.value = ConnectionState.DISCONNECTED
        _participants.value = emptyList()
    }

    private fun registerHandlers() {
        hubConnection?.on("RoomState", { state: Any ->
            // Parse room state from the hub
            @Suppress("UNCHECKED_CAST")
            val map = state as? Map<String, Any> ?: return@on
            val participantsList = (map["participants"] as? List<Map<String, Any>>)?.map {
                ParticipantInfo(
                    userId = it["userId"] as? String ?: "",
                    displayName = it["displayName"] as? String ?: "",
                    isSpeaking = it["isSpeaking"] as? Boolean ?: false,
                    role = it["role"] as? String ?: ""
                )
            } ?: emptyList()
            _participants.value = participantsList
        }, Any::class.java)

        hubConnection?.on("UserJoined", { participant: Any ->
            @Suppress("UNCHECKED_CAST")
            val map = participant as? Map<String, Any> ?: return@on
            val info = ParticipantInfo(
                userId = map["userId"] as? String ?: "",
                displayName = map["displayName"] as? String ?: "",
                isSpeaking = map["isSpeaking"] as? Boolean ?: false,
                role = map["role"] as? String ?: ""
            )
            _participants.value = _participants.value + info
        }, Any::class.java)

        hubConnection?.on("UserLeft", { userId: String ->
            _participants.value = _participants.value.filter { it.userId != userId }
        }, String::class.java)

        hubConnection?.on("UserStartedSpeaking", { userId: String, _: String ->
            _participants.value = _participants.value.map {
                if (it.userId == userId) it.copy(isSpeaking = true) else it
            }
        }, String::class.java, String::class.java)

        hubConnection?.on("UserStoppedSpeaking", { userId: String ->
            _participants.value = _participants.value.map {
                if (it.userId == userId) it.copy(isSpeaking = false) else it
            }
        }, String::class.java)

        hubConnection?.on("ReceiveAudio", { senderId: String, audioData: ByteArray ->
            onAudioReceived?.invoke(senderId, audioData)
        }, String::class.java, ByteArray::class.java)
    }
}
