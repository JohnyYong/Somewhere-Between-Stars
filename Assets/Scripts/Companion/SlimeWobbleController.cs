using UnityEngine;


[RequireComponent(typeof(Renderer))]
public class SlimeWobbleController : MonoBehaviour
{
    [Header("Wobble Range")]
    [Range(0f, 1f)] public float minWobble = 0.05f;

    [Range(0f, 1f)] public float maxWobble = 0.9f;

    [Header("Drift Timing")]
    public float driftSpeed = 0.15f;

    public float followSpeed = 1.5f;

    [Header("Optional: Poke to Jiggle")]
    public bool enablePoke = true;
    public float pokeStrength = 1f;
    public float pokeDecay = 2f;

    [Header("Speaking State")]
    [Range(0f, 1f)] public float speakingWobble = 0.6f;
    public float speakingFollowSpeed = 4f;

    [Header("Thinking State")]
    [Range(0f, 1f)] public float thinkingWobble = 0.35f;
    public float thinkingFollowSpeed = 3f;
    public float thinkingSpinSpeed = 320f;
    public float rotationResetSpeed = 4f;

    static readonly int WobbleAmountId = Shader.PropertyToID("_WobbleAmount");

    Renderer _renderer;
    MaterialPropertyBlock _propBlock;
    float _noiseOffset;
    float _currentWobble;
    float _pokeValue;
    bool _isSpeaking;
    bool _isThinking;
    Quaternion _restRotation;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        _noiseOffset = Random.Range(0f, 1000f); // so multiple slimes don't sync up
        _currentWobble = minWobble;
        _restRotation = transform.localRotation;
    }

    void Update()
    {
        UpdateWobble();
        UpdateRotation();
    }

    void UpdateWobble()
    {
        if (_isThinking)
        {
            _currentWobble = Mathf.Lerp(_currentWobble, thinkingWobble, Time.deltaTime * thinkingFollowSpeed);
        }
        else if (_isSpeaking)
        {
            _currentWobble = Mathf.Lerp(_currentWobble, speakingWobble, Time.deltaTime * speakingFollowSpeed);
        }
        else
        {
            // Perlin noise (0..1) sampled over time gives a smooth, non-repeating
            float n = Mathf.PerlinNoise(_noiseOffset, Time.time * driftSpeed);
            float targetWobble = Mathf.Lerp(minWobble, maxWobble, n);
            _currentWobble = Mathf.Lerp(_currentWobble, targetWobble, Time.deltaTime * followSpeed);
        }

        if (enablePoke && _pokeValue > 0f)
        {
            _pokeValue = Mathf.MoveTowards(_pokeValue, 0f, pokeDecay * Time.deltaTime);
        }

        float finalWobble = Mathf.Clamp01(_currentWobble + _pokeValue);

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetFloat(WobbleAmountId, finalWobble);
        _renderer.SetPropertyBlock(_propBlock);
    }

    void UpdateRotation()
    {
        if (_isThinking)
        {
            transform.Rotate(Vector3.up, thinkingSpinSpeed * Time.deltaTime, Space.Self);
        }
        else if (transform.localRotation != _restRotation)
        {

            transform.localRotation = Quaternion.Slerp(
                transform.localRotation, _restRotation, Time.deltaTime * rotationResetSpeed);
        }
    }


    public void SetSpeaking(bool speaking)
    {
        _isSpeaking = speaking;
    }

    public void SetThinking(bool thinking)
    {
        _isThinking = thinking;
    }

    public void Poke()
    {
        if (!enablePoke) return;
        _pokeValue = pokeStrength;
    }
}