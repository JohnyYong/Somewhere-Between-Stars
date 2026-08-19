using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// Drives Master/SFX/BGM sliders against an AudioMixer's exposed volume
// parameters. Sliders are linear (0-1) but AudioMixer volume is in
// decibels (logarithmic), so conversion happens here. Persists across
// sessions via PlayerPrefs.
public class AudioSettingsManager : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider bgmSlider;

    [Header("Mute")]
    [SerializeField] private Button muteButton;

    // Must exactly match the names you gave the exposed parameters in the
    // Audio Mixer's Exposed Parameters list.
    private const string MasterParam = "MasterVolume";
    private const string SFXParam = "SFXVolume";
    private const string BGMParam = "BGMVolume";

    private const string PrefKeyMaster = "VolumeMaster";
    private const string PrefKeySFX = "VolumeSFX";
    private const string PrefKeyBGM = "VolumeBGM";
    private const string PrefKeyMuted = "VolumeMuted";

    private const float MinDb = -80f; // effectively silent
    private const float MaxDb = 0f;   // full volume, 0dB = no attenuation

    private float _preMuteMasterValue = 1f;
    private bool _isMuted = false;

    void Awake()
    {
        // Loaded in Awake (not Start) so saved volumes are applied before
        // any other script's Start() has a chance to begin playing audio —
        // Awake always runs before Start across all scripts in the scene.
        LoadAndApplySavedVolumes();
    }

    void Start()
    {
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (muteButton != null) muteButton.onClick.AddListener(ToggleMute);
    }

    void LoadAndApplySavedVolumes()
    {
        float master = PlayerPrefs.GetFloat(PrefKeyMaster, 1f);
        float sfx = PlayerPrefs.GetFloat(PrefKeySFX, 1f);
        float bgm = PlayerPrefs.GetFloat(PrefKeyBGM, 1f);
        _isMuted = PlayerPrefs.GetInt(PrefKeyMuted, 0) == 1;

        if (masterSlider != null) masterSlider.value = master;
        if (sfxSlider != null) sfxSlider.value = sfx;
        if (bgmSlider != null) bgmSlider.value = bgm;

        ApplyVolume(MasterParam, _isMuted ? 0f : master);
        ApplyVolume(SFXParam, sfx);
        ApplyVolume(BGMParam, bgm);

        _preMuteMasterValue = master;
    }

    public void SetMasterVolume(float linearValue)
    {
        _preMuteMasterValue = linearValue;
        if (!_isMuted)
        {
            ApplyVolume(MasterParam, linearValue);
        }
        PlayerPrefs.SetFloat(PrefKeyMaster, linearValue);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float linearValue)
    {
        ApplyVolume(SFXParam, linearValue);
        PlayerPrefs.SetFloat(PrefKeySFX, linearValue);
        PlayerPrefs.Save();
    }

    public void SetBGMVolume(float linearValue)
    {
        ApplyVolume(BGMParam, linearValue);
        PlayerPrefs.SetFloat(PrefKeyBGM, linearValue);
        PlayerPrefs.Save();
    }

    public void ToggleMute()
    {
        _isMuted = !_isMuted;
        ApplyVolume(MasterParam, _isMuted ? 0f : _preMuteMasterValue);
        PlayerPrefs.SetInt(PrefKeyMuted, _isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Converts a linear 0-1 slider value into decibels and applies it to
    // the given exposed AudioMixer parameter.
    void ApplyVolume(string parameterName, float linearValue)
    {
        float clamped = Mathf.Clamp01(linearValue);

        // log10(0) is undefined (-infinity), so treat 0 as silence explicitly
        // rather than feeding it into the log conversion.
        float db = clamped <= 0.0001f ? MinDb : Mathf.Log10(clamped) * 20f;
        db = Mathf.Clamp(db, MinDb, MaxDb);

        audioMixer.SetFloat(parameterName, db);
    }
}
