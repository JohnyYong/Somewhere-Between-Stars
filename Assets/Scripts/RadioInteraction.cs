using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class RadioInteraction : MonoBehaviour
{
    [Header("BGM Tracks")]
    public AudioClip[] bgmTracks;
    public float crossfadeDuration = 2f;

    [Header("Custom Music")]
    public CustomMusicLoader customMusicLoader;

    [Header("Sounds")]
    public AudioClip radioTuningStatic;
    public AudioClip beepSound;
    public float staticVolume = 0.7f;
    public float beepVolume = 0.5f;

    [Header("3D Audio Settings")]
    public float minDistance = 0.5f;    // full volume within this range
    public float maxDistance = 5f;      // silent beyond this range

    [Header("References")]
    public Camera playerCamera;
    public AudioManager audioManager;

    [Header("Audio Mixing")]
    public AudioMixerGroup sfxMixerGroup;

    // Combined list: bgmTracks + everything loaded from the custom music folder
    private List<AudioClip> _allTracks = new List<AudioClip>();
    public IReadOnlyList<AudioClip> AllTracks => _allTracks;
    public IReadOnlyList<string> AllTrackNames { get; private set; } = new List<string>();

    public int CurrentTrackIndex => _currentTrack;

    //Fired once a track has finished fading in, playlist UI listens to this to highlight the active row
    public event Action<int> OnTrackChanged;

    private int _currentTrack = 0;
    private bool _isSwitching = false;
    private bool _isReady = false; //Guards interaction until tracks (built-in + custom) finish loading
    private AudioSource _radioSource;   //3D source on the radio itself

    void Start()
    {
        _radioSource = gameObject.AddComponent<AudioSource>();
        _radioSource.spatialBlend = 1f;         // fully 3D
        _radioSource.rolloffMode = AudioRolloffMode.Custom;
        _radioSource.minDistance = minDistance;
        _radioSource.maxDistance = maxDistance;
        _radioSource.playOnAwake = false;
        _radioSource.loop = false;

        if (sfxMixerGroup != null)
        {
            _radioSource.outputAudioMixerGroup = sfxMixerGroup;
        }

        AnimationCurve rolloff = new AnimationCurve();
        rolloff.AddKey(0f, 1f);
        rolloff.AddKey(0.1f, 0.8f);
        rolloff.AddKey(0.5f, 0.3f);
        rolloff.AddKey(1f, 0f);
        _radioSource.SetCustomCurve(
            AudioSourceCurveType.CustomRolloff, rolloff);

        StartCoroutine(BuildTrackList());
    }

    void OnDestroy()
    {
        if (customMusicLoader != null)
            customMusicLoader.OnTracksChanged -= HandleCustomTracksChanged;
    }

    IEnumerator BuildTrackList()
    {
        RebuildFromBuiltIn();

        if (customMusicLoader != null)
        {
            yield return StartCoroutine(customMusicLoader.LoadAllTracks());
            AppendCustomTracks();

            // Live-update whenever the player adds a new track through the UI
            customMusicLoader.OnTracksChanged += HandleCustomTracksChanged;
        }

        _isReady = _allTracks.Count > 0;

        if (!_isReady)
            Debug.LogWarning("RadioInteraction: no tracks available (built-in or custom).");
    }

    void RebuildFromBuiltIn()
    {
        _allTracks.Clear();
        _allTracks.AddRange(bgmTracks);

        var names = new List<string>();
        foreach (var clip in bgmTracks)
            names.Add(clip != null ? clip.name : "Unknown");

        AllTrackNames = names;
    }

    void AppendCustomTracks()
    {
        if (customMusicLoader == null) return;

        _allTracks.AddRange(customMusicLoader.LoadedClips);

        var names = new List<string>(AllTrackNames);
        names.AddRange(customMusicLoader.LoadedClipNames);
        AllTrackNames = names;
    }

    // Called when CustomMusicLoader finishes copying/decoding a newly added track
    void HandleCustomTracksChanged()
    {
        RebuildFromBuiltIn();
        AppendCustomTracks();
        _isReady = _allTracks.Count > 0;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !_isSwitching && _isReady)
            TryInteract();
    }

    void TryInteract()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null &&
        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 60f))
        {
            if (hit.collider.CompareTag("Radio"))
                PlayTrack((_currentTrack + 1) % _allTracks.Count);
        }
    }

    // Public entry point — used by both the physical radio click AND the playlist UI rows
    public void PlayTrack(int index)
    {
        if (_isSwitching || !_isReady) return;
        if (index < 0 || index >= _allTracks.Count) return;
        if (index == _currentTrack) return; // already playing this track

        StartCoroutine(SwitchStation(index));
    }

    IEnumerator SwitchStation(int targetIndex)
    {
        _isSwitching = true;

        if (radioTuningStatic != null)
            _radioSource.PlayOneShot(radioTuningStatic, staticVolume);

        yield return StartCoroutine(
            audioManager.FadeOutBGM(crossfadeDuration * 0.5f));

        float staticLength = radioTuningStatic != null ?
                             radioTuningStatic.length : 1f;
        yield return new WaitForSeconds(staticLength * 0.8f);

        if (beepSound != null)
            _radioSource.PlayOneShot(beepSound, beepVolume);

        yield return new WaitForSeconds(0.3f);

        _currentTrack = targetIndex;

        yield return StartCoroutine(
            audioManager.FadeInBGM(_allTracks[_currentTrack],
                                   crossfadeDuration));

        _isSwitching = false;
        OnTrackChanged?.Invoke(_currentTrack);
    }
}