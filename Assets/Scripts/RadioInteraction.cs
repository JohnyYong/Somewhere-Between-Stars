using UnityEngine;
using System.Collections;

public class RadioInteraction : MonoBehaviour
{
	[Header("BGM Tracks")]
	public AudioClip[] bgmTracks;
	public float crossfadeDuration = 2f;

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

	private int _currentTrack = 0;
	private bool _isSwitching = false;
	private AudioSource _radioSource;   // 3D source on the radio itself

	void Start()
	{
		// Create a 3D audio source on this object
		_radioSource = gameObject.AddComponent<AudioSource>();
		_radioSource.spatialBlend = 1f;         // fully 3D
		_radioSource.rolloffMode = AudioRolloffMode.Custom;
		_radioSource.minDistance = minDistance;
		_radioSource.maxDistance = maxDistance;
		_radioSource.playOnAwake = false;
		_radioSource.loop = false;

		// Set a nice rolloff curve — falls off naturally
		AnimationCurve rolloff = new AnimationCurve();
		rolloff.AddKey(0f, 1f);
		rolloff.AddKey(0.1f, 0.8f);
		rolloff.AddKey(0.5f, 0.3f);
		rolloff.AddKey(1f, 0f);
		_radioSource.SetCustomCurve(
			AudioSourceCurveType.CustomRolloff, rolloff);
	}

	void Update()
	{
		if (Input.GetMouseButtonDown(0) && !_isSwitching)
			TryInteract();
	}

	void TryInteract()
	{
		Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hit, 60f))
		{
			if (hit.collider.CompareTag("Radio"))
				StartCoroutine(SwitchStation());
		}
	}

	IEnumerator SwitchStation()
	{
		_isSwitching = true;

		// Step 1 — play static from radio position (3D)
		if (radioTuningStatic != null)
			_radioSource.PlayOneShot(radioTuningStatic, staticVolume);

		// Step 2 — fade out current BGM while static plays
		yield return StartCoroutine(
			audioManager.FadeOutBGM(crossfadeDuration * 0.5f));

		// Step 3 — wait for static to finish
		float staticLength = radioTuningStatic != null ?
							 radioTuningStatic.length : 1f;
		yield return new WaitForSeconds(staticLength * 0.8f);

		// Step 4 — beep from radio position (3D)
		if (beepSound != null)
			_radioSource.PlayOneShot(beepSound, beepVolume);

		// Step 5 — short pause after beep
		yield return new WaitForSeconds(0.3f);

		// Step 6 — switch and fade in next track
		_currentTrack = (_currentTrack + 1) % bgmTracks.Length;

		yield return StartCoroutine(
			audioManager.FadeInBGM(bgmTracks[_currentTrack],
								   crossfadeDuration));

		_isSwitching = false;
	}
}