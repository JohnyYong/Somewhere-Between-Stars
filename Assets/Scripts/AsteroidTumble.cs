using UnityEngine;

public class AsteroidTumble : MonoBehaviour
{
	private Vector3 _tumbleAxis;
	private float _tumbleSpeed;

	void Start()
	{
		// Random tumble axis and speed for each asteroid
		_tumbleAxis = Random.onUnitSphere;
		_tumbleSpeed = Random.Range(5f, 25f);
	}

	void Update()
	{
		transform.Rotate(_tumbleAxis, _tumbleSpeed * Time.deltaTime);
	}
}