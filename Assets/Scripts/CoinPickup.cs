using UnityEngine;

/// <summary>
/// Attach to a coin GameObject with a SphereCollider (Is Trigger = ON) and a Rigidbody (Is Kinematic = ON).
/// When the Player walks into the trigger, health is restored and the coin is destroyed.
/// Detection uses component-based checks so no tag is required on the player.
/// </summary>
public class CoinPickup : MonoBehaviour
{
    [Tooltip("How much health is restored when this coin is collected.")]
    public float healthRestore = 20f;

    [Tooltip("How much anxiety is reduced when this coin is collected.")]
    public float anxietyReduction = 10f;

    public bool collected = false;

    // Spin animation
    private void Update()
    {
        transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        Debug.Log("[CoinPickup] OnTriggerEnter triggered by GameObject: '" + other.name + "' with Tag: '" + other.tag + "'");
        
        bool isPl = IsPlayer(other);
        Debug.Log("[CoinPickup] IsPlayer check evaluated to: " + isPl);

        if (isPl) Collect();
    }

    private bool IsPlayer(Collider other)
    {
        // Primary: tag check
        if (other.CompareTag("Player")) return true;

        // Walk up hierarchy checking for tag
        Transform t = other.transform.parent;
        while (t != null)
        {
            if (t.CompareTag("Player")) return true;
            t = t.parent;
        }

        // Fallback: does this collider (or its root) have PlayerHealth?
        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null) return true;

        // Fallback 2: does it have a CharacterController? (player always has one)
        CharacterController cc = other.GetComponentInParent<CharacterController>();
        if (cc != null) return true;

        return false;
    }

    private void Collect()
    {
        collected = true;
        Debug.Log("[CoinPickup] '" + gameObject.name + "' collected!");

        // Restore health
        PlayerHealth ph = PlayerHealth.Instance;
        if (ph == null)
        {
            ph = FindObjectOfType<PlayerHealth>();
        }
        if (ph != null)
        {
            ph.Heal(healthRestore);
            Debug.Log("[CoinPickup] Healed player by " + healthRestore);
        }

        // Also reduce anxiety
        if (AnxietyManager.Instance != null)
            AnxietyManager.Instance.ReduceAnxiety(anxietyReduction);

        // Check if all coins collected
        CoinPickup[] allCoins = FindObjectsOfType<CoinPickup>();
        int activeRemaining = 0;
        foreach (var c in allCoins)
        {
            if (c != this && !c.collected)
            {
                activeRemaining++;
            }
        }

        Debug.Log("[CoinPickup] Remaining coins to collect: " + activeRemaining);
        if (activeRemaining == 0)
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene == "Level 2")
            {
                Debug.Log("[CoinPickup] All coins collected in Level 2! Activating WinPanel.");
                Time.timeScale = 0f;
                
                // Unlock and show mouse cursor so the player can interact with the win panel buttons
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;

                Canvas canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform winPanelTrans = canvas.transform.Find("WinPanel");
                    if (winPanelTrans != null)
                    {
                        winPanelTrans.gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                Debug.Log("[CoinPickup] All coins collected in level 1! Loading Level 2...");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Level 2");
            }
        }

        Destroy(gameObject);
    }
}
