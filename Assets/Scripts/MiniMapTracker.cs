using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniMapTracker : MonoBehaviour
{
    public static MiniMapTracker Instance { get; private set; }

    [Header("Mini Map Settings")]
    public Vector3 houseCenter = new Vector3(-4f, 0f, 2f);
    public float mapWorldSize = 35f; // World distance covered by minimap width

    private GameObject miniMapPanel;
    private RectTransform mapContentRect;
    private RectTransform playerMarkerRect;
    private Transform playerTransform;

    // Dictionary tracking active coin pickups to their UI markers
    private Dictionary<CoinPickup, Image> coinMarkers = new Dictionary<CoinPickup, Image>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetMiniMapVisible(bool visible)
    {
        EnsureMiniMapUIExists();
        if (miniMapPanel != null)
        {
            miniMapPanel.SetActive(visible);
        }
    }

    private void Start()
    {
        EnsureMiniMapUIExists();
        LocatePlayer();
        RefreshCoinMarkers();
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            LocatePlayer();
        }

        UpdatePlayerMarkerPosition();
        UpdateCoinMarkersPosition();
    }

    private void LocatePlayer()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p == null)
        {
            CharacterController cc = FindAnyObjectByType<CharacterController>();
            if (cc != null) p = cc.gameObject;
        }
        if (p != null) playerTransform = p.transform;
    }

    public void RefreshCoinMarkers()
    {
        EnsureMiniMapUIExists();
        if (mapContentRect == null) return;

        // Clear missing/destroyed coin markers
        List<CoinPickup> toRemove = new List<CoinPickup>();
        foreach (var kvp in coinMarkers)
        {
            if (kvp.Key == null || kvp.Key.collected)
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var c in toRemove) coinMarkers.Remove(c);

        // Find all active coins in the scene
        CoinPickup[] sceneCoins = FindObjectsOfType<CoinPickup>();
        foreach (var coin in sceneCoins)
        {
            if (coin == null || coin.collected || coinMarkers.ContainsKey(coin)) continue;

            // Create UI marker icon for this coin
            GameObject markerGO = new GameObject("CoinMarker_" + coin.name);
            markerGO.transform.SetParent(mapContentRect, false);

            RectTransform markerRect = markerGO.AddComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(14f, 14f);

            Image markerImg = markerGO.AddComponent<Image>();
            markerImg.color = GetFloorColor(coin.transform.position.y);

            // Add subtle outline ring to marker
            Outline o = markerGO.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.7f);
            o.effectDistance = new Vector2(1.5f, 1.5f);

            coinMarkers[coin] = markerImg;
        }

        UpdateCoinMarkersPosition();
    }

    private Color GetFloorColor(float yPos)
    {
        // Green = Ground Floor (y < 2.0m)
        // Orange = Second Floor (2.0m <= y < 4.5m)
        // Red = Roof (y >= 4.5m)
        if (yPos < 2.0f)
        {
            return new Color(0.13f, 0.78f, 0.37f); // Vibrant Green (#22C55E)
        }
        else if (yPos < 4.5f)
        {
            return new Color(0.98f, 0.45f, 0.09f); // Vibrant Orange (#F97316)
        }
        else
        {
            return new Color(0.94f, 0.27f, 0.27f); // Vibrant Red (#EF4444)
        }
    }

    private void UpdatePlayerMarkerPosition()
    {
        if (playerTransform == null || playerMarkerRect == null || mapContentRect == null) return;

        Vector2 localMapPos = WorldToMiniMapPosition(playerTransform.position);
        playerMarkerRect.anchoredPosition = localMapPos;

        // Rotate player marker with character facing direction
        float yAngle = playerTransform.eulerAngles.y;
        playerMarkerRect.localRotation = Quaternion.Euler(0f, 0f, -yAngle);
    }

    private void UpdateCoinMarkersPosition()
    {
        List<CoinPickup> destroyedCoins = new List<CoinPickup>();

        foreach (var kvp in coinMarkers)
        {
            CoinPickup coin = kvp.Key;
            Image markerImg = kvp.Value;

            if (coin == null || coin.collected)
            {
                if (markerImg != null) Destroy(markerImg.gameObject);
                destroyedCoins.Add(coin);
                continue;
            }

            if (markerImg != null)
            {
                RectTransform r = markerImg.rectTransform;
                r.anchoredPosition = WorldToMiniMapPosition(coin.transform.position);
                markerImg.color = GetFloorColor(coin.transform.position.y);
            }
        }

        foreach (var c in destroyedCoins)
        {
            coinMarkers.Remove(c);
        }
    }

    private Vector2 WorldToMiniMapPosition(Vector3 worldPos)
    {
        float mapWidth = mapContentRect.rect.width;
        float mapHeight = mapContentRect.rect.height;

        float normX = (worldPos.x - houseCenter.x) / mapWorldSize;
        float normZ = (worldPos.z - houseCenter.z) / mapWorldSize;

        float uiX = normX * mapWidth;
        float uiY = normZ * mapHeight;

        // Clamp inside minimap bounds
        float halfW = (mapWidth / 2f) - 10f;
        float halfH = (mapHeight / 2f) - 10f;

        return new Vector2(Mathf.Clamp(uiX, -halfW, halfW), Mathf.Clamp(uiY, -halfH, halfH));
    }

    private void EnsureMiniMapUIExists()
    {
        if (miniMapPanel != null) return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("MiniMapCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        Transform existingMap = canvas.transform.Find("MiniMapPanel");
        if (existingMap != null)
        {
            miniMapPanel = existingMap.gameObject;
            mapContentRect = miniMapPanel.GetComponent<RectTransform>();
            Transform pm = mapContentRect.Find("PlayerMarker");
            if (pm != null) playerMarkerRect = pm.GetComponent<RectTransform>();
            return;
        }

        // Create top-right MiniMapPanel container
        miniMapPanel = new GameObject("MiniMapPanel");
        miniMapPanel.transform.SetParent(canvas.transform, false);

        mapContentRect = miniMapPanel.AddComponent<RectTransform>();
        mapContentRect.anchorMin = new Vector2(1f, 1f);
        mapContentRect.anchorMax = new Vector2(1f, 1f);
        mapContentRect.pivot = new Vector2(1f, 1f);
        mapContentRect.anchoredPosition = new Vector2(-20f, -20f);
        mapContentRect.sizeDelta = new Vector2(160f, 160f);

        Image mapBg = miniMapPanel.AddComponent<Image>();
        mapBg.color = new Color(0.08f, 0.09f, 0.12f, 0.75f); // Translucent dark map bg

        Outline mapOutline = miniMapPanel.AddComponent<Outline>();
        mapOutline.effectColor = new Color(0.3f, 0.7f, 0.9f, 0.85f);
        mapOutline.effectDistance = new Vector2(2f, 2f);

        // Header Title
        GameObject headerGO = new GameObject("MapTitle");
        headerGO.transform.SetParent(miniMapPanel.transform, false);
        RectTransform headerRect = headerGO.AddComponent<RectTransform>();
        headerRect.anchoredPosition = new Vector2(0f, 72f);
        headerRect.sizeDelta = new Vector2(150f, 20f);
        Text titleText = headerGO.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.text = "MINI MAP";
        titleText.fontSize = 11;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.4f, 0.9f, 1f);

        // Player Marker (Cyan arrow / dot)
        GameObject pMarkerGO = new GameObject("PlayerMarker");
        pMarkerGO.transform.SetParent(miniMapPanel.transform, false);
        playerMarkerRect = pMarkerGO.AddComponent<RectTransform>();
        playerMarkerRect.anchorMin = new Vector2(0.5f, 0.5f);
        playerMarkerRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerMarkerRect.pivot = new Vector2(0.5f, 0.5f);
        playerMarkerRect.sizeDelta = new Vector2(14f, 14f);

        Image pImg = pMarkerGO.AddComponent<Image>();
        pImg.color = new Color(0.2f, 0.95f, 1f); // Bright Cyan
        Outline pOut = pMarkerGO.AddComponent<Outline>();
        pOut.effectColor = Color.black;
        pOut.effectDistance = new Vector2(1f, 1f);

        // Bottom Floor Legend Text (Green = Ground, Orange = 2nd Fl, Red = Roof)
        GameObject legendGO = new GameObject("MapLegend");
        legendGO.transform.SetParent(miniMapPanel.transform, false);
        RectTransform legendRect = legendGO.AddComponent<RectTransform>();
        legendRect.anchoredPosition = new Vector2(0f, -72f);
        legendRect.sizeDelta = new Vector2(160f, 20f);
        Text legendText = legendGO.AddComponent<Text>();
        legendText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        legendText.text = "● Ground  ● 2nd  ● Roof";
        legendText.fontSize = 10;
        legendText.fontStyle = FontStyle.Bold;
        legendText.alignment = TextAnchor.MiddleCenter;
        legendText.color = new Color(0.9f, 0.9f, 0.95f);
    }
}
