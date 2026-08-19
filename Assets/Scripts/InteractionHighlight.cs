using System.Collections.Generic;
using UnityEngine;

public class InteractionHighlight : MonoBehaviour
{
	[Header("Settings")]
	public float detectionDistance = 5f;
	public float fadeSpeed = 4f;
	public float glowIntensity = 1.5f;
	public float lightRange = 1.5f;
	public Color lightColor = Color.white;


	[Header("Tags")]
	public string[] interactableTags = { "Radio", "Diary", "RightSofaChair", "Companion" };

	private Camera _playerCamera;
	private GameObject _currentTarget;
	private GameObject _previousTarget;
	private float _currentGlow = 0f;
	private Color prevColor = Color.white;
	private Light _highlightLight;

	void Start()
	{
		_playerCamera = Camera.main;

		// Create highlight point light
		var lightGO = new GameObject("HighlightLight");
		lightGO.transform.parent = transform;

		_highlightLight = lightGO.AddComponent<Light>();
		_highlightLight.type = LightType.Point;
		_highlightLight.color = lightColor;
		prevColor = _highlightLight.color;
		_highlightLight.intensity = 50f;
		_highlightLight.range = lightRange;
		_highlightLight.shadows = LightShadows.None;

		// Interior rendering layer only
		_highlightLight.cullingMask = ~0;
	}

	void Update()
	{
		if (_playerCamera == null)
			_playerCamera = Camera.main;

		if (_playerCamera == null) return;

		DetectTarget();
		UpdateGlow();
	}

	void DetectTarget()
	{
        if (UnityEngine.EventSystems.EventSystem.current != null &&
		UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		Ray ray = _playerCamera.ScreenPointToRay(Input.mousePosition);

		_previousTarget = _currentTarget;
		_currentTarget = null;

		if (Physics.Raycast(ray, out RaycastHit hit, detectionDistance))
		{
			foreach (var tag in interactableTags)
			{
				// Check hit object and its parent
				if (hit.collider.CompareTag(tag) ||
					(hit.collider.transform.parent != null &&
					 hit.collider.transform.parent.CompareTag(tag)))
				{
					_currentTarget = hit.collider.gameObject;
					break;
				}
			}
		}

		// Target changed — snap glow to 0
		if (_previousTarget != _currentTarget)
			_currentGlow = 0f;
	}

	void UpdateGlow()
	{
		float targetIntensity = _currentTarget != null ? glowIntensity : 0f;

		_currentGlow = Mathf.Lerp(
			_currentGlow, targetIntensity,
			Time.deltaTime * fadeSpeed);

		_highlightLight.intensity = _currentGlow;

		// Move light to current target
		if (_currentTarget != null)
		{
			_highlightLight.transform.position =
				_currentTarget.transform.position + Vector3.up * 0.2f;
		}

		_highlightLight.gameObject.SetActive(_currentGlow > 0.001f);

		if (prevColor != lightColor)
		{
            _highlightLight.color = lightColor;
			prevColor = lightColor;
        }

    }
}