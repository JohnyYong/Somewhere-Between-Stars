using UnityEngine;
using UnityEngine.UI;

// Simple keyboard-driven test: press Space to start recording, press
// Space again to stop and transcribe, watch the Console for the result.
// Meant to be added alongside LocalWhisperSpeechToText on the same object.
public class LocalVoiceAgentSttKeyboardTester : MonoBehaviour
{
    [SerializeField] private LocalWhisperSpeechToText speechToText;
    void Awake()
    {
        if (speechToText == null) speechToText = GetComponent<LocalWhisperSpeechToText>();
    }

    void OnEnable()
    {
        if (speechToText != null)
        {
            speechToText.OnTranscriptionComplete += HandleTranscription;
            speechToText.OnRecordingStarted += HandleRecordingStarted;
            speechToText.OnRecordingStopped += HandleRecordingStopped;
        }
    }

    void OnDisable()
    {
        if (speechToText != null)
        {
            speechToText.OnTranscriptionComplete -= HandleTranscription;
            speechToText.OnRecordingStarted -= HandleRecordingStarted;
            speechToText.OnRecordingStopped -= HandleRecordingStopped;
        }
    }

    void Update()
    {
        if (speechToText == null) return;

        //if (Input.GetKeyDown(recordKey))
        //{
        //    if (speechToText.IsRecording)
        //        speechToText.StopRecording();
        //    else
        //        speechToText.StartRecording();
        //}
    }

    void HandleRecordingStarted()
    {
        Debug.Log("Recording started...");
    }

    void HandleRecordingStopped()
    {
        Debug.Log("Recording stopped, transcribing...");
    }

    void HandleTranscription(string text)
    {
        Debug.Log("Transcription: " + text);
    }
}
