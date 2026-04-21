using System.Collections.Generic;
using UnityEngine;

public class PlanetSpawner : MonoBehaviour
{
	[Header("References")]
	public GameObject planetPrefab;
	public Transform worldRoot;
	public Transform trainTransform;

	[Header("Spawn Settings")]
	public float spawnAheadDistance = 8000f;
	public float despawnBehindDistance = 2000f;
	public float minSpacing = 1500f;
	public float maxSpacing = 4000f;

	[Header("Planet Size")]
	public float minRadius = 300f;
	public float maxRadius = 1200f;

	[Header("Planet Position")]
	public float minLateralOffset = 400f;
	public float maxLateralOffset = 2000f;
	public float minVerticalOffset = -300f;
	public float maxVerticalOffset = 500f;

	private List<GameObject> _activePlanets = new();
	private float _nextSpawnZ;

	void Start()
	{
		// Start spawning ahead of the train
		_nextSpawnZ = trainTransform.position.z + 2000f;

		// Spawn initial batch so world isn't empty on start
		while (_nextSpawnZ < trainTransform.position.z + spawnAheadDistance)
		{
			SpawnPlanet(_nextSpawnZ);
			_nextSpawnZ += Random.Range(minSpacing, maxSpacing);
		}
	}

	void Update()
	{
		float trainZ = -worldRoot.position.z;

		// Keep spawning ahead
		while (_nextSpawnZ < trainZ + spawnAheadDistance)
		{
			SpawnPlanet(_nextSpawnZ);
			_nextSpawnZ += Random.Range(minSpacing, maxSpacing);
		}

		// Despawn planets that are far behind
		for (int i = _activePlanets.Count - 1; i >= 0; i--)
		{
			if (_activePlanets[i] == null)
			{
				_activePlanets.RemoveAt(i);
				continue;
			}

			// Use local position relative to WorldRoot instead
			float planetWorldZ = worldRoot.position.z + _activePlanets[i].transform.localPosition.z;

			if (planetWorldZ < -despawnBehindDistance)
			{
				Destroy(_activePlanets[i]);
				_activePlanets.RemoveAt(i);
			}
		}
	}

	void SpawnPlanet(float atZ)
	{
		// Random side — left or right of train
		bool leftSide = Random.value > 0.5f;
		float lateralX = Random.Range(minLateralOffset, maxLateralOffset);
		if (leftSide) lateralX = -lateralX;

		float verticalY = Random.Range(minVerticalOffset, maxVerticalOffset);

		Vector3 spawnPos = new Vector3(lateralX, verticalY, atZ);

		// Spawn under WorldRoot so it moves with the world
		GameObject planet = Instantiate(planetPrefab, spawnPos,
							Quaternion.identity, worldRoot);

		// Random size
		float radius = Random.Range(minRadius, maxRadius);
		planet.transform.localScale = Vector3.one * radius;

		// Random rotation so planets aren't all oriented the same
		planet.transform.rotation = Random.rotation;

		// Trigger color randomization
		var randomizer = planet.GetComponent<PlanetColorRandomizer>();
		if (randomizer != null)
			randomizer.Randomize();

		_activePlanets.Add(planet);
	}
}