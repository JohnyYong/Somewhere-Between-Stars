using UnityEngine;

public class SeatInteraction : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;
    public Transform sofaSeatPosition;
    public Transform journalViewPosition;
    public Transform camOrigin;  // drag CamOrigin here
    public Transform companionViewPosition;

    public InteractionHighlight highlight;
    public TrainController trainController;
    public JournalManager journalManager;
    public SlimeReply slimeReply;

    [Header("Settings")]
    public float transitionSpeed = 2.0f;
    public KeyCode exitKey = KeyCode.Escape;
    public GameObject ChatSystem;
    // State
    private enum PlayerState { Standing, Seated, ViewingJournal, TalkingToCompanion }
    private PlayerState _currentState = PlayerState.Standing;

    private PlayerState _stateBeforeOverlay = PlayerState.Standing;

    // Stored positions
    private Vector3 _standingLocalPos;
    private Quaternion _standingLocalRot;
    private Vector3 _seatedLocalPos;
    private Quaternion _seatedLocalRot;

    // Transition
    private bool _isTransitioning = false;
    private float _transitionProgress = 0f;
    private Vector3 _transitionStartPos;
    private Quaternion _transitionStartRot;
    private Vector3 _transitionTargetPos;
    private Quaternion _transitionTargetRot;
    private PlayerState _transitionDestination;

    [SerializeField] private GameObject playerChatBox;

    void Start()
    {
        // Use CamOrigin as the standing reference instead of initial position
        _standingLocalPos = playerCamera.parent
            .InverseTransformPoint(camOrigin.position);
        _standingLocalRot = camOrigin.rotation;
    }

    void Update()
    {
        HandleInput();
        HandleTransition();
    }

    void HandleInput()
    {
        if (_isTransitioning) return;

        // Escape logic — always goes up one level
        if (Input.GetKeyDown(exitKey))
        {
            ChatSystem.SetActive(false);
            playerChatBox.SetActive(false);
            if (_currentState == PlayerState.ViewingJournal ||
                _currentState == PlayerState.TalkingToCompanion)
            {
                // Return to whatever state we were in before entering this overlay
                BeginTransition(_stateBeforeOverlay);
                return;
            }

            if (_currentState == PlayerState.Seated)
            {
                BeginTransition(PlayerState.Standing);
                return;
            }


        }

        // Click interactions
        if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }


            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 60f))
            {
                if (hit.collider.CompareTag("RightSofaChair") &&
                    _currentState == PlayerState.Standing)
                {
                    BeginTransition(PlayerState.Seated);
                }
                else if (hit.collider.CompareTag("Diary") &&
                         _currentState != PlayerState.ViewingJournal)
                {
                    EnterOverlay(PlayerState.ViewingJournal);
                }
                else if (hit.collider.CompareTag("Companion") &&
                         _currentState != PlayerState.TalkingToCompanion)
                {
                    EnterOverlay(PlayerState.TalkingToCompanion);
                    ChatSystem.SetActive(true);
                    playerChatBox.SetActive(true);
                }
            }
        }
    }

    // Shared entry point for both overlay states (Journal, Companion).
    // Remembers whether we were Standing or Seated so Escape can return correctly.
    void EnterOverlay(PlayerState overlayState)
    {
        if (_currentState == PlayerState.Standing || _currentState == PlayerState.Seated)
        {
            _stateBeforeOverlay = _currentState;

            if (_currentState == PlayerState.Seated)
            {
                _seatedLocalPos = playerCamera.localPosition;
                _seatedLocalRot = playerCamera.localRotation;
            }
        }
        // If we're already in the other overlay (e.g. Journal -> Companion directly),
        // _stateBeforeOverlay keeps whatever was recorded on the way into that overlay.

        BeginTransition(overlayState);
    }

    void BeginTransition(PlayerState destination)
    {
        _transitionDestination = destination;
        _transitionProgress = 0f;
        _transitionStartPos = playerCamera.localPosition;
        _transitionStartRot = playerCamera.localRotation;

        switch (destination)
        {
            case PlayerState.Standing:
                _transitionTargetPos = playerCamera.parent
                    .InverseTransformPoint(camOrigin.position);
                _transitionTargetRot = camOrigin.rotation;
                break;

            case PlayerState.Seated:
                _transitionTargetPos = playerCamera.parent
                    .InverseTransformPoint(sofaSeatPosition.position);
                _transitionTargetRot = sofaSeatPosition.rotation;
                break;

            case PlayerState.ViewingJournal:
                _transitionTargetPos = playerCamera.parent
                    .InverseTransformPoint(journalViewPosition.position);
                _transitionTargetRot = journalViewPosition.rotation;
                break;

            case PlayerState.TalkingToCompanion:
                _transitionTargetPos = playerCamera.parent
                    .InverseTransformPoint(companionViewPosition.position);
                _transitionTargetRot = companionViewPosition.rotation;
                break;
        }

        _isTransitioning = true;

        if (trainController != null)
        {
            trainController.isSwaySuspended = true;
            trainController.isShakeSuspended = true;
        }

        if (_currentState == PlayerState.ViewingJournal &&
            destination != PlayerState.ViewingJournal)
        {
            if (journalManager != null)
                journalManager.CloseJournal();
        }

        // Leaving the companion — end the conversation cleanly
        if (_currentState == PlayerState.TalkingToCompanion &&
            destination != PlayerState.TalkingToCompanion)
        {
            if (slimeReply != null)
                slimeReply.EndConversation();
        }
    }
    void HandleTransition()
    {
        if (!_isTransitioning) return;

        _transitionProgress += Time.deltaTime * transitionSpeed;
        _transitionProgress = Mathf.Clamp01(_transitionProgress);

        float t = Mathf.SmoothStep(0f, 1f, _transitionProgress);

        Vector3 newPos = Vector3.Lerp(
            _transitionStartPos, _transitionTargetPos, t);
        Quaternion newRot = Quaternion.Lerp(
            _transitionStartRot, _transitionTargetRot, t);

        playerCamera.localPosition = newPos;
        playerCamera.localRotation = newRot;

        // Keep shake anchor updated
        if (trainController != null)
            trainController.seatedLocalPosition = newPos;

        if (_transitionProgress >= 1f)
        {
            playerCamera.localPosition = _transitionTargetPos;
            playerCamera.localRotation = _transitionTargetRot;
            _isTransitioning = false;
            _currentState = _transitionDestination;

            if (_currentState == PlayerState.Standing && trainController != null)
            {
                trainController.UpdateSwayOrigin(_standingLocalPos);
                ReEnableSway();  // no more Invoke — called immediately
            }
            else
            {
                // Re-enable shake for seated/journal/companion states
                // sway stays suspended
                if (trainController != null)
                    trainController.isShakeSuspended = false;
            }

            if (_currentState == PlayerState.ViewingJournal)
            {
                Debug.Log("Arrived at journal — journalManager: " +
                          (journalManager != null ? "assigned" : "NULL"));
                if (journalManager != null)
                    journalManager.OpenJournal();
            }

            if (_currentState == PlayerState.TalkingToCompanion)
            {
                if (slimeReply != null)
                    slimeReply.BeginConversation();
            }
        }
    }

    void ReEnableSway()
    {
        if (trainController != null)
        {
            trainController.UpdateSwayOrigin(_standingLocalPos);
            trainController.FadeInSway();
        }
    }
}