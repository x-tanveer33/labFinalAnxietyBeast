using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

// Attach this to a manager object (or the breathing Canvas itself).
// It freezes gameplay (Time.timeScale = 0) while running the breathing
// animation on UNSCALED time, so the UI keeps moving while everything
// else in the scene is frozen.
public class BoxBreathingController : MonoBehaviour
{
    private enum Phase { Inhale, HoldFull, Exhale, HoldEmpty }

    [Header("UI References")]
    [SerializeField] private GameObject breathingRoot;   // parent panel, disabled by default
    [SerializeField] private RectTransform indicatorDot; // the dot that travels around the box
    [SerializeField] private TMP_Text instructionText;   // "Inhale...", "Hold...", "Exhale..."
    [SerializeField] private TMP_Text cycleCountText;    // optional "Cycle 1 / 3"

    [Header("Box Corners (4 empty RectTransforms placed at the square's corners)")]
    [SerializeField] private RectTransform bottomLeft;
    [SerializeField] private RectTransform topLeft;
    [SerializeField] private RectTransform topRight;
    [SerializeField] private RectTransform bottomRight;

    [Header("Timing")]
    [SerializeField] private float phaseDuration = 4f; // classic box breathing = 4s per side
    [SerializeField] private int cyclesToComplete = 3;

    [Header("Player Lock (drag movement/look/interact scripts here)")]
    [SerializeField] private MonoBehaviour[] scriptsToDisableDuringBreathing;

    [Header("Events")]
    public UnityEvent onBreathingStart;
    public UnityEvent onBreathingEnd;

    private Coroutine breathingRoutine;
    private System.Action onCompleteCallback;

    public bool IsBreathing { get; private set; }

    public void StartBreathing(System.Action onComplete = null)
    {
        if (IsBreathing) return;

        onCompleteCallback = onComplete;
        IsBreathing = true;

        Time.timeScale = 0f; // freezes everything driven by Time.deltaTime / physics

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        foreach (var script in scriptsToDisableDuringBreathing)
        {
            if (script != null) script.enabled = false;
        }

        if (breathingRoot != null) breathingRoot.SetActive(true);
        onBreathingStart.Invoke();

        breathingRoutine = StartCoroutine(RunBreathingCycles());
    }

    private IEnumerator RunBreathingCycles()
    {
        for (int cycle = 1; cycle <= cyclesToComplete; cycle++)
        {
            if (cycleCountText != null)
                cycleCountText.text = $"Cycle {cycle} / {cyclesToComplete}";

            yield return RunPhase(bottomLeft, topLeft, "Inhale...");
            yield return RunPhase(topLeft, topRight, "Hold...");
            yield return RunPhase(topRight, bottomRight, "Exhale...");
            yield return RunPhase(bottomRight, bottomLeft, "Hold...");
        }

        EndBreathing();
    }

    private IEnumerator RunPhase(RectTransform from, RectTransform to, string label)
    {
        if (instructionText != null) instructionText.text = label;

        float elapsed = 0f;
        Vector2 startPos = from.anchoredPosition;
        Vector2 endPos = to.anchoredPosition;

        while (elapsed < phaseDuration)
        {
            // unscaledDeltaTime keeps this running even though Time.timeScale = 0
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / phaseDuration);

            if (indicatorDot != null)
                indicatorDot.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            yield return null;
        }
    }

    private void EndBreathing()
    {
        if (breathingRoot != null) breathingRoot.SetActive(false);

        Time.timeScale = 1f;

        foreach (var script in scriptsToDisableDuringBreathing)
        {
            if (script != null) script.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        IsBreathing = false;
        onBreathingEnd.Invoke();
        onCompleteCallback?.Invoke();
        onCompleteCallback = null;
    }

    private void OnDisable()
    {
        // Safety net: never leave the game permanently frozen if this object
        // gets disabled/destroyed mid-sequence.
        if (IsBreathing)
        {
            Time.timeScale = 1f;
        }
    }
}
