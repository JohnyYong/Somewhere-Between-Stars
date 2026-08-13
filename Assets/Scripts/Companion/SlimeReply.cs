using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using LLMUnity;
using TMPro;
using UnityEngine;

public class SlimeReply : MonoBehaviour
{
    [SerializeField] private LLMAgent _llmAgent;
    [SerializeField] private TextMeshProUGUI repliesText;
    [SerializeField] private RectTransform repliesPanel;

    [SerializeField] private SlimeWobbleController _wobbleController;
    [SerializeField] private SlimeMemoryManager _memoryManager;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private float charDelay = 0.03f;

    [Header("Panel Bounce")]
    [SerializeField] private float panelOpenDuration = 0.35f;
    [SerializeField] private float panelCloseDuration = 0.2f;

    [Header("Greeting")]
    [SerializeField] private string greetingPrompt = "The player has just sat down to talk with you. Greet them warmly in your usual manner.";


    private string _latestReply = "";
    private Coroutine _typewriterRoutine;
    private Coroutine _panelRoutine;
    private bool _isReplying = false;
    private bool _hasReceivedFirstToken = false;

    void Start()
    {
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnSubmit);
        }

        if (_memoryManager != null)
        {
            _memoryManager.OnMemoryCleared += HandleMemoryCleared;
        }

        // Start closed
        if (repliesPanel != null)
        {
            repliesPanel.localScale = Vector3.zero;
            repliesPanel.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (_memoryManager != null)
        {
            _memoryManager.OnMemoryCleared -= HandleMemoryCleared;
        }
    }

    // Reset the visible panel/text if memory gets cleared while it's open
    void HandleMemoryCleared()
    {
        if (_isReplying) return; // ClearMemory() already guards this, but stay safe

        repliesText.text = "";
        ClosePanel(instant: true);
    }

    // Call this when the player arrives at the companion (e.g. from SeatInteraction)
    public void BeginConversation()
    {
        if (_isReplying) return;
        SendToSlime(greetingPrompt);

        if (inputField != null)
        {
            inputField.text = "";
            inputField.ActivateInputField();
        }
    }

    // Call this when the player leaves the companion
    public void EndConversation()
    {
        if (_typewriterRoutine != null)
        {
            StopCoroutine(_typewriterRoutine);
            _typewriterRoutine = null;
        }

        if (_wobbleController != null)
        {
            _wobbleController.SetSpeaking(false);
            _wobbleController.SetThinking(false);
        }

        _isReplying = false;
        _hasReceivedFirstToken = false;
        SetInputEnabled(true);
        ClosePanel(instant: true);

        if (inputField != null)
        {
            inputField.DeactivateInputField();
        }
    }

    void OnSubmit(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText)) return;
        if (_isReplying) return;

        SendToSlime(userText);
        inputField.text = "";
    }

    public void SendToSlime(string userText)
    {
        _isReplying = true;
        _hasReceivedFirstToken = false;

        if (_llmAgent.llm != null)
        {
            _llmAgent.seed = Random.Range(0, int.MaxValue);
        }

        if (_wobbleController != null) _wobbleController.SetThinking(true);
        SetInputEnabled(false);
        ClosePanel();

        _llmAgent.Chat(userText, HandleReply, ReplyComplete);
    }
    void HandleReply(string reply)
    {
        if (!_hasReceivedFirstToken)
        {
            _hasReceivedFirstToken = true;
            if (_wobbleController != null) _wobbleController.SetThinking(false);
        }

        _latestReply = reply;
    }

    string TruncateToSentences(string text, int maxSentences = 2)
    {
        var sentences = Regex.Matches(text, @"[^.!?]+[.!?]+")
                              .Cast<Match>()
                              .Select(m => m.Value.Trim())
                              .ToList();

        if (sentences.Count == 0) return text.Trim();
        return string.Join(" ", sentences.Take(maxSentences));
    }

    void ReplyComplete()
    {
        string finalReply = TruncateToSentences(_latestReply, 2);
        PlayTypewriter(finalReply);
    }

    void PlayTypewriter(string fullText)
    {
        if (_typewriterRoutine != null) StopCoroutine(_typewriterRoutine);
        _typewriterRoutine = StartCoroutine(CharacterByCharacter(fullText));
    }

    IEnumerator CharacterByCharacter(string fullText)
    {
        repliesText.text = "";

        // Panel bounces open right as speaking/typing starts
        OpenPanel();
        if (_wobbleController != null) _wobbleController.SetSpeaking(true);

        foreach (char c in fullText)
        {
            repliesText.text += c;
            yield return new WaitForSeconds(charDelay);
        }

        if (_wobbleController != null) _wobbleController.SetSpeaking(false);

        _isReplying = false;
        SetInputEnabled(true);
        if (inputField != null) inputField.ActivateInputField();

        _typewriterRoutine = null;
    }

    void SetInputEnabled(bool enabled)
    {
        if (inputField == null) return;
        inputField.interactable = enabled;
    }

    // --- Panel bounce open/close ---
    void OpenPanel()
    {
        if (repliesPanel == null) return;
        if (_panelRoutine != null) StopCoroutine(_panelRoutine);

        repliesPanel.gameObject.SetActive(true);
        _panelRoutine = StartCoroutine(ScalePanel(Vector3.one, panelOpenDuration, easeOutBack: true));
    }

    void ClosePanel(bool instant = false)
    {
        if (repliesPanel == null) return;
        if (_panelRoutine != null) StopCoroutine(_panelRoutine);

        if (instant)
        {
            repliesPanel.localScale = Vector3.zero;
            repliesPanel.gameObject.SetActive(false);
            return;
        }

        _panelRoutine = StartCoroutine(ScalePanel(Vector3.zero, panelCloseDuration, easeOutBack: false, deactivateOnFinish: true));
    }

    IEnumerator ScalePanel(Vector3 targetScale, float duration, bool easeOutBack, bool deactivateOnFinish = false)
    {
        Vector3 startScale = repliesPanel.localScale;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            float eased = easeOutBack ? EaseOutBack(normalized) : EaseInBack(normalized);
            repliesPanel.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
            yield return null;
        }

        repliesPanel.localScale = targetScale;

        if (deactivateOnFinish)
        {
            repliesPanel.gameObject.SetActive(false);
        }

        _panelRoutine = null;
    }

    float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    float EaseInBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }
}