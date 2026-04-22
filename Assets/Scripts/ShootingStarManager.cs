using System.Collections;
using UnityEngine;

public class ShootingStarManager : MonoBehaviour
{
	[Header("Timing")]
	public float minInterval = 10f;
	public float maxInterval = 20f;

	[Header("Spawn Area")]
	public float spawnRangeX = 800f;
	public float spawnRangeY = 400f;
	public float spawnDistance = 500f;   // distance from camera

	[Header("Direction")]
	public float minAngle = -60f;        // degrees from horizontal
	public float maxAngle = -20f;
	public float speedMin = 600f;
	public float speedMax = 1200f;

	[Header("Rare Double Star")]
	[Range(0f, 1f)]
	public float doubleStarChance = 0.15f;
	public float doubleStarOffset = 30f;

	[Header("References")]
	public ParticleSystem shootingStarParticle;
	public Camera playerCamera;

	void Start()
	{
		if (playerCamera == null)
			playerCamera = Camera.main;

		StartCoroutine(ShootingStarRoutine());
	}
	void Update()
	{
		// Press T to trigger shooting star manually (test only)
		if (Input.GetKeyDown(KeyCode.T))
			SpawnShootingStar(Vector3.zero);

		// Press Y to trigger double star
		if (Input.GetKeyDown(KeyCode.Y))
		{
			SpawnShootingStar(Vector3.zero);
			StartCoroutine(SpawnDelayed());
		}
	}

	IEnumerator SpawnDelayed()
	{
		yield return new WaitForSeconds(0.2f);
		SpawnShootingStar(new Vector3(
			Random.Range(-doubleStarOffset, doubleStarOffset),
			Random.Range(-doubleStarOffset, doubleStarOffset),
			0));
	}
	IEnumerator ShootingStarRoutine()
	{
		// Initial delay so game isn't immediately interrupted
		yield return new WaitForSeconds(Random.Range(10f, 30f));

		while (true)
		{
			// Wait random interval
			float interval = Random.Range(minInterval, maxInterval);
			yield return new WaitForSeconds(interval);

			// Spawn shooting star
			SpawnShootingStar(Vector3.zero);

			// Rare double star
			if (Random.value < doubleStarChance)
			{
				yield return new WaitForSeconds(
					Random.Range(0.1f, 0.4f));
				SpawnShootingStar(new Vector3(
					Random.Range(-doubleStarOffset, doubleStarOffset),
					Random.Range(-doubleStarOffset, doubleStarOffset),
					0));
			}
		}
	}

	void SpawnShootingStar(Vector3 extraOffset)
	{
		// Random spawn position in front of camera
		Vector3 camPos = playerCamera.transform.position;
		Vector3 camForward = playerCamera.transform.forward;

		// Spawn in the sky area visible through window
		float randX = Random.Range(-spawnRangeX, spawnRangeX);
		float randY = Random.Range(0f, spawnRangeY); // upper half of view
		float randZ = spawnDistance;

		Vector3 spawnPos = camPos +
						  camForward * randZ +
						  playerCamera.transform.right * randX +
						  playerCamera.transform.up * randY +
						  extraOffset;

		// Random angle for direction
		float angle = Random.Range(minAngle, maxAngle);
		float speed = Random.Range(speedMin, speedMax);

		// Convert angle to direction vector
		float rad = angle * Mathf.Deg2Rad;
		Vector3 direction = new Vector3(
			Mathf.Cos(rad),
			Mathf.Sin(rad),
			0f
		) * speed;

		// Randomise if going left or right
		if (Random.value > 0.5f)
			direction.x = -direction.x;

		// Apply velocity to particle system
		var velocityModule = shootingStarParticle.velocityOverLifetime;
		velocityModule.x = direction.x;
		velocityModule.y = direction.y;

		// Move particle system to spawn position and play
		shootingStarParticle.transform.position = spawnPos;
		shootingStarParticle.Play();
	}
}