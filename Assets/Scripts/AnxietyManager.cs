using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton manager for the player's anxiety meter.
/// Attach this script to an empty GameObject in the scene.
/// Wire the AnxietySlider field in the Inspector.
/// </summary>
public class AnxietyManager : MonoBehaviour
{
    public static AnxietyManager Instance { get; private set; }

    [Header("Anxiety Settings")]
    [Tooltip("Starting anxiety value (0-100).")]
    public float startingAnxiety = 0f;

    [Tooltip("Maximum anxiety value.")]
    public float maxAnxiety = 100f;

    [Header("UI")]
    [Tooltip("Drag the AnxietySlider / HealthBar UI element here.")]
    public Slider anxietySlider;

    [Tooltip("Optional fill image on the slider for colour feedback.")]
    public Image sliderFillImage;

    private float currentAnxiety;

    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        currentAnxiety = startingAnxiety;

        if (anxietySlider != null)
        {
            anxietySlider.minValue = 0;
            anxietySlider.maxValue = maxAnxiety;
            anxietySlider.value    = currentAnxiety;
            anxietySlider.interactable = false;

            // Auto-find fill image if not manually assigned
            if (sliderFillImage == null)
            {
                Transform fillArea = anxietySlider.transform.Find("Fill Area/Fill");
                if (fillArea != null)
                    sliderFillImage = fillArea.GetComponent<Image>();
            }
        }
        else
        {
            Debug.LogWarning("[AnxietyManager] No Slider assigned! Wire anxietySlider in the Inspector.");
        }

        UpdateAnxietyUI();
        Debug.Log("[AnxietyManager] Initialized. Starting anxiety: " + currentAnxiety);
    }

    /// <summary>Reduce anxiety by the given amount (e.g. coin pickup).</summary>
    public void ReduceAnxiety(float amount)
    {
        currentAnxiety = Mathf.Clamp(currentAnxiety - amount, 0f, maxAnxiety);
        UpdateAnxietyUI();
        Debug.Log("[AnxietyManager] Anxiety reduced by " + amount + ". Current: " + currentAnxiety);
    }

    /// <summary>Increase anxiety by the given amount (e.g. beast attack).</summary>
    public void IncreaseAnxiety(float amount)
    {
        currentAnxiety = Mathf.Clamp(currentAnxiety + amount, 0f, maxAnxiety);
        UpdateAnxietyUI();
        Debug.Log("[AnxietyManager] Anxiety increased by " + amount + ". Current: " + currentAnxiety);

        if (currentAnxiety >= maxAnxiety)
        {
            Debug.Log("[AnxietyManager] MAX ANXIETY reached — Game Over!");
            GameOverManager.Instance?.ShowGameOver();
        }
    }

    /// <summary>Returns the current anxiety value.</summary>
    public float GetCurrentAnxiety() => currentAnxiety;

    private void UpdateAnxietyUI()
    {
        if (anxietySlider == null) return;

        anxietySlider.value = currentAnxiety;

        // Colour the fill: green -> yellow -> red as anxiety rises
        if (sliderFillImage != null)
        {
            float ratio = currentAnxiety / maxAnxiety;
            if (ratio < 0.5f)
                sliderFillImage.color = Color.Lerp(Color.green, Color.yellow, ratio / 0.5f);
            else
                sliderFillImage.color = Color.Lerp(Color.yellow, Color.red, (ratio - 0.5f) / 0.5f);
        }
    }
}
