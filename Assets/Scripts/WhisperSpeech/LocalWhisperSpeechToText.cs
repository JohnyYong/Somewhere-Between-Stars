using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Whisper;
using Whisper.Utils;

// Wraps WhisperManager + MicrophoneRecord into a simple start/stop API
// with a single event for the finished transcription. Meant to sit on
// the same GameObject as those two components (per the setup steps),
// but will also auto-find them via GetComponent if left unassigned.
public class LocalWhisperSpeechToText : MonoBehaviour
{
    [SerializeField] private WhisperManager whisperManager;
    [SerializeField] private MicrophoneRecord microphoneRecord;
    [SerializeField] private TMP_InputField _inputField;

    public event Action<string> OnTranscriptionComplete;
    public event Action OnRecordingStarted;
    public event Action OnRecordingStopped;

    [SerializeField] private SlimeReply replyBrain;

    public bool IsRecording { get; private set; }

    void Awake()
    {
        if (whisperManager == null) whisperManager = GetComponent<WhisperManager>();
        if (microphoneRecord == null) microphoneRecord = GetComponent<MicrophoneRecord>();

        if (whisperManager == null)
            Debug.LogError("LocalWhisperSpeechToText: no WhisperManager found or assigned.");
        if (microphoneRecord == null)
            Debug.LogError("LocalWhisperSpeechToText: no MicrophoneRecord found or assigned.");
    }

    void OnEnable()
    {
        if (microphoneRecord != null)
            microphoneRecord.OnRecordStop += HandleRecordStop;
    }

    void OnDisable()
    {
        if (microphoneRecord != null)
            microphoneRecord.OnRecordStop -= HandleRecordStop;
    }

    public void RecordingTrigger()
    {
        if (IsRecording) { StopRecording(); }
        else { StartRecording(); }
    }

    public void StartRecording()
    {
        if (IsRecording) return;
        if (microphoneRecord == null) return;

        IsRecording = true;
        _inputField.DeactivateInputField();
        microphoneRecord.StartRecord();
        OnRecordingStarted?.Invoke();
    }

    public void StopRecording()
    {
        if (!IsRecording) return;
        if (microphoneRecord == null) return;

        IsRecording = false;
        
        microphoneRecord.StopRecord();
        OnRecordingStopped?.Invoke();
        _inputField.ActivateInputField();
        // Transcription kicks off once HandleRecordStop fires below,
        // after the recorded audio finishes flushing.
    }

    async void HandleRecordStop(AudioChunk recordedAudio)
    {
        if (whisperManager == null) return;

        var result = await whisperManager.GetTextAsync(
            recordedAudio.Data, recordedAudio.Frequency, recordedAudio.Channels);

        if (result == null || string.IsNullOrWhiteSpace(result.Result)) return;

        string text = result.Result.Trim();
        OnTranscriptionComplete?.Invoke(text);
        replyBrain.SendToSlime(text); //To answer
    }
}
