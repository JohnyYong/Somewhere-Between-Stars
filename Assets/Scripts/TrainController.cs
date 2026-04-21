using UnityEngine;

public class TrainController : MonoBehaviour
{
	[Header("Movement")]
	public float trainSpeed = 40f;
	public float swayAmount = 0.04f;
	public float swaySpeed = 0.8f;

	[Header("Mode")]
	public bool passengerMode = true;

	[Header("References")]
	public Transform worldRoot;
	public Camera playerCamera;

	private Vector3 _swayOrigin;

	void Start()
	{
		if (playerCamera == null)
			playerCamera = Camera.main;

		_swayOrigin = playerCamera.transform.localPosition;
	}

	void Update()
	{
		// World always scrolls — regardless of mode
		worldRoot.position -= new Vector3(0, 0, trainSpeed * Time.deltaTime);

		if (passengerMode)
		{
			// Gentle automatic sway — like sitting on a real train
			float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
			float swayY = Mathf.Sin(Time.time * swaySpeed * 0.6f) * swayAmount * 0.4f;
			playerCamera.transform.localPosition = _swayOrigin + new Vector3(swayX, swayY, 0);

			// Very subtle automatic gaze drift — eyes naturally wander
			float gazeY = Mathf.Sin(Time.time * 0.12f) * 3f;   // slow left/right
			float gazeX = Mathf.Sin(Time.time * 0.07f) * 1.5f; // very slow up/down
			playerCamera.transform.localRotation = Quaternion.Euler(gazeX, 90f + gazeY, 0);
		}
	}

	// Call this from a UI button or keypress later if you want to toggle
	public void ToggleMode()
	{
		passengerMode = !passengerMode;
	}
}