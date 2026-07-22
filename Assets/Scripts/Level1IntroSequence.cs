using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Level1IntroSequence : MonoBehaviour
{
    public static Level1IntroSequence Instance { get; private set; }

    public float introDuration = 10f;
    private GameObject introCanvasPanel;
    private GameObject cam2GO;
    private Camera mainCam;
    private Text typewriterText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartIntroSequence()
    {
        StartCoroutine(IntroSequenceRoutine());
    }

    private IEnumerator IntroSequenceRoutine()
    {
        // 1. Enable Camera (2)
        cam2GO = FindCamera2();
        mainCam = Camera.main;

        if (cam2GO != null)
        {
            cam2GO.SetActive(true);
            Camera c2 = cam2GO.GetComponent<Camera>();
            if (c2 == null) c2 = cam2GO.AddComponent<Camera>();
            c2.enabled = true;
            c2.depth = 10; // Higher depth guarantees Camera (2) takes over rendering!

            AudioListener al2 = cam2GO.GetComponent<AudioListener>();
            if (al2 != null) al2.enabled = true;

            if (mainCam != null && mainCam.gameObject != cam2GO)
            {
                mainCam.enabled = false;
            }
            Debug.Log("[Level1IntroSequence] Successfully enabled Camera (2).");
        }
        else
        {
            Debug.LogWarning("[Level1IntroSequence] Could not find 'Camera (2)' in scene!");
        }

        // 2. Enable or Create Canvas 'Intro to level 1'
        EnsureIntroCanvasPanelExists();
        if (introCanvasPanel != null)
        {
            introCanvasPanel.SetActive(true);
            Debug.Log("[Level1IntroSequence] Enabled UI Canvas 'Intro to level 1'.");
        }

        // 3. Disable Mini Map and Anxiety Meter for the 20s intro duration
        if (MiniMapTracker.Instance != null)
        {
            MiniMapTracker.Instance.SetMiniMapVisible(false);
        }
        if (AnxietyManager.Instance != null)
        {
            AnxietyManager.Instance.SetAnxietyUIVisible(false);
        }

        // 4. Start typewriter animation and wait 10 seconds concurrently
        if (typewriterText != null)
        {
            StartCoroutine(TypewriterEffect(typewriterText,
                "To survive, you must sync your movement with the Box Breathing Circle—" +
                "inhaling, holding, and exhaling to keep your heart steady and your footsteps silent.\n\n" +
                "Maintain your inner calm to navigate the dark, collect the glowing Memory Fragments, and escape.",
                introDuration * 0.85f)); // finishes just before the 10s timer ends
        }

        yield return new WaitForSeconds(introDuration);

        // 5. Disable Camera (2) and re-enable main camera
        if (cam2GO != null)
        {
            Camera c2 = cam2GO.GetComponent<Camera>();
            if (c2 != null) c2.enabled = false;
            cam2GO.SetActive(false);
        }

        if (mainCam != null)
        {
            mainCam.enabled = true;
        }

        // 6. Disable UI Canvas 'Intro to level 1'
        if (introCanvasPanel != null)
        {
            introCanvasPanel.SetActive(false);
        }

        // 7. Re-enable Mini Map and Anxiety Meter for gameplay
        if (MiniMapTracker.Instance != null)
        {
            MiniMapTracker.Instance.SetMiniMapVisible(true);
        }
        if (AnxietyManager.Instance != null)
        {
            AnxietyManager.Instance.SetAnxietyUIVisible(true);
        }

        Debug.Log("[Level1IntroSequence] 20 seconds completed. Restored gameplay cameras, Mini Map, and Anxiety UI.");
    }

    private GameObject FindCamera2()
    {
        // 1. Search active objects
        GameObject cam = GameObject.Find("Camera (2)");
        if (cam != null) return cam;

        // 2. Search loaded scene including inactive cameras
        Camera[] cams = Resources.FindObjectsOfTypeAll<Camera>();
        foreach (Camera c in cams)
        {
            if (c != null && c.gameObject.scene.isLoaded && c.name.Equals("Camera (2)", System.StringComparison.OrdinalIgnoreCase))
            {
                return c.gameObject;
            }
        }

        // 3. Search loaded scene including inactive transforms
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in transforms)
        {
            if (t != null && t.gameObject.scene.isLoaded && t.name.Equals("Camera (2)", System.StringComparison.OrdinalIgnoreCase))
            {
                return t.gameObject;
            }
        }

        return null;
    }

    private void EnsureIntroCanvasPanelExists()
    {
        // 1. Search the ENTIRE scene (active AND inactive) for 'Intro to level 1'
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t == null || !t.gameObject.scene.isLoaded) continue;
            if (t.name.Equals("Intro to level 1", System.StringComparison.OrdinalIgnoreCase))
            {
                introCanvasPanel = t.gameObject;
                // Try to grab any existing Text component for typewriter
                typewriterText = introCanvasPanel.GetComponentInChildren<Text>();
                Debug.Log("[Level1IntroSequence] Found existing 'Intro to level 1' panel at: " + GetPath(t));
                return;
            }
        }

        // 2. Not found — create on a dedicated high-priority Canvas (sortingOrder=100)
        Debug.Log("[Level1IntroSequence] Creating 'Intro to level 1' programmatically.");

        GameObject introCanvasGO = new GameObject("IntroCanvas");
        Canvas introCanvas = introCanvasGO.AddComponent<Canvas>();
        introCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        introCanvas.sortingOrder = 100;
        CanvasScaler scaler = introCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        introCanvasGO.AddComponent<GraphicRaycaster>();

        // Full-screen dim overlay
        introCanvasPanel = new GameObject("Intro to level 1");
        introCanvasPanel.transform.SetParent(introCanvas.transform, false);
        RectTransform rTransform = introCanvasPanel.AddComponent<RectTransform>();
        rTransform.anchorMin = Vector2.zero;
        rTransform.anchorMax = Vector2.one;
        rTransform.sizeDelta = Vector2.zero;
        Image bg = introCanvasPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.82f); // Deep black cinematic overlay

        // Center content box
        GameObject box = new GameObject("ContentBox");
        box.transform.SetParent(introCanvasPanel.transform, false);
        RectTransform boxRect = box.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(900f, 300f);

        // Typewriter body text — starts empty, filled by TypewriterEffect coroutine
        GameObject bodyGO = new GameObject("BodyText");
        bodyGO.transform.SetParent(box.transform, false);
        RectTransform bodyRect = bodyGO.AddComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.sizeDelta = Vector2.zero;
        bodyRect.anchoredPosition = Vector2.zero;

        typewriterText = bodyGO.AddComponent<Text>();
        typewriterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        typewriterText.text = "";                              // empty — typewriter fills it
        typewriterText.fontSize = 22;
        typewriterText.fontStyle = FontStyle.Italic;
        typewriterText.alignment = TextAnchor.MiddleCenter;
        typewriterText.color = new Color(0.9f, 0.08f, 0.08f); // RED
        typewriterText.lineSpacing = 1.4f;

        // Subtle red glow outline
        Outline glow = bodyGO.AddComponent<Outline>();
        glow.effectColor = new Color(0.6f, 0f, 0f, 0.7f);
        glow.effectDistance = new Vector2(2f, 2f);
    }

    /// <summary>Reveals <paramref name="text"/> one character at a time over <paramref name="duration"/> seconds.</summary>
    private IEnumerator TypewriterEffect(Text target, string fullText, float duration)
    {
        target.text = "";
        if (fullText.Length == 0 || duration <= 0f) yield break;

        float delay = duration / fullText.Length;
        foreach (char c in fullText)
        {
            target.text += c;
            yield return new WaitForSeconds(delay);
        }
    }

    private void CreateText(Transform parent, string goName, string content,
        Vector2 anchoredPos, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        Text t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = content;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
    }

    private string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
