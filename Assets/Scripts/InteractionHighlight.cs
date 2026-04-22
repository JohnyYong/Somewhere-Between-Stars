using System.Collections.Generic;
using UnityEngine;

public class InteractionHighlight : MonoBehaviour
{
	[Header("Settings")]
	public float detectionDistance = 5f;
	public float fadeSpeed = 4f;
	public Color glowColor = new Color(1f, 1f, 1f, 1f);
	[Range(0f, 1f)]
	public float glowIntensity = 0.15f;  // keep this low!

	[Header("Tags")]
	public string[] interactableTags = { "Radio", "Window", "RightSofaChair" };

	private Camera _playerCamera;
	private GameObject _currentTarget;
	private GameObject _previousTarget;
	private float _currentGlow = 0f;

	// Store original emission colors to restore later
	private Dictionary<Material, Color> _originalEmissions = new();

	void Start() => _playerCamera = Camera.main;

	void Update()
	{
		DetectTarget();
		UpdateGlow();
	}

	void DetectTarget()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		Ray ray = _playerCamera.ScreenPointToRay(Input.mousePosition);

		_previousTarget = _currentTarget;
		_currentTarget = null;

		if (Physics.Raycast(ray, out RaycastHit hit, detectionDistance))
		{
			foreach (var tag in interactableTags)
			{
				if (hit.collider.CompareTag(tag))
				{
					_currentTarget = hit.collider.gameObject;
					break;
				}
			}
		}

		// Target changed — fade out old
		if (_previousTarget != _currentTarget && _previousTarget != null)
		{
			_currentGlow = 0f;
			SetGlow(_previousTarget, 0f);
		}
	}

	void UpdateGlow()
	{
		if (_currentTarget != null)
		{
			_currentGlow = Mathf.Lerp(_currentGlow, glowIntensity,
									   Time.deltaTime * fadeSpeed);
			SetGlow(_currentTarget, _currentGlow);
		}
		else
		{
			_currentGlow = Mathf.Lerp(_currentGlow, 0f,
									   Time.deltaTime * fadeSpeed);
		}
	}

	void SetGlow(GameObject target, float intensity)
	{
		var renderers = target.GetComponentsInChildren<Renderer>();

		foreach (var r in renderers)
		{
			foreach (var mat in r.materials)
			{
				if (intensity > 0.001f)
				{
					mat.EnableKeyword("_EMISSION");
					mat.SetColor("_EmissionColor", glowColor * intensity);
				}
				else
				{
					mat.DisableKeyword("_EMISSION");
					mat.SetColor("_EmissionColor", Color.black);
				}
			}
		}
	}
}