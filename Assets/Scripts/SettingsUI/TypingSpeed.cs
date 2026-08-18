using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TypingSpeed : MonoBehaviour
{
    [Header("Speed Setting")]
    [Range(0.25f, 4f)]
    public float typingSpeed = 1f;

    [Header("Base Timing")]
    [SerializeField] private float baseCharDelay = 0.05f;

    [Header("Demo Preview")]
    [SerializeField] private TextMeshProUGUI animatingText;
    [SerializeField] private string demoText = "THIS IS YOUR REPLY SPEED.";
    [SerializeField] private float pauseBetweenLoops = 1f;

    [Header("Slider (optional, wire if the slider isn't hooked up elsewhere)")]
    [SerializeField] private Slider speedSlider;

    private Coroutine _demoRoutine;

    void Start()
    {
        if (speedSlider != null)
        {
            speedSlider.value = typingSpeed;
            speedSlider.onValueChanged.AddListener(OnSpeedChanged);
        }

        PlayDemo();
    }

    void OnDisable()
    {
        if (_demoRoutine != null)
        {
            StopCoroutine(_demoRoutine);
            _demoRoutine = null;
        }
    }

    public void OnSpeedChanged(float newSpeed)
    {
        typingSpeed = newSpeed;
        PlayDemo(); 
    }

    public float GetCharDelay()
    {
        return baseCharDelay / Mathf.Max(typingSpeed, 0.01f);
    }

    void PlayDemo()
    {
        if (animatingText == null) return;
        if (_demoRoutine != null) StopCoroutine(_demoRoutine);
        _demoRoutine = StartCoroutine(AnimateDemo());
    }

    IEnumerator AnimateDemo()
    {
        animatingText.text = "";
        float delay = GetCharDelay();

        foreach (char c in demoText)
        {
            animatingText.text += c;
            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(pauseBetweenLoops);
        _demoRoutine = StartCoroutine(AnimateDemo()); // loop so the player can keep testing
    }
}