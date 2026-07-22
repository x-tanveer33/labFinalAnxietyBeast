using System;
using UnityEngine;
using UnityEngine.UI;

public class AnxietyManager : MonoBehaviour
{
    public static AnxietyManager Instance { get; private set; }

    [Header("Anxiety Settings")]
    [Range(0f, 100f)] public float startingAnxiety = 0f;
    public float maxAnxiety = 100f;

    [Header("Rates & Dynamics")]
    public float sprintRiseRate = 4.0f;       // Rise rate when running/sprinting
    public float moveRiseRate = 1.5f;         // Rise rate when moving
    public float proximityFactor = 2.0f;      // Rise multiplier when close to Beast
    public float naturalDecayRate = 1.8f;     // Natural decay rate in safe areas
    public float breathingReductionAmount = 35f; // Fast drop on breathing exercise

    [Header("UI References")]
    public Slider anxietySlider;
    public Image sliderFillImage;
    public Text anxietyText;

    private float currentAnxiety = 0f;

    // Visual overlay panels for tiered effects
    private GameObject canvasGO;
    private Image vignetteOverlay; // Dark edges (25%-33%)
    private Image blurOverlay;     // Vision blur (34%-66%)
    private Image blackoutOverlay; // Blackout panel (67%-100%)

    // References to active entities
    private ThirdPersonController playerController;
    private BeastAI beastAI;
    private float originalPlayerVelocity = 5f;
    private float originalBeastDetectionRadius = 8.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetAnxietyUIVisible(bool visible)
    {
        EnsureUIAndOverlaysExist();
        if (anxietySlider != null)
        {
            anxietySlider.gameObject.SetActive(visible);
        }
    }

    private void Start()
    {
        currentAnxiety = Mathf.Clamp(startingAnxiety, 0f, maxAnxiety);

        // Find references
        playerController = FindAnyObjectByType<ThirdPersonController>();
        if (playerController != null)
        {
            originalPlayerVelocity = playerController.velocity;
        }

        beastAI = FindAnyObjectByType<BeastAI>();
        if (beastAI != null)
        {
            originalBeastDetectionRadius = beastAI.detectionRadius;
        }

        EnsureUIAndOverlaysExist();
        UpdateAnxietyUI();
        ApplyTieredEffects();

        Debug.Log("[AnxietyManager] System Initialized at top-center. Initial anxiety: " + currentAnxiety);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return; // Paused (e.g. Box Breathing or Game Over)

        // Refresh references if missing
        if (playerController == null) playerController = FindAnyObjectByType<ThirdPersonController>();
        if (beastAI == null) beastAI = FindAnyObjectByType<BeastAI>();

        CalculateAnxietyDynamics();
        UpdateAnxietyUI();
        ApplyTieredEffects();
    }

    private void CalculateAnxietyDynamics()
    {
        float deltaAnxiety = 0f;

        bool isCrouching = false;
        bool isMoving = false;
        bool isSprinting = false;

        if (playerController != null)
        {
            isCrouching = playerController.IsCrouching;
            CharacterController cc = playerController.GetComponent<CharacterController>();
            if (cc != null && cc.velocity.sqrMagnitude > 0.15f)
            {
                isMoving = true;
                isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }
        }

        float distToBeast = 999f;
        if (beastAI != null)
        {
            distToBeast = Vector3.Distance(transform.position, beastAI.transform.position);
            if (playerController != null)
            {
                distToBeast = Vector3.Distance(playerController.transform.position, beastAI.transform.position);
            }
        }

        // 1. Movement Anxiety Dynamics
        if (isMoving)
        {
            if (isCrouching)
            {
                // CROUCH WALK: Anxiety DOES NOT RISE while crouch walking!
                deltaAnxiety += 0f;
            }
            else
            {
                // Standing Walk / Sprint
                deltaAnxiety += (isSprinting ? sprintRiseRate : moveRiseRate) * Time.deltaTime;
            }
        }

        // 2. Check Beast Proximity (only when close to Beast)
        if (distToBeast < 12f)
        {
            float proximityIntensity = Mathf.Clamp01((12f - distToBeast) / 12f);
            deltaAnxiety += (proximityIntensity * 6f * proximityFactor) * Time.deltaTime;
        }

        // 3. Decay Dynamics (Crouch Idle vs Safe Area Natural Decay)
        if (!isMoving && isCrouching)
        {
            // CROUCH IDLE: Reduces anxiety TWICE as fast if not in close distance with the beast (>8.0m)
            if (distToBeast > 8.0f)
            {
                deltaAnxiety -= (naturalDecayRate * 2.0f) * Time.deltaTime; // 2x reduction rate!
            }
        }
        else if (distToBeast > 16f && !isSprinting)
        {
            // Standard Natural Decay in safe area
            deltaAnxiety -= naturalDecayRate * Time.deltaTime;
        }

        currentAnxiety = Mathf.Clamp(currentAnxiety + deltaAnxiety, 0f, maxAnxiety);

        // Check 100% Game Over Trigger
        if (currentAnxiety >= maxAnxiety)
        {
            Debug.Log("[AnxietyManager] 100% Max Anxiety Reached - Game Over!");
            if (GameOverManager.Instance != null)
            {
                GameOverManager.Instance.ShowGameOver();
            }
        }
    }

