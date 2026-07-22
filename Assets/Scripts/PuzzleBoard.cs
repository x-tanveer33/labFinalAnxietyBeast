using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PuzzleBoard : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public int[] correctOrder;           // Set size to 3, then set Element 0, 1, 2
    public int maxSlots = 3;
    
    [Header("Slots")]
    public Transform[] slotPositions;    // Drag 3 empty objects here
    
    [Header("Visuals")]
    public GameObject completionEffect;
    public Material correctMaterial;
    public Material wrongMaterial;
    
    [Header("Audio")]
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip winSound;
    
    [Header("Win")]
    public string nextSceneName;
    public float winDelay = 3f;
    
    // Internal tracking
    private List<int> placedOrder = new List<int>();
    private bool isSolved = false;
    private GameObject[] placedVisuals;
    private int collectedCount = 0;
    
    void Start()
    {
        placedVisuals = new GameObject[maxSlots];
    }
    
    // Call this when player collects a fragment
    public void CollectFragment(int fragmentID)
    {
        if (isSolved) return;
        if (collectedCount >= maxSlots) return;
        
        collectedCount++;
        Debug.Log("Collected fragment ID: " + fragmentID + " | Total: " + collectedCount + "/" + maxSlots);
        
        // Auto-place when all collected
        if (collectedCount >= maxSlots)
        {
            PlaceFragments();
        }
    }
    
    // For testing - press P to auto-solve
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ForceSolve();
        }
    }
    
    void PlaceFragments()
    {
        // For now, auto-fill with collected order (0, 1, 2)
        // In full version, this comes from player's inventory
        placedOrder.Clear();
        ClearPlacedVisuals();
        
        for (int i = 0; i < maxSlots; i++)
        {
            placedOrder.Add(i); // Place in collected order
            
            if (i < slotPositions.Length && slotPositions[i] != null)
            {
                // Create a simple cube as placeholder visual
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.transform.position = slotPositions[i].position;
                visual.transform.localScale = Vector3.one * 0.5f;
                Destroy(visual.GetComponent<Collider>()); // Remove collider
                placedVisuals[i] = visual;
            }
        }
        
        CheckSolution();
    }
    
    void CheckSolution()
    {
        if (correctOrder == null || correctOrder.Length == 0)
        {
            Debug.LogWarning("Correct Order not set! Set it in Inspector.");
            return;
        }
        
        bool isCorrect = true;
        
        for (int i = 0; i < correctOrder.Length; i++)
        {
            if (i >= placedOrder.Count || placedOrder[i] != correctOrder[i])
            {
                isCorrect = false;
                break;
            }
        }
        
        if (isCorrect)
        {
            PuzzleSolved();
        }
        else
        {
            PuzzleFailed();
        }
    }
    
    void PuzzleSolved()
    {
        isSolved = true;
        Debug.Log("PUZZLE SOLVED!");
        
        // Green glow
        foreach (GameObject visual in placedVisuals)
        {
            if (visual != null && correctMaterial != null)
            {
                Renderer rend = visual.GetComponent<Renderer>();
                if (rend != null) rend.material = correctMaterial;
            }
        }
        
        if (winSound != null)
            AudioSource.PlayClipAtPoint(winSound, transform.position);
        
        if (completionEffect != null)
            Instantiate(completionEffect, transform.position, Quaternion.identity);
        
        Invoke(nameof(LoadWinScene), winDelay);
    }
    
    void PuzzleFailed()
    {
        Debug.Log("WRONG ORDER!");
        
        // Red glow
        foreach (GameObject visual in placedVisuals)
        {
            if (visual != null && wrongMaterial != null)
            {
                Renderer rend = visual.GetComponent<Renderer>();
                if (rend != null) rend.material = wrongMaterial;
            }
        }
        
        if (wrongSound != null)
            AudioSource.PlayClipAtPoint(wrongSound, transform.position);
        
        Invoke(nameof(ResetPuzzle), 2f);
    }
    
    void ResetPuzzle()
    {
        ClearPlacedVisuals();
        placedOrder.Clear();
        collectedCount = 0;
        Debug.Log("Puzzle reset. Collect fragments again.");
    }
    
    void ClearPlacedVisuals()
    {
        if (placedVisuals == null) return;
        foreach (GameObject visual in placedVisuals)
        {
            if (visual != null) Destroy(visual);
        }
    }
    
    void LoadWinScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Debug.Log("No next scene set! Puzzle solved though.");
    }
    
    // For testing
    public void ForceSolve()
    {
        PuzzleSolved();
    }
}