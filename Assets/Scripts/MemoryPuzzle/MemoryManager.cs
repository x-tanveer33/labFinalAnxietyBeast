using UnityEngine;
using System.Collections.Generic;

public class MemoryManager : MonoBehaviour
{
    public static MemoryManager Instance;

    [Header("Memory Order (Correct Sequence)")]
    public List<string> correctOrder = new List<string> 
    { 
        "Presentation", 
        "Friends", 
        "Certificate" 
    };

    [Header("UI")]
    public UnityEngine.UI.Text counterText;  // Use regular UI Text instead of TMP

    [Header("Events")]
    public bool allMemoriesCollected = false;

    private List<MemoryPickup> collectedMemories = new List<MemoryPickup>();
    private int totalMemories = 3;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateCounterUI();
    }

    public void CollectMemory(MemoryPickup memory)
    {
        if (!collectedMemories.Contains(memory))
        {
            collectedMemories.Add(memory);
            Debug.Log("[MemoryManager] Collected: " + memory.memoryName);

            // Trigger horror effects (simple version)
            TriggerHorrorEffects();

            UpdateCounterUI();

            if (collectedMemories.Count >= totalMemories)
            {
                allMemoriesCollected = true;
                Debug.Log("[MemoryManager] ALL MEMORIES COLLECTED!");
            }
        }
    }

    void UpdateCounterUI()
    {
        if (counterText != null)
        {
            counterText.text = "Memories Collected\n" + collectedMemories.Count + " / " + totalMemories;
        }
        else
        {
            Debug.Log("Memories: " + collectedMemories.Count + " / " + totalMemories);
        }
    }

    void TriggerHorrorEffects()
    {
        // Simple horror effect: flicker lights
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light.type == LightType.Point || light.type == LightType.Spot)
            {
                StartCoroutine(FlickerLight(light));
            }
        }

        Debug.Log("[Horror] Lights flicker! Beast breathing gets louder...");
    }

    private System.Collections.IEnumerator FlickerLight(Light light)
    {
        if (light == null) yield break;

        float originalIntensity = light.intensity;

        for (int i = 0; i < 5; i++)
        {
            light.intensity = originalIntensity * 0.2f;
            yield return new WaitForSeconds(0.05f);
            light.intensity = originalIntensity;
            yield return new WaitForSeconds(0.05f);
        }

        // Make it slightly darker permanently
        light.intensity = originalIntensity * 0.7f;
    }

    public List<MemoryPickup> GetCollectedMemories()
    {
        return collectedMemories;
    }

    public bool HasCollectedAll()
    {
        return allMemoriesCollected;
    }

    public bool CheckOrder(List<string> placedOrder)
    {
        if (placedOrder.Count != correctOrder.Count)
            return false;

        for (int i = 0; i < correctOrder.Count; i++)
        {
            if (placedOrder[i] != correctOrder[i])
                return false;
        }

        return true;
    }

    public void ResetPuzzle()
    {
        Debug.Log("[MemoryManager] Puzzle reset - memories returned.");
    }
}