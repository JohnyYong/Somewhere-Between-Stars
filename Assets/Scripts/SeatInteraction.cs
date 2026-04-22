using UnityEngine;

public class SeatInteraction : MonoBehaviour
{
	[Header("References")]
	public Transform playerCamera;
	public Transform sofaSeatPosition;
	public InteractionHighlight highlight;
	public TrainController trainController;

	[Header("Settings")]
	public float transitionSpeed = 2.0f;
	public KeyCode exitKey = KeyCode.Escape;

	private Vector3 _originalLocalPosition;
	private Quaternion _originalLocalRotation;
	private bool _isSeated = false;
	private bool _isTransitioning = false;
	private Vector3 _transitionStartPos;
	private Quaternion _transitionStartRot;
	private float _transitionProgress = 0f;

	void Start()
	{
		// Store original LOCAL position — not world position
		_originalLocalPosition = playerCamera.localPosition;
		_originalLocalRotation = playerCamera.localRotation;
	}

	void Update()
	{
		HandleInput();
		HandleTransition();
	}

	void HandleInput()
	{
		if (Input.GetMouseButtonDown(0) && !_isSeated && !_isTransitioning)
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			if (Physics.Raycast(ray, out RaycastHit hit, 60f))
			{
				if (hit.collider.CompareTag("RightSofaChair"))
					SitDown();
			}
		}

		if (Input.GetKeyDown(exitKey) && _isSeated && !_isTransitioning)
			StandUp();
	}

	void SitDown()
	{
		_isSeated = true;
		_isTransitioning = true;
		_transitionProgress = 0f;
		_transitionStartPos = playerCamera.localPosition;
		_transitionStartRot = playerCamera.localRotation;

		// Set seated position BEFORE suspending sway
		// so TrainController has the right anchor immediately
		if (trainController != null)
		{
			trainController.seatedLocalPosition = playerCamera.localPosition;
			trainController.isSwaySuspended = true;
		}
	}

	void StandUp()
	{
		_isSeated = false;
		_isTransitioning = true;
		_transitionProgress = 0f;
		_transitionStartPos = playerCamera.localPosition;
		_transitionStartRot = playerCamera.localRotation;
		// Keep sway suspended during transition
		// it gets re-enabled when transition completes
	}

	void HandleTransition()
	{
		if (!_isTransitioning) return;

		_transitionProgress += Time.deltaTime * transitionSpeed;
		_transitionProgress = Mathf.Clamp01(_transitionProgress);

		float t = Mathf.SmoothStep(0f, 1f, _transitionProgress);

		Vector3 targetLocalPos = _isSeated ?
			playerCamera.parent.InverseTransformPoint(sofaSeatPosition.position)
			: _originalLocalPosition;

		Quaternion targetLocalRot = _isSeated ?
			sofaSeatPosition.rotation : _originalLocalRotation;

		Vector3 newPos = Vector3.Lerp(_transitionStartPos, targetLocalPos, t);
		Quaternion newRot = Quaternion.Lerp(_transitionStartRot, targetLocalRot, t);

		playerCamera.localPosition = newPos;
		playerCamera.localRotation = newRot;

		// Keep shake anchor updated during both sit and stand transitions
		if (trainController != null)
			trainController.seatedLocalPosition = newPos;

		if (_transitionProgress >= 1f)
		{
			playerCamera.localPosition = targetLocalPos;
			playerCamera.localRotation = targetLocalRot;
			_isTransitioning = false;

			if (!_isSeated && trainController != null)
			{
				// Update sway origin to current position before re-enabling
				trainController.UpdateSwayOrigin(targetLocalPos);
				trainController.isSwaySuspended = false;
			}
		}
	}
}