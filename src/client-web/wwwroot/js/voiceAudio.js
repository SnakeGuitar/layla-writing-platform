let mediaStream = null;
let audioContext = null;
let processor = null;
let dotNetRef = null;
let captureId = 0;

const CAPTURE_BUFFER_SIZE = 1024;

export async function startCapture(dotNetReference) {
    stopCapture();
    const currentCaptureId = ++captureId;
    dotNetRef = dotNetReference;
    const context = new AudioContext({ sampleRate: 16000 });
    audioContext = context;

    const stream = await navigator.mediaDevices.getUserMedia({
        audio: {
            sampleRate: 16000,
            channelCount: 1,
            echoCancellation: true,
            noiseSuppression: true
        }
    });

    if (currentCaptureId !== captureId || audioContext !== context) {
        stream.getTracks().forEach(t => t.stop());
        await context.close();
        return;
    }

    mediaStream = stream;
    const source = context.createMediaStreamSource(stream);

    // ScriptProcessorNode for broad compatibility
    const currentProcessor = context.createScriptProcessor(CAPTURE_BUFFER_SIZE, 1, 1);
    processor = currentProcessor;
    currentProcessor.onaudioprocess = (e) => {
        if (currentCaptureId !== captureId || dotNetRef == null) return;

        const float32 = e.inputBuffer.getChannelData(0);
        const int16 = new Int16Array(float32.length);
        for (let i = 0; i < float32.length; i++) {
            const s = Math.max(-1, Math.min(1, float32[i]));
            int16[i] = s < 0 ? s * 0x8000 : s * 0x7FFF;
        }
        const bytes = new Uint8Array(int16.buffer);
        const base64 = btoa(String.fromCharCode.apply(null, bytes));
        dotNetRef.invokeMethodAsync('OnAudioCaptured', base64);
    };

    source.connect(currentProcessor);
    currentProcessor.connect(context.destination);
}

export function stopCapture() {
    captureId++;
    if (processor) {
        processor.disconnect();
        processor = null;
    }
    if (mediaStream) {
        mediaStream.getTracks().forEach(t => t.stop());
        mediaStream = null;
    }
    if (audioContext) {
        audioContext.close();
        audioContext = null;
    }
    dotNetRef = null;
}

let playbackContext = null;

export function initPlayback() {
    if (!playbackContext) {
        playbackContext = new AudioContext({ sampleRate: 16000 });
    }
}

export function playAudio(base64Data) {
    if (!playbackContext) return;

    const binary = atob(base64Data);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }

    const int16 = new Int16Array(bytes.buffer);
    const float32 = new Float32Array(int16.length);
    for (let i = 0; i < int16.length; i++) {
        float32[i] = int16[i] / 32768.0;
    }

    const buffer = playbackContext.createBuffer(1, float32.length, 16000);
    buffer.copyToChannel(float32, 0);
    const source = playbackContext.createBufferSource();
    source.buffer = buffer;
    source.connect(playbackContext.destination);
    source.start();
}

export function disposePlayback() {
    if (playbackContext) {
        playbackContext.close();
        playbackContext = null;
    }
}
