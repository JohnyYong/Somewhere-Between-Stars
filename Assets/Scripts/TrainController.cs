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

		// Calculate shake offset
		Vector3 shakeOffset = Vector3.zero;
		Quaternion shakeRot = Quaternion.identity;

		if (!isShakeSuspended)
		{
			float shakeX = (Mathf.PerlinNoise(Time.time * shakeSpeed, 0f)
						   - 0.5f) * shakePosAmount;
			float shakeY = (Mathf.PerlinNoise(0f, Time.time * shakeSpeed)
						   - 0.5f) * shakePosAmount;
			float rotShakeX = (Mathf.PerlinNoise(Time.time * 4f, 5f)
							  - 0.5f) * shakeRotAmount;
			float rotShakeZ = (Mathf.PerlinNoise(Time.time * 4f, 10f)
							  - 0.5f) * shakeRotAmount;

			shakeOffset = new Vector3(shakeX, shakeY, 0);
			shakeRot = Quaternion.Euler(rotShakeX, 0, rotShakeZ);
		}

		if (isSwaySuspended)
		{
			// Seated — shake around the stored seat position
			playerCamera.transform.localPosition =
				seatedLocalPosition + shakeOffset;
		}
		else if (passengerMode)
		{
			// Standing passenger mode — sway + shake
			float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
			float swayY = Mathf.Sin(Time.time * swaySpeed * 0.6f)
						  * swayAmount * 0.4f;
			float gazeY = Mathf.Sin(Time.time * 0.12f) * 3f;
			float gazeX = Mathf.Sin(Time.time * 0.07f) * 1.5f;

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