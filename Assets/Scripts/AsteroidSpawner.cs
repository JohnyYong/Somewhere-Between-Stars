using System.Collections.Generic;
using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
	[Header("References")]
	public GameObject[] asteroidPrefabs;    // drag all 9 variants here
	public Transform worldRoot;
	public Transform trainTransform;

	[Header("Spawn Settings")]
	public float spawnAheadDistance = 6000f;
	public float despawnBehindDistance = 1500f;
	public float minSpacing = 80f;
	public float maxSpacing = 400f;

	[Header("Asteroid Size")]
	public float minScale = 5f;
	public float maxScale = 80f;

	[Header("Position")]
	public float minLateralOffset = 100f;
	public float maxLateralOffset = 1200f;
	public float minVerticalOffset = -200f;
	public float maxVerticalOffset = 300f;

	[Header("Clustering")]
	[Range(0f, 1f)]
	public float clusterChance = 0.3f;      // chance to spawn a cluster
	public int minClusterSize = 3;
	public int maxClusterSize = 8;
	public float clusterRadius = 150f;

	private List<GameObject> _active = new();
	private float _nextSpawnZ;

	void Start()
	{
		_nextSpawnZ = 300f;

		while (_nextSpawnZ < spawnAheadDistance)
		{
			if (Random.value < clusterChance)
				SpawnCluster(_nextSpawnZ);
			else
				SpawnSingle(_nextSpawnZ);

			_nextSpawnZ += Random.Range(minSpacing, maxSpacing);
		}
	}

	void Update()
	{
		float trainZ = -worldRoot.position.z;

		while (_nextSpawnZ < trainZ + spawnAheadDistance)
		{
			if (Random.value < clusterChance)
				SpawnCluster(_nextSpawnZ);
			else
				SpawnSingle(_nextSpawnZ);

			_nextSpawnZ += Random.Range(minSpacing, maxSpacing);
		}

		// Despawn
		for (int i = _active.Count - 1; i >= 0; i--)
		{
			if (_active[i] == null)
			{
				_active.RemoveAt(i);
				continue;
			}

			float planetWorldZ = worldRoot.position.z +
								 _active[i].transform.localPosition.z;

			if (planetWorldZ < -despawnBehindDistance)
			{
				Destroy(_active[i]);
				_active.RemoveAt(i);
			}
		}
	}

	void SpawnSingle(float atZ)
	{
		bool left = Random.value > 0.5f;
		float x = Random.Range(minLateralOffset, maxLateralOffset) * (left ? -1 : 1);
		float y = Random.Range(minVerticalOffset, maxVerticalOffset);

		PlaceAsteroid(new Vector3(x, y, atZ));
	}

	void SpawnCluster(float atZ)
	{
		// Pick a cluster center
		bool left = Random.value > 0.5f;
		float cx = Random.Range(minLateralOffset, maxLateralOffset) * (left ? -1 : 1);
		float cy = Random.Range(minVerticalOffset, maxVerticalOffset);
		Vector3 center = new Vector3(cx, cy, atZ);

		int count = Random.Range(minClusterSize, maxClusterSize);
		for (int i = 0; i < count; i++)
		{
			// Scatter asteroids around the cluster center
			Vector3 offset = Random.insideUnitSphere * clusterRadius;
			PlaceAsteroid(center + offset);
		}
	}

	void PlaceAsteroid(Vector3 pos)
	{
		var prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
		var go = Instantiate(prefab, pos, Random.rotation, worldRoot);

		float scale = Random.Range(minScale, maxScale);
		go.transform.localScale = Vector3.one * scale;

		_active.Add(go);
	}
}