    public void ReduceAnxiety(float amount)
    {
        currentAnxiety = Mathf.Clamp(currentAnxiety - amount, 0f, maxAnxiety);
        UpdateAnxietyUI();
        ApplyTieredEffects();
        Debug.Log("[AnxietyManager] Reduced anxiety by " + amount + ". Current: " + currentAnxiety);
    }

    public void IncreaseAnxiety(float amount)
    {
        currentAnxiety = Mathf.Clamp(currentAnxiety + amount, 0f, maxAnxiety);
        UpdateAnxietyUI();
        ApplyTieredEffects();
        Debug.Log("[AnxietyManager] Increased anxiety by " + amount + ". Current: " + currentAnxiety);

        if (currentAnxiety >= maxAnxiety && GameOverManager.Instance != null)
        {
            GameOverManager.Instance.ShowGameOver();
        }
    }

    public float GetCurrentAnxiety() => currentAnxiety;

    private void ApplyTieredEffects()
    {
        float pct = (currentAnxiety / maxAnxiety) * 100f;

        // Tier 1: 0% - 24% (Normal)
        // Tier 2: 25% - 33% (Edge Darkening Vignette)
        // Tier 3: 34% - 66% (Vision Blur, Heavy Sluggish Controls, Beast Hearing Expansion, Chaotic Audio)
        // Tier 4: 67% - 100% (Screen Blackout -> Game Over)

        // 1. Dark Edges Vignette (starts at 80%)
        if (vignetteOverlay != null)
        {
            if (pct >= 80f)
            {
                float vigAlpha = Mathf.Clamp01((pct - 80f) / 20f) * 0.5f;
                vignetteOverlay.color = new Color(0f, 0f, 0f, vigAlpha);
            }
            else
            {
                vignetteOverlay.color = Color.clear;
            }
        }

        // 2. Vision Blur Overlay (34% - 79%)
        if (blurOverlay != null)
        {
            if (pct >= 34f && pct < 80f)
            {
                float blurAlpha = Mathf.Clamp01((pct - 34f) / 46f) * 0.4f;
                // Add gentle pulse
                float pulse = (Mathf.Sin(Time.unscaledTime * 3f) + 1f) * 0.08f;
                blurOverlay.color = new Color(0.1f, 0.02f, 0.04f, Mathf.Clamp01(blurAlpha + pulse));
            }
            else
            {
                blurOverlay.color = Color.clear;
            }
        }

        // 3. Blackout Overlay (80% - 100%)
        if (blackoutOverlay != null)
        {
            if (pct >= 80f)
            {
                float blackAlpha = Mathf.Clamp01((pct - 80f) / 20f);
                blackoutOverlay.color = new Color(0f, 0f, 0f, blackAlpha);
            }
            else
            {
                blackoutOverlay.color = Color.clear;
            }
        }

        // 4. Sluggish Controls / Heavy Movement (34% - 66%)
        if (playerController != null)
        {
            if (pct >= 34f)
            {
                float sluggishFactor = Mathf.Lerp(1.0f, 0.55f, Mathf.Clamp01((pct - 34f) / 32f));
                playerController.velocity = originalPlayerVelocity * sluggishFactor;
            }
            else
            {
                playerController.velocity = originalPlayerVelocity;
            }
        }

        // 5. Increased Beast Hearing / Detection Range (34% - 66%)
        if (beastAI != null)
        {
            if (pct >= 34f)
            {
                float hearingMultiplier = Mathf.Lerp(1.0f, 1.8f, Mathf.Clamp01((pct - 34f) / 32f));
                beastAI.detectionRadius = originalBeastDetectionRadius * hearingMultiplier;
            }
            else
            {
                beastAI.detectionRadius = originalBeastDetectionRadius;
            }
        }
    }

    private void UpdateAnxietyUI()
    {
        if (anxietySlider != null)
        {
            anxietySlider.value = currentAnxiety;

            if (sliderFillImage != null)
            {
                float ratio = currentAnxiety / maxAnxiety;
                if (ratio < 0.34f)
                    sliderFillImage.color = Color.Lerp(new Color(0.2f, 0.8f, 0.3f), Color.yellow, ratio / 0.34f);
                else if (ratio < 0.80f)
                    sliderFillImage.color = Color.Lerp(Color.yellow, new Color(1f, 0.5f, 0f), (ratio - 0.34f) / 0.46f);
                else
                    sliderFillImage.color = Color.Lerp(new Color(1f, 0.5f, 0f), Color.red, (ratio - 0.80f) / 0.20f);
            }
        }

        if (anxietyText != null)
        {
            anxietyText.text = "ANXIETY " + Mathf.RoundToInt((currentAnxiety / maxAnxiety) * 100f) + "%";
        }
    }

