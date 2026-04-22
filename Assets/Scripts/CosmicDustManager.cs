using System.Collections;
using UnityEngine;

public class CosmicDustManager : MonoBehaviour
{
	[Header("References")]
	public ParticleSystem cosmicDust;
	public Camera playerCamera;
	public Transform worldRoot;

	[Header("Base Dust Settings")]
	public float baseDensity = 15f;
	public float cloudDensity = 60f;

	[Header("Cloud Patches")]
	public float minCloudInterval = 45f;
	public float maxCloudInterval = 120f;
	public float cloudDuration = 8f;
	public float cloudFadeDuration = 3f;

	[Header("Parallax")]
	public float dustFollowSpeed = 0.3f;

	private ParticleSystem.EmissionModule _emission;
	private bool _inCloud = false;

	void Start()
	{
		if (playerCamera == null)
			playerCamera = Camera.main;

		_emission = cosmicDust.emission;
		_emission.rateOverTime = baseDensity;

		// Keep dust centered on camera
		cosmicDust.transform.position = playerCamera.transform.position;

		StartCoroutine(CloudPatchRoutine());
	}

	void Update()
	{
		// Keep dust box following camera loosely
		// so particles are always around the player
		Vector3 target = new Vector3(
			playerCamera.transform.position.x + 10,
			playerCamera.transform.position.y,
			playerCamera.transform.position.z);

		cosmicDust.transform.position = Vector3.Lerp(
			cosmicDust.transform.position,
			target,
			Time.deltaTime * dustFollowSpeed);
	}

	IEnumerator CloudPatchRoutine()
	{
		// Initial delay
		yield return new WaitForSeconds(Random.Range(20f, 40f));

		while (true)
		{
			float interval = Random.Range(minCloudInterval, maxCloudInterval);
			yield return new WaitForSeconds(interval);

			StartCoroutine(DenseCloudRoutine());
		}
	}

	IEnumerator DenseCloudRoutine()
	{
		_inCloud = true;

		// Fade in to dense cloud
		float elapsed = 0f;
		while (elapsed < cloudFadeDuration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / cloudFadeDuration;
			_emission.rateOverTime = Mathf.Lerp(
				baseDensity, cloudDensity, t);
			yield return null;
		}

		// Hold dense cloud
		yield return new WaitForSeconds(cloudDuration);

		// Fade back to base
		elapsed = 0f;
		while (elapsed < cloudFadeDuration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / cloudFadeDuration;
			_emission.rateOverTime = Mathf.Lerp(
				cloudDensity, baseDensity, t);
			yield return null;
		}

		_emission.rateOverTime = baseDensity;
		_inCloud = false;
	}
}