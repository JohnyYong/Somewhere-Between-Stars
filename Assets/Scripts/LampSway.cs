using UnityEngine;

public class LampSway : MonoBehaviour
{
	[Header("Sway Settings")]
	public float swayAngle = 8f;        // max degrees of swing
	public float swaySpeed = 0.6f;      // how fast it swings
	public float swayRandomness = 0.2f; // adds slight irregularity

	private float _timeOffset;
	private Quaternion _startRotation;

	void Start()
	{
		_startRotation = transform.localRotation;
		// Random offset so multiple lamps don't sync perfectly
		_timeOffset = Random.Range(0f, 100f);
	}

	void Update()
	{
		float sway = Mathf.Sin((Time.time + _timeOffset) * swaySpeed) * swayAngle;
		transform.localRotation = _startRotation *
								  Quaternion.Euler(0, 0, sway);
	}
}