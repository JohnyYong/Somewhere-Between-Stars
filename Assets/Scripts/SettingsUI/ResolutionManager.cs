using UnityEngine;
using UnityEngine.UI;

// Handles a fixed, curated list of resolution buttons plus a
// Fullscreen/Windowed toggle. Persists the chosen settings across
// sessions and clamps requests against what the display can actually
// support, so a bad saved value can never leave the player with an
// unusable window.
public class ResolutionManager : MonoBehaviour
{
    [System.Serializable]
    public class ResolutionOption
    {
        public int width;
        public int height;
        public Button button;
    }

    [SerializeField] private ResolutionOption[] resolutionOptions;
    [SerializeField] private Button fullscreenButton;
    [SerializeField] private Button windowedButton;

    [Header("Selected State Visuals (optional)")]
    [SerializeField] private Color selectedColor = new Color(0.35f, 0.65f, 1f);
    [SerializeField] private Color normalColor = Color.white;

    private const string PrefKeyWidth = "ResolutionWidth";
    private const string PrefKeyHeight = "ResolutionHeight";
    private const string PrefKeyFullscreen = "ResolutionFullscreen";

    private int _currentWidth;
    private int _currentHeight;
    private bool _currentFullscreen;

    // Captured ONCE before any SetResolution call runs. Screen.currentResolution
    // reports whatever resolution is CURRENTLY applied while in fullscreen —
    // it drifts every time SetResolution runs, so we can't use it live as a
    // "max capability" ceiling or clamping would ratchet down to whatever
    // was last selected instead of the monitor's true maximum.
    private Resolution _trueNativeResolution;

    void Awake()
    {
        _trueNativeResolution = Screen.currentResolution;
        LoadAndApplySavedResolution();
    }

    void Start()
    {
        // Buttons are wired directly via their OnClick() events in the
        // Inspector, calling SetResolution(string) / SetFullscreen(bool)
        // below — no listeners added in code.
        RefreshButtonHighlights();
    }

    // Call from a resolution button's OnClick(), passing a string like
    // "1920x1080" as the dynamic parameter.
    public void SetResolution(string resolutionString)
    {
        var parts = resolutionString.Split('x');
        if (parts.Length != 2 || !int.TryParse(parts[0], out int width) || !int.TryParse(parts[1], out int height))
        {
            Debug.LogError($"ResolutionManager: could not parse resolution string '{resolutionString}'. Expected format like '1920x1080'.");
            return;
        }

        ApplyResolution(width, height, _currentFullscreen);
    }

    // Call from the Fullscreen button's OnClick() with true, and the
    // Windowed button's OnClick() with false.
    public void SetFullscreen(bool fullscreen)
    {
        ApplyResolution(_currentWidth, _currentHeight, fullscreen);
    }

    void LoadAndApplySavedResolution()
    {
        // Sensible fallback default if nothing's been saved yet: 1080p,
        // or the native resolution if the display is smaller than that.
        int defaultWidth = Mathf.Min(1920, _trueNativeResolution.width);
        int defaultHeight = Mathf.Min(1080, _trueNativeResolution.height);

        _currentWidth = PlayerPrefs.GetInt(PrefKeyWidth, defaultWidth);
        _currentHeight = PlayerPrefs.GetInt(PrefKeyHeight, defaultHeight);
        _currentFullscreen = PlayerPrefs.GetInt(PrefKeyFullscreen, 1) == 1; // default: fullscreen on

        ApplyResolutionInternal(_currentWidth, _currentHeight, _currentFullscreen, save: false);
    }

    public void ApplyResolution(int width, int height, bool fullscreen)
    {
        ApplyResolutionInternal(width, height, fullscreen, save: true);
        RefreshButtonHighlights();
    }

    void ApplyResolutionInternal(int width, int height, bool fullscreen, bool save)
    {
        // Safety: never request something larger than the display can
        // physically show. Clamped against the NATIVE resolution captured
        // once at startup — not the live Screen.currentResolution, which
        // would drift to match whatever was last applied.
        int safeWidth = Mathf.Clamp(width, 640, _trueNativeResolution.width);
        int safeHeight = Mathf.Clamp(height, 480, _trueNativeResolution.height);

        if (safeWidth != width || safeHeight != height)
        {
            Debug.LogWarning(
                $"Requested resolution {width}x{height} exceeds this display's capability ({_trueNativeResolution.width}x{_trueNativeResolution.height}). Clamped to {safeWidth}x{safeHeight}.");
        }

        // FullScreenWindow (borderless) is generally more stable across
        // multi-monitor setups and alt-tabbing than exclusive fullscreen.
        FullScreenMode mode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

        Screen.SetResolution(safeWidth, safeHeight, mode);

        _currentWidth = safeWidth;
        _currentHeight = safeHeight;
        _currentFullscreen = fullscreen;

        if (save)
        {
            PlayerPrefs.SetInt(PrefKeyWidth, safeWidth);
            PlayerPrefs.SetInt(PrefKeyHeight, safeHeight);
            PlayerPrefs.SetInt(PrefKeyFullscreen, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        Debug.Log($"Resolution applied: {safeWidth}x{safeHeight}, Fullscreen: {fullscreen}");
        StartCoroutine(LogActualResolutionNextFrame());
    }

    System.Collections.IEnumerator LogActualResolutionNextFrame()
    {
        yield return null; // Screen.width/height can lag a frame behind SetResolution
        Debug.Log($"Actual screen size after apply: {Screen.width}x{Screen.height}, cached native: {_trueNativeResolution.width}x{_trueNativeResolution.height} (live Screen.currentResolution: {Screen.currentResolution.width}x{Screen.currentResolution.height} — this drifts, don't use for clamping)");
    }

    void RefreshButtonHighlights()
    {
        foreach (var option in resolutionOptions)
        {
            if (option.button == null) continue;
            bool isSelected = option.width == _currentWidth && option.height == _currentHeight;
            SetButtonColor(option.button, isSelected ? selectedColor : normalColor);
        }

        if (fullscreenButton != null) SetButtonColor(fullscreenButton, _currentFullscreen ? selectedColor : normalColor);
        if (windowedButton != null) SetButtonColor(windowedButton, !_currentFullscreen ? selectedColor : normalColor);
    }

    void SetButtonColor(Button btn, Color color)
    {
        var image = btn.GetComponent<Image>();
        if (image != null) image.color = color;
    }
}