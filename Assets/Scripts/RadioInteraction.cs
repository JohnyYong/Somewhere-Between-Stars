using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

//Need to make it such that the users can add music which they want as well
//Maybe like a playlist maker / system for them as well

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

    [Header("Audio Mixing")]
    public AudioMixerGroup sfxMixerGroup;

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
	}

	void Update()
	{
		if (Input.GetMouseButtonDown(0) && !_isSwitching)
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
				StartCoroutine(SwitchStation());
		}
	}

	IEnumerator SwitchStation()
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

		_currentTrack = (_currentTrack + 1) % bgmTracks.Length;

		yield return StartCoroutine(
			audioManager.FadeInBGM(bgmTracks[_currentTrack],
								   crossfadeDuration));

		_isSwitching = false;
	}
}