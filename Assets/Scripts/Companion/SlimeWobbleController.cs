using UnityEngine;

// Attach this to the same GameObject as the MeshRenderer using the
// Custom/SlimeSphere shader. It drives the shader's _WobbleAmount property
// up and down over time using Perlin noise, so the sphere drifts between
// "smooth" and "wobbly" on its own instead of needing hand-keyframed values.
//
// Uses a MaterialPropertyBlock so it won't create a material instance
// or break GPU instancing/batching.
[RequireComponent(typeof(Renderer))]
public class SlimeWobbleController : MonoBehaviour
{
    [Header("Wobble Range")]
    [Tooltip("Minimum wobble amount (near-smooth state).")]
    [Range(0f, 1f)] public float minWobble = 0.05f;

    [Tooltip("Maximum wobble amount (fully jiggly state).")]
    [Range(0f, 1f)] public float maxWobble = 0.9f;

    [Header("Drift Timing")]
    [Tooltip("How quickly the target wobble level drifts between calm and jiggly. Lower = slower, more gradual mood swings.")]
    public float driftSpeed = 0.15f;

    [Tooltip("How quickly the current wobble value chases the drifting target. Higher = snappier transitions.")]
    public float followSpeed = 1.5f;

    [Header("Optional: Poke to Jiggle")]
    [Tooltip("If true, calling Poke() will temporarily spike the wobble amount (e.g. on collision or click).")]
    public bool enablePoke = true;
    public float pokeStrength = 1f;
    public float pokeDecay = 2f;

    static readonly int WobbleAmountId = Shader.PropertyToID("_WobbleAmount");

    Renderer _renderer;
    MaterialPropertyBlock _propBlock;
    float _noiseOffset;
    float _currentWobble;
    float _pokeValue;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        _noiseOffset = Random.Range(0f, 1000f); // so multiple slimes don't sync up
        _currentWobble = minWobble;
    }

    void Update()
    {
        // Perlin noise (0..1) sampled over time gives a smooth, non-repeating
        // drift between calm and jiggly states.
        float n = Mathf.PerlinNoise(_noiseOffset, Time.time * driftSpeed);
        float targetWobble = Mathf.Lerp(minWobble, maxWobble, n);

        _currentWobble = Mathf.Lerp(_currentWobble, targetWobble, Time.deltaTime * followSpeed);

        if (enablePoke && _pokeValue > 0f)
        {
            _pokeValue = Mathf.MoveTowards(_pokeValue, 0f, pokeDecay * Time.deltaTime);
        }

        float finalWobble = Mathf.Clamp01(_currentWobble + _pokeValue);

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetFloat(WobbleAmountId, finalWobble);
        _renderer.SetPropertyBlock(_propBlock);
    }

    /// <summary>
    /// Call this (e.g. from an OnCollisionEnter or OnMouseDown) to make the
    /// slime spike into a jiggly state momentarily, then settle back down.
    /// </summary>
    public void Poke()
    {
        if (!enablePoke) return;
        _pokeValue = pokeStrength;
    }
}
