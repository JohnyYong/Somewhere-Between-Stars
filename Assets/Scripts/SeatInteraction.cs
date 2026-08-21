using System.Collections;
using UnityEngine;

public class SeatInteraction : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;
    public Transform sofaSeatPosition;
    public Transform journalViewPosition;
    public Transform camOrigin;  // drag CamOrigin here
    public Transform companionViewPosition;
    public Transform luggageSettingViewPosition;

    public InteractionHighlight highlight;
    public TrainController trainController;
    public JournalManager journalManager;
    public SlimeReply slimeReply;

    [Header("Settings")]
    public float transitionSpeed = 2.0f;
    public KeyCode exitKey = KeyCode.Escape;
    public GameObject ChatSystem;
    public GameObject settingsPanel; //shown when viewing the luggage settings

    [Header("Settings Panel Bounce")]
    public float settingsOpenDuration = 0.35f;
    public float settingsCloseDuration = 0.2f;

    // State
    private enum PlayerState { Standing, Seated, ViewingJournal, TalkingToCompanion, ViewingSettings }
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

    // --- Settings panel bounce state ---
    private RectTransform _settingsPanelRect;
    private Coroutine _settingsPanelRoutine;

    void Start()
    {
        // Use CamOrigin as the standing reference instead of initial position
        _standingLocalPos = playerCamera.parent
            .InverseTransformPoint(camOrigin.position);
        _standingLocalRot = camOrigin.rotation;

        if (settingsPanel != null)
        {
            _settingsPanelRect = settingsPanel.GetComponent<RectTransform>();
            if (_settingsPanelRect != null)
                _settingsPanelRect.localScale = Vector3.zero;
            settingsPanel.SetActive(false);
        }
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
            CloseSettingsPanel();

            if (_currentState == PlayerState.ViewingJournal ||
                _currentState == PlayerState.TalkingToCompanion ||
                _currentState == PlayerState.ViewingSettings)
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
                else if (hit.collider.CompareTag("Luggage") &&
                         _currentState != PlayerState.ViewingSettings)
                {
                    EnterOverlay(PlayerState.ViewingSettings);
                }
            }
        }
    }

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

            case PlayerState.ViewingSettings:
                _transitionTargetPos = playerCamera.parent
                    .InverseTransformPoint(luggageSettingViewPosition.position);
                _transitionTargetRot = luggageSettingViewPosition.rotation;
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

        if (_currentState == PlayerState.TalkingToCompanion &&
            destination != PlayerState.TalkingToCompanion)
        {
            if (slimeReply != null)
                slimeReply.EndConversation();
        }

        // Leaving settings — bounce the panel closed
        if (_currentState == PlayerState.ViewingSettings &&
            destination != PlayerState.ViewingSettings)
        {
            CloseSettingsPanel();
        }
    }

    void HandleTransition()
    {
        if (!_isTransitioning) return;

        _transitionProgress += Time.deltaTime * transitionSpeed;
        _transitionProgress = Mathf.Clamp01(_transitionProgress);

        float t = Mathf.SmoothStep(0f, 1f, _transitionProgress);

        Vector3 newPos = Vector3.Lerp(_transitionStartPos, _transitionTargetPos, t);
        Quaternion newRot = Quaternion.Lerp(_transitionStartRot, _transitionTargetRot, t);

        playerCamera.localPosition = newPos;
        playerCamera.localRotation = newRot;

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
                ReEnableSway();
            }
            else
            {
                if (trainController != null)
                    trainController.isShakeSuspended = false;
            }

            if (_currentState == PlayerState.ViewingJournal)
            {
                if (journalManager != null)
                    journalManager.OpenJournal();
            }

            if (_currentState == PlayerState.TalkingToCompanion)
            {
                if (slimeReply != null)
                    slimeReply.BeginConversation();
            }

            if (_currentState == PlayerState.ViewingSettings)
            {
                if (settingsPanel != null)
                    OpenSettingsPanel();
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

    // --- Settings panel bounce ---

    void OpenSettingsPanel()
    {
        if (settingsPanel == null || _settingsPanelRect == null) return;
        if (_settingsPanelRoutine != null) StopCoroutine(_settingsPanelRoutine);

        settingsPanel.SetActive(true);
        _settingsPanelRoutine = StartCoroutine(
            ScaleSettingsPanel(Vector3.one, settingsOpenDuration, easeOutBack: true));
    }

    void CloseSettingsPanel(bool instant = false)
    {
        if (settingsPanel == null || _settingsPanelRect == null) return;
        if (_settingsPanelRoutine != null) StopCoroutine(_settingsPanelRoutine);

        if (instant || !settingsPanel.activeSelf)
        {
            _settingsPanelRect.localScale = Vector3.zero;
            settingsPanel.SetActive(false);
            return;
        }

        _settingsPanelRoutine = StartCoroutine(
            ScaleSettingsPanel(Vector3.zero, settingsCloseDuration, easeOutBack: false, deactivateOnFinish: true));
    }

    IEnumerator ScaleSettingsPanel(Vector3 targetScale, float duration, bool easeOutBack, bool deactivateOnFinish = false)
    {
        Vector3 startScale = _settingsPanelRect.localScale;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            float eased = easeOutBack ? EaseOutBack(normalized) : EaseInBack(normalized);
            _settingsPanelRect.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
            yield return null;
        }

        _settingsPanelRect.localScale = targetScale;

        if (deactivateOnFinish)
            settingsPanel.SetActive(false);

        _settingsPanelRoutine = null;
    }

    float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    float EaseInBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }
}