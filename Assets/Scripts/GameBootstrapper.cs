using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameBootstrapper : MonoBehaviour
{
    private static GameBootstrapper Instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
        if (Instance == null)
        {
            GameObject bootstrapperGO = new GameObject("GameBootstrapper");
            Instance = bootstrapperGO.AddComponent<GameBootstrapper>();
            DontDestroyOnLoad(bootstrapperGO);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoadedEvent;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedEvent;
    }

    private void Start()
    {
        // Run setup for the active scene when first starting
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "Level 2")
        {
            SetupLevel2();
        }
        else if (currentScene == "level 1")
        {
            SetupGame();
        }
    }

    private void OnSceneLoadedEvent(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[GameBootstrapper] Scene loaded: " + scene.name);
        if (scene.name == "Level 2")
        {
            SetupLevel2();
        }
        else if (scene.name == "level 1")
        {
            SetupGame();
        }
    }

    private void SetupGame()
    {
        Debug.Log("[GameBootstrapper] Setting up Level 1...");
        Time.timeScale = 1f;

        // Find Player
        GameObject player = GameObject.Find("ThirdPersonController");
        if (player == null)
        {
            CharacterController cc = FindAnyObjectByType<CharacterController>();
            if (cc != null) player = cc.gameObject;
        }

        if (player != null)
        {
            player.tag = "Player";

            // Elevate player slightly above floor level to prevent getting stuck in collision
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                player.transform.position = new Vector3(player.transform.position.x, Mathf.Max(player.transform.position.y, 0.5f), player.transform.position.z);
                cc.enabled = true;
            }

            // Ensure PlayerHealth component exists
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph == null)
            {
                ph = player.AddComponent<PlayerHealth>();
            }
            ph.healthDecayRate = 2f; // Level 1 decay rate
            ph.Heal(100f);
        }

        // Find Canvas
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.localScale = Vector3.one;
            }
        }

        // Setup Health Bar and wire to PlayerHealth
        WireHealthUI(player, canvas);

        // Setup GameOverManager
        GameOverManager gom = FindAnyObjectByType<GameOverManager>();
        if (gom != null)
        {
            if (canvas != null)
            {
                Transform gameOverPanelTrans = canvas.transform.Find("GameOverPanel");
                if (gameOverPanelTrans != null)
                {
                    gom.gameOverPanel = gameOverPanelTrans.gameObject;
                    gom.gameOverPanel.SetActive(false);

                    Transform retryTrans = gameOverPanelTrans.Find("Retry");
                    if (retryTrans != null)
                    {
                        gom.retryButton = retryTrans.GetComponent<Button>();
                        if (gom.retryButton != null)
                        {
                            gom.retryButton.onClick.RemoveAllListeners();
                            gom.retryButton.onClick.AddListener(gom.Retry);
                        }
                    }
                    
                    Transform mainMenuTrans = gameOverPanelTrans.Find("mainmenue");
                    if (mainMenuTrans != null)
                    {
                        gom.mainMenuButton = mainMenuTrans.GetComponent<Button>();
                        if (gom.mainMenuButton != null)
                        {
                            gom.mainMenuButton.onClick.RemoveAllListeners();
                            gom.mainMenuButton.onClick.AddListener(gom.MainMenu);
                        }
                    }
                }
            }
        }

        // Setup the Beast (BeastAI)
        BeastAI beast = FindAnyObjectByType<BeastAI>();
        if (beast != null)
        {
            beast.currentLevel = 1;
            beast.patrolSpeed = 1f;
            beast.chaseSpeed = 4.5f;
            beast.beastAttackDamage = 20f;
            
            Debug.Log("[GameBootstrapper] Beast configured for Level 1.");
        }

        // Spawn Coins for Level 1
        SpawnCoinsLevel1();
    }

    private void SetupLevel2()
    {
        Debug.Log("[GameBootstrapper] Setting up Level 2 (Terrain Cloned Scene)...");
        Time.timeScale = 1f;

        // Find Player
        GameObject player = GameObject.Find("ThirdPersonController");
        if (player == null)
        {
            CharacterController cc = FindAnyObjectByType<CharacterController>();
            if (cc != null) player = cc.gameObject;
        }

        if (player != null)
        {
            player.tag = "Player";
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            
            // Snap Player starting position on Level 2 terrain (keep close to Level 1 starting region for visibility)
            Vector3 playerPos = new Vector3(-8.56f, 0.5f, -8.18f);
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(playerPos.x, playerPos.y + 10f, playerPos.z), Vector3.down, out hit, 30f))
            {
                playerPos.y = hit.point.y + 0.5f;
            }
            player.transform.position = playerPos;
            
            if (cc != null) cc.enabled = true;
            Debug.Log("[GameBootstrapper] Repositioned player for Level 2 at ground height: " + playerPos.y);

            // Increase health decay rate in Level 2 to make it harder
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.healthDecayRate = 3.5f; // Slightly faster health decay, but easy and balanced!
                ph.Heal(100f); // Heal player back to full at level start
            }
        }

        // Make Canvas scale valid
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.localScale = Vector3.one;
            }
            // Wire health bar in Level 2 as well
            WireHealthUI(player, canvas);
        }

        // Setup GameOverManager in Level 2
        GameOverManager gom = FindAnyObjectByType<GameOverManager>();
        if (gom != null)
        {
            if (canvas != null)
            {
                Transform gameOverPanelTrans = canvas.transform.Find("GameOverPanel");
                if (gameOverPanelTrans != null)
                {
                    gom.gameOverPanel = gameOverPanelTrans.gameObject;
                    gom.gameOverPanel.SetActive(false);

                    Transform retryTrans = gameOverPanelTrans.Find("Retry");
                    if (retryTrans != null)
                    {
                        gom.retryButton = retryTrans.GetComponent<Button>();
                        if (gom.retryButton != null)
                        {
                            gom.retryButton.onClick.RemoveAllListeners();
                            gom.retryButton.onClick.AddListener(gom.Retry);
                        }
                    }
                    
                    Transform mainMenuTrans = gameOverPanelTrans.Find("mainmenue");
                    if (mainMenuTrans != null)
                    {
                        gom.mainMenuButton = mainMenuTrans.GetComponent<Button>();
                        if (gom.mainMenuButton != null)
                        {
                            gom.mainMenuButton.onClick.RemoveAllListeners();
                            gom.mainMenuButton.onClick.AddListener(gom.MainMenu);
                        }
                    }
                }
            }
        }

        // Setup the Beast in Level 2
        BeastAI beastAI = FindAnyObjectByType<BeastAI>();
        if (beastAI != null)
        {
            beastAI.gameObject.SetActive(true);
            beastAI.currentLevel = 2;
            beastAI.patrolSpeed = 1.8f;      // Balanced patrol speed (a little slow)
            beastAI.chaseSpeed = 4.8f;       // Balanced chase speed (a little slow)
            beastAI.beastAttackDamage = 30f; // Higher damage

            // Snap Beast position to the terrain (close enough to starting area but offset)
            Vector3 beastPos = new Vector3(-20f, 0.5f, -20f);
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(beastPos.x, beastPos.y + 10f, beastPos.z), Vector3.down, out hit, 30f))
            {
                beastPos.y = hit.point.y;
            }
            
            UnityEngine.AI.NavMeshAgent nma = beastAI.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (nma != null) nma.enabled = false;
            beastAI.transform.position = beastPos;
            if (nma != null) nma.enabled = true;

            // Create dynamic patrol points snapped to the terrain in the active play area
            Vector3 p1Pos = new Vector3(-5f, 0.5f, -5f);
            Vector3 p2Pos = new Vector3(-20f, 0.5f, 5f);
            Vector3 p3Pos = new Vector3(-15f, 0.5f, -25f);
            Vector3 p4Pos = new Vector3(5f, 0.5f, -10f);

            RaycastHit pHit;
            if (Physics.Raycast(new Vector3(p1Pos.x, p1Pos.y + 10f, p1Pos.z), Vector3.down, out pHit, 30f)) p1Pos.y = pHit.point.y;
            if (Physics.Raycast(new Vector3(p2Pos.x, p2Pos.y + 10f, p2Pos.z), Vector3.down, out pHit, 30f)) p2Pos.y = pHit.point.y;
            if (Physics.Raycast(new Vector3(p3Pos.x, p3Pos.y + 10f, p3Pos.z), Vector3.down, out pHit, 30f)) p3Pos.y = pHit.point.y;
            if (Physics.Raycast(new Vector3(p4Pos.x, p4Pos.y + 10f, p4Pos.z), Vector3.down, out pHit, 30f)) p4Pos.y = pHit.point.y;

            GameObject p1 = new GameObject("Level2_Patrol_1") { transform = { position = p1Pos } };
            GameObject p2 = new GameObject("Level2_Patrol_2") { transform = { position = p2Pos } };
            GameObject p3 = new GameObject("Level2_Patrol_3") { transform = { position = p3Pos } };
            GameObject p4 = new GameObject("Level2_Patrol_4") { transform = { position = p4Pos } };

            beastAI.patrolPoints = new Transform[] { p1.transform, p2.transform, p3.transform, p4.transform };
            Debug.Log("[GameBootstrapper] Beast repositioned and configured for Level 2 at height: " + beastPos.y);
        }

        // Spawn Coins for Level 2 (6 Coins spread out)
        SpawnCoinsLevel2();
    }

    private void WireHealthUI(GameObject player, Canvas canvas)
    {
        if (player == null || canvas == null) return;

        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph == null) ph = player.AddComponent<PlayerHealth>();

        Slider healthSlider = null;
        GameObject healthBarGO = GameObject.Find("HealthBar");
        if (healthBarGO != null)
        {
            healthSlider = healthBarGO.GetComponent<Slider>();
        }
        else
        {
            healthSlider = canvas.GetComponentInChildren<Slider>();
        }

        if (healthSlider != null)
        {
            ph.healthSlider = healthSlider;
            healthSlider.gameObject.SetActive(true);
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.interactable = false;

            // Set Slider Background color to dark grey so empty space is visible
            FixHealthBarVisuals(healthSlider);

            // Find Fill image under healthSlider hierarchy
            Transform fillAreaTrans = healthSlider.transform.Find("Fill Area");
            if (fillAreaTrans != null)
            {
                Transform fillTrans = fillAreaTrans.Find("Fill");
                if (fillTrans != null)
                {
                    // Reset layout offsets to fix any UI glitch hiding the fill
                    RectTransform fillRect = fillTrans.GetComponent<RectTransform>();
                    if (fillRect != null)
                    {
                        fillRect.localRotation = Quaternion.identity;
                        fillRect.localPosition = Vector3.zero;
                        fillRect.localScale = Vector3.one;
                        fillRect.anchoredPosition = Vector2.zero;
                        fillRect.sizeDelta = Vector2.zero;
                        Debug.Log("[GameBootstrapper] Reset slider fill layout offsets.");
                    }

                    Image fillImage = fillTrans.GetComponent<Image>();
                    if (fillImage != null)
                    {
                        fillImage.enabled = true;
                        ph.healthFillImage = fillImage;
                    }
                    healthSlider.fillRect = fillTrans.GetComponent<RectTransform>();
                }
            }

            GameObject healthTextGO = GameObject.Find("HealthText");
            if (healthTextGO != null)
            {
                healthTextGO.SetActive(true);
                ph.healthText = healthTextGO.GetComponent<Text>();
            }

            ph.Heal(0f); // Force update UI representation
        }
    }

    private void FixHealthBarVisuals(Slider healthSlider)
    {
        if (healthSlider == null) return;
        Transform bgTrans = healthSlider.transform.Find("Background");
        if (bgTrans != null)
        {
            Image bgImage = bgTrans.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = new Color(0.15f, 0.15f, 0.15f, 1f); // Dark charcoal grey
                Debug.Log("[GameBootstrapper] HealthBar Background color set to dark grey.");
            }
        }
    }

    private void SpawnCoinsLevel1()
    {
        CreateCoins(GetCoinPositionsNearBeast(3));
    }

    private void SpawnCoinsLevel2()
    {
        CreateCoins(GetCoinPositionsNearBeast(6));
    }

    private Vector3[] GetCoinPositionsNearBeast(int count)
    {
        BeastAI beast = FindAnyObjectByType<BeastAI>();
        Vector3 center = beast != null
            ? beast.transform.position
            : new Vector3(-9.333f, 0.5f, -26.26f);

        Vector3[] offsets = new Vector3[]
        {
            Vector3.zero,
            new Vector3(2f, 0f, 0f),
            new Vector3(-2f, 0f, 0f),
            new Vector3(0f, 0f, 2f),
            new Vector3(0f, 0f, -2f),
            new Vector3(1.5f, 0f, 1.5f),
        };

        Vector3[] positions = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = i < offsets.Length ? offsets[i] : Vector3.zero;
            positions[i] = new Vector3(center.x + offset.x, 0.5f, center.z + offset.z);
        }

        Debug.Log("[GameBootstrapper] Spawning " + count + " coins near beast at " + center);
        return positions;
    }

    private void CreateCoins(Vector3[] positions)
    {
        // Load gold material from Resources
        Material goldMaterial = Resources.Load<Material>("CoinGold");
        if (goldMaterial == null)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            if (urpLit != null)
            {
                goldMaterial = new Material(urpLit);
            }
            else
            {
                GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Material defaultMaterial = tempSphere.GetComponent<MeshRenderer>().sharedMaterial;
                Destroy(tempSphere);
                goldMaterial = new Material(defaultMaterial);
            }
            goldMaterial.name = "CoinGoldMaterial";
            goldMaterial.color = new Color(1f, 0.85f, 0f); // Bright gold/yellow
            if (goldMaterial.HasProperty("_BaseColor")) goldMaterial.SetColor("_BaseColor", new Color(1f, 0.85f, 0f));
            if (goldMaterial.HasProperty("_Color")) goldMaterial.SetColor("_Color", new Color(1f, 0.85f, 0f));
            if (goldMaterial.HasProperty("_Metallic")) goldMaterial.SetFloat("_Metallic", 0.9f);
            if (goldMaterial.HasProperty("_Smoothness")) goldMaterial.SetFloat("_Smoothness", 0.8f);
            if (goldMaterial.HasProperty("_Glossiness")) goldMaterial.SetFloat("_Glossiness", 0.8f);
        }

        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 pos = positions[i];

            // Raycast down from 10 units above to snap to floor colliders/Terrain
            RaycastHit hit;
            Vector3 origin = new Vector3(pos.x, pos.y + 10f, pos.z);
            if (Physics.Raycast(origin, Vector3.down, out hit, 30f))
            {
                pos.y = hit.point.y + 0.8f;
            }
            else if (Terrain.activeTerrain != null)
            {
                pos.y = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y + 0.8f;
            }

            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coin.name = "Coin_LevelSpawn_" + (i + 1);
            coin.transform.position = pos;
            coin.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            MeshRenderer mr = coin.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = goldMaterial;

            SphereCollider sc = coin.GetComponent<SphereCollider>();
            if (sc != null)
            {
                sc.isTrigger = true;
                sc.radius = 0.8f;
            }

            Rigidbody rb = coin.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            coin.AddComponent<CoinPickup>();
            Debug.Log("[GameBootstrapper] Spawned coin at " + pos);
        }
    }
}
