using UnityEngine;

/// <summary>
/// Makes a Light flash repeatedly, switching to a random color each flash.
/// Attach this to any GameObject that has a Light component (Point, Spot, or Area).
/// </summary>
[RequireComponent(typeof(Light))]
public class DJLightFlash : MonoBehaviour
{
    [Header("Flash Timing")]
    [Tooltip("Minimum time (seconds) between color flashes.")]
    public float minFlashInterval = 0.05f;

    [Tooltip("Maximum time (seconds) between color flashes.")]
    public float maxFlashInterval = 0.3f;

    [Header("Light Settings")]
    [Tooltip("Random intensity range for extra sparkle. Leave both the same to disable.")]
    public float minIntensity = 2f;
    public float maxIntensity = 6f;

    [Tooltip("If true, colors are picked from the palette below. If false, fully random RGB colors are used.")]
    public bool usePalette = true;

    [Tooltip("Custom color palette (only used if Use Palette is enabled). Leave empty for a default neon set.")]
    public Color[] colorPalette;

    [Header("Optional")]
    [Tooltip("If true, the light also randomly toggles on/off like a strobe.")]
    public bool strobeEffect = false;

    private Light _light;
    private float _timer;
    private float _nextInterval;

    private static readonly Color[] DefaultPalette = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan,
        new Color(1f, 0.5f, 0f),   // orange
        new Color(0.6f, 0f, 1f)    // purple
    };

    void Awake()
    {
        _light = GetComponent<Light>();

        if (usePalette && (colorPalette == null || colorPalette.Length == 0))
        {
            colorPalette = DefaultPalette;
        }
    }

    void Start()
    {
        PickNewInterval();
        FlashNewColor();
    }

    void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= _nextInterval)
        {
            _timer = 0f;
            PickNewInterval();
            FlashNewColor();
        }
    }

    private void FlashNewColor()
    {
        // Pick color
        _light.color = usePalette
            ? colorPalette[Random.Range(0, colorPalette.Length)]
            : new Color(Random.value, Random.value, Random.value);

        // Pick intensity
        if (!Mathf.Approximately(minIntensity, maxIntensity))
        {
            _light.intensity = Random.Range(minIntensity, maxIntensity);
        }

        // Optional strobe on/off
        if (strobeEffect)
        {
            _light.enabled = Random.value > 0.15f; // mostly on, occasional blackout
        }
    }

    private void PickNewInterval()
    {
        _nextInterval = Random.Range(minFlashInterval, maxFlashInterval);
    }
}
