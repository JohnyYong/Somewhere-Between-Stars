using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	// Audio sources
	private AudioSource _trainSource;
	private AudioSource _trainSoftSource;
	private AudioSource _spaceSourceA;
	private AudioSource _spaceSourceB;
	private AudioSource _windowSource;
	private AudioSource _sfxSource;

	private int _currentSpaceClip = 0;
	private bool _usingSourceA = true;

	void Start()
	{
		// Create all audio sources
		_trainSource = CreateSource("TrainMain", trainInteriorVolume, true);
		_trainSoftSource = CreateSource("TrainSoft", trainInteriorVolume * 0.5f, true);
		_spaceSourceA = CreateSource("SpaceA", 0f, true);
		_spaceSourceB = CreateSource("SpaceB", 0f, true);
		_windowSource = CreateSource("Window", windowRattleVolume, true);
		_sfxSource = CreateSource("SFX", 1f, false);

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

	AudioSource CreateSource(string name, float volume, bool loop)
	{
		var go = new GameObject($"AudioSource_{name}");
		go.transform.parent = transform;
		var source = go.AddComponent<AudioSource>();
		source.volume = volume;
		source.loop = loop;
		source.spatialBlend = 0f; // 2D sound
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
}