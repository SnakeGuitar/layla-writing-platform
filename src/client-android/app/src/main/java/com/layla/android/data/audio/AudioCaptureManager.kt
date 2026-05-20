package com.layla.android.data.audio

import android.annotation.SuppressLint
import android.media.AudioFormat
import android.media.AudioRecord
import android.media.MediaRecorder
import kotlin.concurrent.thread

class AudioCaptureManager {
    private var audioRecord: AudioRecord? = null

    // @Volatile so the capture thread observes the writer's value without
    // synchronization — otherwise the loop can spin on a stale `true` while
    // stopCapture() releases the AudioRecord underneath it.
    @Volatile
    private var isRecording = false
    private var captureThread: Thread? = null

    companion object {
        const val SAMPLE_RATE = 16000
        const val CHANNEL_CONFIG = AudioFormat.CHANNEL_IN_MONO
        const val AUDIO_FORMAT = AudioFormat.ENCODING_PCM_16BIT
        const val FRAME_DURATION_MS = 20
        const val FRAME_SIZE = SAMPLE_RATE * 2 * FRAME_DURATION_MS / 1000 // 640 bytes per 20ms
    }

    @SuppressLint("MissingPermission")
    fun startCapture(onAudioData: (ByteArray) -> Unit) {
        if (isRecording) return

        val bufferSize = AudioRecord.getMinBufferSize(SAMPLE_RATE, CHANNEL_CONFIG, AUDIO_FORMAT)
            .coerceAtLeast(FRAME_SIZE * 2)

        val record = AudioRecord(
            MediaRecorder.AudioSource.MIC,
            SAMPLE_RATE,
            CHANNEL_CONFIG,
            AUDIO_FORMAT,
            bufferSize
        )
        audioRecord = record

        isRecording = true
        record.startRecording()

        captureThread = thread(name = "AudioCapture") {
            val buffer = ByteArray(FRAME_SIZE)
            while (isRecording) {
                val bytesRead = try {
                    record.read(buffer, 0, FRAME_SIZE)
                } catch (_: IllegalStateException) {
                    break
                }
                if (bytesRead > 0) {
                    onAudioData(buffer.copyOf(bytesRead))
                }
            }
        }
    }

    fun stopCapture() {
        if (!isRecording && audioRecord == null) return
        isRecording = false

        // Join the capture thread BEFORE touching the AudioRecord — releasing
        // while the thread is still inside `read()` causes a native crash.
        try {
            captureThread?.join(500)
        } catch (_: InterruptedException) {
            Thread.currentThread().interrupt()
        }
        captureThread = null

        audioRecord?.let { rec ->
            try { rec.stop() } catch (_: IllegalStateException) {}
            rec.release()
        }
        audioRecord = null
    }
}
