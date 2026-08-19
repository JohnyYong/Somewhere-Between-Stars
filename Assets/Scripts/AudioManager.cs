using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [Header("Train Interior")]
    public AudioClip[] trainInteriorClips;
    public AudioClip softTrainInterior;
    public float trainInteriorVolume = 0.4f;

    [Header("Train Whistles")]
    public AudioClip softWhistle;
    public AudioClip loudWhistle;
    public float minWhistleInterval = 120f;  // 2 minutes
    public float maxWhistleInterval = 360f;  // 6 minutes

    [Header("Space Ambience")]
    public AudioClip[] spaceAmbienceClips;
    public float spaceAmbienceVolume = 0.35f;
    public float crossfadeDuration = 3f;

    [Header("Wood Creaks")]
    public AudioClip[] woodCreakClips;
    public float minCreakInterval = 8f;
    public float maxCreakInterval = 25f;
    public float woodCreakVolume = 0.2f;

    [Header("Curtain Rustle")]
    public AudioClip[] curtainRustleClips;
    public float minRustleInterval = 15f;
    public float maxRustleInterval = 45f;
    public float curtainRustleVolume = 0.15f;

    [Header("Window Rattle")]
    public AudioClip windowRattle;
    public float windowRattleVolume = 0.18f;

    [Header("Audio Mixing")]
    [Tooltip("Ambient loops and one-shot environmental sounds (train, window, creaks, rustles, whistles) route here.")]
    public AudioMixerGroup sfxMixerGroup;
    [Tooltip("Space ambience tracks — what the radio crossfades between — route here.")]
    public AudioMixerGroup bgmMixerGroup;

    // Audio sources
    private AudioSource _trainSource;
    private AudioSource _trainSoftSource;
    private AudioSource _spaceSourceA;
    private AudioSource _spaceSourceB;
    private AudioSource _windowSource;
    private AudioSource _sfxSource;

    private int _currentSpaceClip = 0;
    private bool _usingSourceA = true;
    public static AudioManager instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // Create all audio sources, routed to the appropriate mixer group
        // so the Master/SFX/BGM sliders actually affect them.
        _trainSource = CreateSource("TrainMain", trainInteriorVolume, true, sfxMixerGroup);
        _trainSoftSource = CreateSource("TrainSoft", trainInteriorVolume * 0.5f, true, sfxMixerGroup);
        _spaceSourceA = CreateSource("SpaceA", 0f, true, bgmMixerGroup);
        _spaceSourceB = CreateSource("SpaceB", 0f, true, bgmMixerGroup);
        _windowSource = CreateSource("Window", windowRattleVolume, true, sfxMixerGroup);
        _sfxSource = CreateSource("SFX", 1f, false, sfxMixerGroup);

        // Start loops
        StartTrainAudio();
        StartWindowRattle();
        StartSpaceAmbience();

        // Start random triggers
        StartCoroutine(WhistleRoutine());
        StartCoroutine(WoodCreakRoutine());
        StartCoroutine(CurtainRustleRoutine());
        StartCoroutine(SpaceCrossfadeRoutine());
    }

    AudioSource CreateSource(string name, float volume, bool loop, AudioMixerGroup mixerGroup = null)
    {
        var go = new GameObject($"AudioSource_{name}");
        go.transform.parent = transform;
        var source = go.AddComponent<AudioSource>();
        source.volume = volume;
        source.loop = loop;
        source.spatialBlend = 0f; // 2D sound

        if (mixerGroup != null)
        {
            source.outputAudioMixerGroup = mixerGroup;
        }

        return source;
    }

    void StartTrainAudio()
    {
        if (trainInteriorClips.Length > 0)
        {
            _trainSource.clip = trainInteriorClips[0];
            _trainSource.Play();
        }

        if (softTrainInterior != null)
        {
            _trainSoftSource.clip = softTrainInterior;
            _trainSoftSource.Play();
        }
    }

    void StartWindowRattle()
    {
        if (windowRattle == null) return;
        _windowSource.clip = windowRattle;
        _windowSource.Play();
    }

    void StartSpaceAmbience()
    {
        if (spaceAmbienceClips.Length == 0) return;
        _spaceSourceA.clip = spaceAmbienceClips[0];
        _spaceSourceA.volume = spaceAmbienceVolume;
        _spaceSourceA.Play();
        _usingSourceA = true;
    }

    // Crossfade between space ambience tracks
    IEnumerator SpaceCrossfadeRoutine()
    {
        while (true)
        {
            // Wait for current clip to near its end
            AudioSource current = _usingSourceA ? _spaceSourceA : _spaceSourceB;
            AudioSource next = _usingSourceA ? _spaceSourceB : _spaceSourceA;

            float waitTime = current.clip != null ?
                             current.clip.length - crossfadeDuration - 2f : 60f;
            waitTime = Mathf.Max(waitTime, 30f);

            yield return new WaitForSeconds(waitTime);

            // Pick next clip
            _currentSpaceClip = (_currentSpaceClip + 1) % spaceAmbienceClips.Length;
            next.clip = spaceAmbienceClips[_currentSpaceClip];
            next.volume = 0f;
            next.Play();

            // Crossfade
            float elapsed = 0f;
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / crossfadeDuration;
                current.volume = Mathf.Lerp(spaceAmbienceVolume, 0f, t);
                next.volume = Mathf.Lerp(0f, spaceAmbienceVolume, t);
                yield return null;
            }

            current.Stop();
            _usingSourceA = !_usingSourceA;
        }
    }

    // Random whistle triggers
    IEnumerator WhistleRoutine()
    {
        yield return new WaitForSeconds(Random.Range(60f, 120f)); // first whistle delay

        while (true)
        {
            float interval = Random.Range(minWhistleInterval, maxWhistleInterval);
            yield return new WaitForSeconds(interval);

            // 80% soft whistle, 20% loud
            AudioClip whistle = Random.value < 0.8f ? softWhistle : loudWhistle;
            if (whistle != null)
                _sfxSource.PlayOneShot(whistle, 0.5f);
        }
    }

    // Random wood creaks
    IEnumerator WoodCreakRoutine()
    {
        while (true)
        {
            float interval = Random.Range(minCreakInterval, maxCreakInterval);
            yield return new WaitForSeconds(interval);

            if (woodCreakClips.Length > 0)
            {
                var clip = woodCreakClips[Random.Range(0, woodCreakClips.Length)];
                _sfxSource.PlayOneShot(clip, woodCreakVolume);
            }
        }
    }

    // Random curtain rustles
    IEnumerator CurtainRustleRoutine()
    {
        while (true)
        {
            float interval = Random.Range(minRustleInterval, maxRustleInterval);
            yield return new WaitForSeconds(interval);

            if (curtainRustleClips.Length > 0)
            {
                var clip = curtainRustleClips[Random.Range(0, curtainRustleClips.Length)];
                _sfxSource.PlayOneShot(clip, curtainRustleVolume);
            }
        }
    }

    public void PlaySFX(AudioClip clip, float volume)
    {
        _sfxSource.PlayOneShot(clip, volume);
    }

    public IEnumerator FadeOutBGM(float duration)
    {
        float startVolume = _spaceSourceA.volume;
        float elapsed = 0f;

        AudioSource active = _usingSourceA ? _spaceSourceA : _spaceSourceB;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            active.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        active.Stop();
    }
    void OnApplicationFocus(bool hasFocus)
    {
        // Focus-based ducking removed — it was hardcoding AudioListener.volume,
        // which fought with the player's Master slider (routed through the
        // Mixer's Master group via AudioSettingsManager). The Mixer's
        // MasterVolume parameter is now the single source of truth for
        // master volume. If focus-ducking is wanted back, it should be done
        // as a temporary attenuation ON TOP of the player's saved slider
        // value via AudioSettingsManager, not by overwriting AudioListener.volume directly.
    }
    public IEnumerator FadeInBGM(AudioClip clip, float duration)
    {
        AudioSource next = _usingSourceA ? _spaceSourceB : _spaceSourceA;
        next.clip = clip;
        next.volume = 0f;
        next.Play();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            next.volume = Mathf.Lerp(0f, spaceAmbienceVolume, elapsed / duration);
            yield return null;
        }

        next.volume = spaceAmbienceVolume;
        _usingSourceA = !_usingSourceA;
    }
}