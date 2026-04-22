using UnityEngine;

public class TrainController : MonoBehaviour
{
	[Header("Movement")]
	public float trainSpeed = 40f;
	public float swayAmount = 0.04f;
	public float swaySpeed = 0.8f;

	[Header("Shake")]
	public float shakePosAmount = 0.004f;
	public float shakeRotAmount = 0.08f;
	public float shakeSpeed = 12f;

	[Header("Mode")]
	public bool passengerMode = true;

	[Header("References")]
	public Transform worldRoot;
	public Camera playerCamera;

	[HideInInspector] public bool isSwaySuspended = false;
	[HideInInspector] public bool isShakeSuspended = false;
	[HideInInspector] public Vector3 seatedLocalPosition;

	private Vector3 _swayOrigin;

	[Header("Sway Fade")]
	public float swayFadeInDuration = 1.5f;

	private float _swayFadeProgress = 1f;  // 1 = full sway, 0 = no sway
	private bool _isFadingInSway = false;

	public void FadeInSway()
	{
		_swayFadeProgress = 0f;
		_isFadingInSway = true;
		isSwaySuspended = false;
		isShakeSuspended = false;
	}

	void Start()
	{
		if (playerCamera == null)
			playerCamera = Camera.main;
		_swayOrigin = playerCamera.transform.localPosition;
	}

	void Update()
	{
		// World always scrolls
		worldRoot.position -= new Vector3(0, 0, trainSpeed * Time.deltaTime);

		// Handle sway fade in progress
		if (_isFadingInSway)
		{
			_swayFadeProgress += Time.deltaTime / swayFadeInDuration;
			_swayFadeProgress = Mathf.Clamp01(_swayFadeProgress);

			if (_swayFadeProgress >= 1f)
				_isFadingInSway = false;
		}

		// Calculate shake offset
		Vector3 shakeOffset = Vector3.zero;
		Quaternion shakeRot = Quaternion.identity;

		if (!isShakeSuspended)
		{
			float shakeX = (Mathf.PerlinNoise(Time.time * shakeSpeed, 0f)
						   - 0.5f) * shakePosAmount * _swayFadeProgress;
			float shakeY = (Mathf.PerlinNoise(0f, Time.time * shakeSpeed)
						   - 0.5f) * shakePosAmount * _swayFadeProgress;
			float rotShakeX = (Mathf.PerlinNoise(Time.time * 4f, 5f)
							  - 0.5f) * shakeRotAmount * _swayFadeProgress;
			float rotShakeZ = (Mathf.PerlinNoise(Time.time * 4f, 10f)
							  - 0.5f) * shakeRotAmount * _swayFadeProgress;

			shakeOffset = new Vector3(shakeX, shakeY, 0);
			shakeRot = Quaternion.Euler(rotShakeX, 0, rotShakeZ);
		}

		if (isSwaySuspended)
		{
			// Seated or transitioning — shake around anchor
			playerCamera.transform.localPosition =
				seatedLocalPosition + shakeOffset;
		}
		else if (passengerMode)
		{
			// Standing passenger mode — sway + shake with fade
			float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmount
						  * _swayFadeProgress;
			float swayY = Mathf.Sin(Time.time * swaySpeed * 0.6f)
						  * swayAmount * 0.4f * _swayFadeProgress;
			float gazeY = Mathf.Sin(Time.time * 0.12f) * 3f
						  * _swayFadeProgress;
			float gazeX = Mathf.Sin(Time.time * 0.07f) * 1.5f
						  * _swayFadeProgress;

			playerCamera.transform.localPosition = _swayOrigin +
				new Vector3(swayX, swayY, 0) + shakeOffset;
			playerCamera.transform.localRotation =
				Quaternion.Euler(gazeX, 90f + gazeY, 0) * shakeRot;
		}
	}
	public void UpdateSwayOrigin(Vector3 newOrigin)
	{
		_swayOrigin = newOrigin;
	}
	public void ToggleMode()
	{
		passengerMode = !passengerMode;
	}
}