package com.layla.android.data.audio

import android.annotation.SuppressLint
import android.media.AudioFormat
import android.media.AudioRecord
import android.media.MediaRecorder
import kotlin.concurrent.thread

class AudioCaptureManager {
    private var audioRecord: AudioRecord? = null
    private var isRecording = false

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

        audioRecord = AudioRecord(
            MediaRecorder.AudioSource.MIC,
            SAMPLE_RATE,
            CHANNEL_CONFIG,
            AUDIO_FORMAT,
            bufferSize
        )

        isRecording = true
        audioRecord?.startRecording()

        thread(name = "AudioCapture") {
            val buffer = ByteArray(FRAME_SIZE)
            while (isRecording) {
                val bytesRead = audioRecord?.read(buffer, 0, FRAME_SIZE) ?: -1
                if (bytesRead > 0) {
                    onAudioData(buffer.copyOf(bytesRead))
                }
            }
        }
    }

    fun stopCapture() {
        isRecording = false
        audioRecord?.stop()
        audioRecord?.release()
        audioRecord = null
    }
}
