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
    [SerializeField] private SlimeWobbleController _wobbleController;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private float charDelay = 0.03f;

    [Header("Greeting")]
    [Tooltip("Sent to the LLM automatically when a conversation begins, so the slime speaks first.")]
    [SerializeField] private string greetingPrompt = "The player has just sat down to talk with you. Greet them warmly in your usual manner.";

    private string _latestReply = "";
    private Coroutine _typewriterRoutine;
    private bool _isReplying = false;

    void Start()
    {
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnSubmit);
        }
    }

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

    public void EndConversation()
    {
        if (_typewriterRoutine != null)
        {
            StopCoroutine(_typewriterRoutine);
            _typewriterRoutine = null;
        }

        if (_wobbleController != null) _wobbleController.SetSpeaking(false);

        _isReplying = false;

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
        inputField.ActivateInputField();
    }

    public void SendToSlime(string userText)
    {
        _isReplying = true;
        _llmAgent.Chat(userText, HandleReply, ReplyComplete);
    }

    void HandleReply(string reply)
    {
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
        if (_wobbleController != null) _wobbleController.SetSpeaking(true);

        foreach (char c in fullText)
        {
            repliesText.text += c;
            yield return new WaitForSeconds(charDelay);
        }

        if (_wobbleController != null) _wobbleController.SetSpeaking(false);
        _isReplying = false;
        _typewriterRoutine = null;
    }
}