    private void EnsureUIAndOverlaysExist()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cGO = new GameObject("AnxietyCanvas");
            canvas = cGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cGO.AddComponent<CanvasScaler>();
            cGO.AddComponent<GraphicRaycaster>();
        }
        canvasGO = canvas.gameObject;

        // 1. Build Top-Center Anxiety Bar if not assigned
        if (anxietySlider == null)
        {
            Transform existingSlider = canvas.transform.Find("TopCenterAnxietyBar");
            if (existingSlider != null)
            {
                anxietySlider = existingSlider.GetComponent<Slider>();
            }
            else
            {
                GameObject barGO = new GameObject("TopCenterAnxietyBar");
                barGO.transform.SetParent(canvas.transform, false);

                RectTransform barRect = barGO.AddComponent<RectTransform>();
                barRect.anchorMin = new Vector2(0.5f, 1f);
                barRect.anchorMax = new Vector2(0.5f, 1f);
                barRect.pivot = new Vector2(0.5f, 1f);
                barRect.anchoredPosition = new Vector2(0f, -20f);
                barRect.sizeDelta = new Vector2(320f, 22f);

                Image bgImage = barGO.AddComponent<Image>();
                bgImage.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);

                Outline outline = barGO.AddComponent<Outline>();
                outline.effectColor = new Color(0.6f, 0.2f, 0.2f, 0.8f);
                outline.effectDistance = new Vector2(2f, 2f);

                anxietySlider = barGO.AddComponent<Slider>();
                anxietySlider.minValue = 0f;
                anxietySlider.maxValue = maxAnxiety;
                anxietySlider.value = currentAnxiety;

                // Fill Area
                GameObject fillArea = new GameObject("Fill Area");
                fillArea.transform.SetParent(barGO.transform, false);
                RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
                fillAreaRect.anchorMin = Vector2.zero;
                fillAreaRect.anchorMax = Vector2.one;
                fillAreaRect.sizeDelta = Vector2.zero;

                GameObject fill = new GameObject("Fill");
                fill.transform.SetParent(fillArea.transform, false);
                RectTransform fillRect = fill.AddComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.sizeDelta = Vector2.zero;
                sliderFillImage = fill.AddComponent<Image>();
                sliderFillImage.color = new Color(0.2f, 0.8f, 0.3f);

                anxietySlider.fillRect = fillRect;

                // Label Text
                GameObject textGO = new GameObject("AnxietyText");
                textGO.transform.SetParent(barGO.transform, false);
                RectTransform textRect = textGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                anxietyText = textGO.AddComponent<Text>();
                anxietyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                anxietyText.text = "ANXIETY 0%";
                anxietyText.fontSize = 14;
                anxietyText.fontStyle = FontStyle.Bold;
                anxietyText.alignment = TextAnchor.MiddleCenter;
                anxietyText.color = Color.white;
            }
        }

        // 2. Build Vignette Edge Overlay Panel (25%-33%)
        Transform vTrans = canvas.transform.Find("AnxietyVignetteOverlay");
        if (vTrans != null)
        {
            vignetteOverlay = vTrans.GetComponent<Image>();
        }
        else
        {
            GameObject vGO = new GameObject("AnxietyVignetteOverlay");
            vGO.transform.SetParent(canvas.transform, false);
            vGO.transform.SetAsFirstSibling(); // Behind text, in front of world
            RectTransform vRect = vGO.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;
            vRect.sizeDelta = Vector2.zero;
            vignetteOverlay = vGO.AddComponent<Image>();
            vignetteOverlay.color = Color.clear;
            vignetteOverlay.raycastTarget = false;
        }

        // 3. Build Vision Blur Overlay Panel (34%-66%)
        Transform bTrans = canvas.transform.Find("AnxietyBlurOverlay");
        if (bTrans != null)
        {
            blurOverlay = bTrans.GetComponent<Image>();
        }
        else
        {
            GameObject bGO = new GameObject("AnxietyBlurOverlay");
            bGO.transform.SetParent(canvas.transform, false);
            bGO.transform.SetSiblingIndex(1);
            RectTransform bRect = bGO.AddComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.sizeDelta = Vector2.zero;
            blurOverlay = bGO.AddComponent<Image>();
            blurOverlay.color = Color.clear;
            blurOverlay.raycastTarget = false;
        }

        // 4. Build Blackout Overlay Panel (67%-100%)
        Transform boTrans = canvas.transform.Find("AnxietyBlackoutOverlay");
        if (boTrans != null)
        {
            blackoutOverlay = boTrans.GetComponent<Image>();
        }
        else
        {
            GameObject boGO = new GameObject("AnxietyBlackoutOverlay");
            boGO.transform.SetParent(canvas.transform, false);
            boGO.transform.SetSiblingIndex(2);
            RectTransform boRect = boGO.AddComponent<RectTransform>();
            boRect.anchorMin = Vector2.zero;
            boRect.anchorMax = Vector2.one;
            boRect.sizeDelta = Vector2.zero;
            blackoutOverlay = boGO.AddComponent<Image>();
            blackoutOverlay.color = Color.clear;
            blackoutOverlay.raycastTarget = false;
        }
    }
}
