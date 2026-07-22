using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BoxBreathingUI : MonoBehaviour
{
    public static BoxBreathingUI Instance { get; private set; }

    private GameObject panelGO;
    private Text stageText;
    private Text timerText;
    private Text instructionText;
    private Image progressFillImage;
    private Button startButton;
    private CoinPickup currentTargetCoin;
    private bool isExecuting = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartExercise(CoinPickup coin)
    {
        if (isExecuting) return;
        currentTargetCoin = coin;

        // Freeze game
        Time.timeScale = 0f;

        // Unlock mouse cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ensure UI panel exists
        EnsureUIExists();

        panelGO.SetActive(true);
        startButton.gameObject.SetActive(true);
        startButton.interactable = true;

        stageText.text = "BREATHING EXERCISE";
        timerText.text = "4-4-4";
        instructionText.color = new Color(0.95f, 0.88f, 0.8f);
        instructionText.text = "Press Start, then Hold [E] to Inhale/Exhale & [T] to Hold Breath.";
        if (progressFillImage != null) progressFillImage.fillAmount = 0f;
    }

    private void OnStartButtonClicked()
    {
        if (isExecuting) return;
        StartCoroutine(RunBreathingSequence());
    }

    private IEnumerator RunBreathingSequence()
    {
        isExecuting = true;
        startButton.gameObject.SetActive(false);

        float phaseDuration = 4f;

        // 1. INHALE (Hold E for 4s)
        yield return StartCoroutine(RunInteractivePhaseTimer("INHALE", "E", KeyCode.E, phaseDuration, fillUp: true));

        // 2. HOLD (Hold T for 4s)
        yield return StartCoroutine(RunInteractivePhaseTimer("HOLD", "T", KeyCode.T, phaseDuration, fillUp: true));

        // 3. EXHALE (Hold E for 4s)
        yield return StartCoroutine(RunInteractivePhaseTimer("EXHALE", "E", KeyCode.E, phaseDuration, fillUp: false));

        // Completion (Last Hold timer removed per user request)
        stageText.text = "COMPLETE!";
        timerText.text = "0.0s";
        instructionText.color = new Color(0.95f, 0.85f, 0.4f);
        instructionText.text = "Objective achieved! Coin collected.";
        if (progressFillImage != null) progressFillImage.fillAmount = 1f;

        yield return new WaitForSecondsRealtime(1f);

        // Resume game
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        panelGO.SetActive(false);
        isExecuting = false;

        // Reduce Anxiety significantly on breathing completion
        if (AnxietyManager.Instance != null)
        {
            AnxietyManager.Instance.ReduceAnxiety(35f);
        }

        // Complete coin collection objective
        if (currentTargetCoin != null)
        {
            currentTargetCoin.CompleteCollection();
            currentTargetCoin = null;
        }
    }

    private IEnumerator RunInteractivePhaseTimer(string phaseTitle, string keyName, KeyCode requiredKey, float duration, bool fillUp)
    {
        float elapsed = 0f;
        stageText.text = phaseTitle;

        while (elapsed < duration)
        {
            bool isHolding = Input.GetKey(requiredKey);
            if (isHolding)
            {
                elapsed += Time.unscaledDeltaTime;
                instructionText.color = new Color(0.95f, 0.82f, 0.35f); // Warm amber active feedback
                instructionText.text = "Holding [" + keyName + "]... " + phaseTitle + " in progress";
            }
            else
            {
                instructionText.color = new Color(1f, 0.6f, 0.3f); // Warm orange prompt warning
                instructionText.text = "HOLD [" + keyName + "] TO " + phaseTitle;
            }

            float remaining = Mathf.Max(0f, duration - elapsed);
            timerText.text = remaining.ToString("F1") + "s";

            float progress = Mathf.Clamp01(elapsed / duration);
            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = fillUp ? progress : (1f - progress);
            }

            yield return null;
        }
        timerText.text = "0.0s";
    }

    private void EnsureUIExists()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("BoxBreathingCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        Transform existingPanel = canvas.transform.Find("BoxBreathingPanel");
        if (existingPanel != null)
        {
            panelGO = existingPanel.gameObject;
            BindUIReferences(panelGO);
            return;
        }

        // Programmatically construct brownish transparent BoxBreathingPanel
        panelGO = new GameObject("BoxBreathingPanel");
        panelGO.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(460f, 320f);

        // Brownish translucent background
        Image bgImage = panelGO.AddComponent<Image>();
        bgImage.color = new Color(0.24f, 0.14f, 0.08f, 0.65f);

        // Warm copper outline / Border
        Outline outline = panelGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.55f, 0.25f, 0.9f);
        outline.effectDistance = new Vector2(3f, 3f);

        // Stage Title (INHALE / HOLD / EXHALE)
        GameObject stageTextGO = new GameObject("StageText");
        stageTextGO.transform.SetParent(panelGO.transform, false);
        RectTransform stageRect = stageTextGO.AddComponent<RectTransform>();
        stageRect.anchoredPosition = new Vector2(0f, 90f);
        stageRect.sizeDelta = new Vector2(420f, 50f);
        stageText = stageTextGO.AddComponent<Text>();
        stageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stageText.fontSize = 34;
        stageText.alignment = TextAnchor.MiddleCenter;
        stageText.fontStyle = FontStyle.Bold;
        stageText.color = new Color(1f, 0.82f, 0.45f);

        // Countdown Timer Text
        GameObject timerTextGO = new GameObject("TimerText");
        timerTextGO.transform.SetParent(panelGO.transform, false);
        RectTransform timerRect = timerTextGO.AddComponent<RectTransform>();
        timerRect.anchoredPosition = new Vector2(0f, 35f);
        timerRect.sizeDelta = new Vector2(420f, 50f);
        timerText = timerTextGO.AddComponent<Text>();
        timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timerText.fontSize = 44;
        timerText.alignment = TextAnchor.MiddleCenter;
        timerText.fontStyle = FontStyle.Bold;
        timerText.color = new Color(1f, 0.95f, 0.85f);

        // Progress Bar Background
        GameObject barBgGO = new GameObject("ProgressBarBg");
        barBgGO.transform.SetParent(panelGO.transform, false);
        RectTransform barBgRect = barBgGO.AddComponent<RectTransform>();
        barBgRect.anchoredPosition = new Vector2(0f, -15f);
        barBgRect.sizeDelta = new Vector2(380f, 22f);
        Image barBgImage = barBgGO.AddComponent<Image>();
        barBgImage.color = new Color(0.2f, 0.12f, 0.06f, 0.85f);

        // Progress Bar Fill (Copper / Amber fill)
        GameObject barFillGO = new GameObject("ProgressBarFill");
        barFillGO.transform.SetParent(barBgGO.transform, false);
        RectTransform barFillRect = barFillGO.AddComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.sizeDelta = Vector2.zero;
        progressFillImage = barFillGO.AddComponent<Image>();
        progressFillImage.type = Image.Type.Filled;
        progressFillImage.fillMethod = Image.FillMethod.Horizontal;
        progressFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFillImage.color = new Color(0.85f, 0.52f, 0.2f, 1f);

        // Instruction Text
        GameObject instructGO = new GameObject("InstructionText");
        instructGO.transform.SetParent(panelGO.transform, false);
        RectTransform instructRect = instructGO.AddComponent<RectTransform>();
        instructRect.anchoredPosition = new Vector2(0f, -55f);
        instructRect.sizeDelta = new Vector2(420f, 45f);
        instructionText = instructGO.AddComponent<Text>();
        instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instructionText.fontSize = 16;
        instructionText.fontStyle = FontStyle.Bold;
        instructionText.alignment = TextAnchor.MiddleCenter;
        instructionText.color = new Color(0.95f, 0.88f, 0.8f);

        // Start Breathing Button (Brownish warm button)
        GameObject btnGO = new GameObject("StartBreathingButton");
        btnGO.transform.SetParent(panelGO.transform, false);
        RectTransform btnRect = btnGO.AddComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0f, -110f);
        btnRect.sizeDelta = new Vector2(220f, 45f);
        Image btnImage = btnGO.AddComponent<Image>();
        btnImage.color = new Color(0.7f, 0.45f, 0.2f, 0.95f);

        startButton = btnGO.AddComponent<Button>();
        ColorBlock colors = startButton.colors;
        colors.highlightedColor = new Color(0.85f, 0.55f, 0.25f, 1f);
        colors.pressedColor = new Color(0.55f, 0.35f, 0.15f, 1f);
        startButton.colors = colors;

        GameObject btnTextGO = new GameObject("Text");
        btnTextGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTextRect = btnTextGO.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
        Text btnText = btnTextGO.AddComponent<Text>();
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.text = "Start Breathing";
        btnText.fontSize = 18;
        btnText.fontStyle = FontStyle.Bold;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;

        startButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void BindUIReferences(GameObject panel)
    {
        Transform stageTrans = panel.transform.Find("StageText");
        if (stageTrans != null) stageText = stageTrans.GetComponent<Text>();

        Transform timerTrans = panel.transform.Find("TimerText");
        if (timerTrans != null) timerText = timerTrans.GetComponent<Text>();

        Transform instructTrans = panel.transform.Find("InstructionText");
        if (instructTrans != null) instructionText = instructTrans.GetComponent<Text>();

        Transform barBg = panel.transform.Find("ProgressBarBg");
        if (barBg != null)
        {
            Transform fill = barBg.Find("ProgressBarFill");
            if (fill != null) progressFillImage = fill.GetComponent<Image>();
        }

        Transform btnTrans = panel.transform.Find("StartBreathingButton");
        if (btnTrans != null)
        {
            startButton = btnTrans.GetComponent<Button>();
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
    }
}
