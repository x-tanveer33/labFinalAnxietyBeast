using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class MemoryBoard : MonoBehaviour
{
    [Header("Board Settings")]
    public bool isSolved = false;
    public bool playerNear = false;

    [Header("Puzzle UI")]
    public GameObject puzzleUI;
    public TextMeshProUGUI feedbackText;

    [Header("Memory Buttons (drag your 3 memory buttons here)")]
    public UnityEngine.UI.Button[] memoryButtons;  // Size 3: Presentation, Friends, Certificate

    [Header("Slot Buttons (drag your 3 slot buttons here)")]
    public UnityEngine.UI.Button[] slotButtons;    // Size 3: Slot1, Slot2, Slot3

    [Header("Submit Button")]
    public UnityEngine.UI.Button submitButton;

    [Header("Feedback")]
    public AudioClip correctSound;
    public AudioClip wrongSound;
    private AudioSource audioSource;

    [Header("Visuals")]
    public Light boardLight;
    public Material glowMaterial;
    private Material originalMaterial;
    private Renderer boardRenderer;

    private string[] placedMemories = new string[3];
    private string selectedMemory = "";
    private bool puzzleOpen = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        boardRenderer = GetComponentInChildren<Renderer>();
        if (boardRenderer != null)
            originalMaterial = boardRenderer.material;

        if (boardLight != null)
            boardLight.enabled = false;

        if (puzzleUI != null)
            puzzleUI.SetActive(false);

        // Clear slots
        for (int i = 0; i < 3; i++)
            placedMemories[i] = "";

        SetupButtons();
    }

    void SetupButtons()
    {
        // Memory buttons
        if (memoryButtons.Length >= 3)
        {
            memoryButtons[0].onClick.AddListener(() => SelectMemory("Presentation"));
            memoryButtons[1].onClick.AddListener(() => SelectMemory("Friends"));
            memoryButtons[2].onClick.AddListener(() => SelectMemory("Certificate"));
        }

        // Slot buttons
        if (slotButtons.Length >= 3)
        {
            slotButtons[0].onClick.AddListener(() => PlaceInSlot(0));
            slotButtons[1].onClick.AddListener(() => PlaceInSlot(1));
            slotButtons[2].onClick.AddListener(() => PlaceInSlot(2));
        }

        // Submit
        if (submitButton != null)
            submitButton.onClick.AddListener(CheckSolution);
    }

    void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.E) && !isSolved && !puzzleOpen)
        {
            OpenPuzzle();
        }

        if (puzzleOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePuzzle();
        }

        // Hint
        if (playerNear && Input.GetKey(KeyCode.F) && puzzleOpen)
        {
            ShowFeedback("Hint: Some memories happened before others...", Color.yellow);
        }
    }

    void OpenPuzzle()
    {
        if (MemoryManager.Instance == null) return;
        if (!MemoryManager.Instance.HasCollectedAll())
        {
            ShowFeedback("Collect all 3 memories first!", Color.red);
            return;
        }

        puzzleOpen = true;
        selectedMemory = "";

        // Reset for fresh attempt
        for (int i = 0; i < 3; i++)
            placedMemories[i] = "";

        UpdateSlotDisplay();

        if (puzzleUI != null)
            puzzleUI.SetActive(true);

        ShowFeedback("Click a memory, then click a slot to place it.", Color.white);

        Debug.Log("Puzzle Opened! Order: Presentation -> Friends -> Certificate");
    }

    void ClosePuzzle()
    {
        puzzleOpen = false;
        if (puzzleUI != null)
            puzzleUI.SetActive(false);
    }

    void SelectMemory(string memoryName)
    {
        selectedMemory = memoryName;
        ShowFeedback("Selected: " + memoryName + ". Now click a slot.", Color.cyan);
    }

    void PlaceInSlot(int slotIndex)
    {
        if (string.IsNullOrEmpty(selectedMemory))
        {
            ShowFeedback("Select a memory first!", Color.red);
            return;
        }

        placedMemories[slotIndex] = selectedMemory;
        UpdateSlotDisplay();

        ShowFeedback("Placed " + selectedMemory + " in Slot " + (slotIndex + 1), Color.green);
        selectedMemory = "";

        // Auto-check if all filled
        if (!string.IsNullOrEmpty(placedMemories[0]) && 
            !string.IsNullOrEmpty(placedMemories[1]) && 
            !string.IsNullOrEmpty(placedMemories[2]))
        {
            ShowFeedback("All slots filled! Click SUBMIT.", Color.yellow);
        }
    }

    void UpdateSlotDisplay()
    {
        for (int i = 0; i < 3 && i < slotButtons.Length; i++)
        {
            TextMeshProUGUI btnText = slotButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                if (string.IsNullOrEmpty(placedMemories[i]))
                    btnText.text = "Slot " + (i + 1) + "\n[Empty]";
                else
                    btnText.text = "Slot " + (i + 1) + "\n" + placedMemories[i];
            }
        }
    }

    void CheckSolution()
    {
        if (string.IsNullOrEmpty(placedMemories[0]) || 
            string.IsNullOrEmpty(placedMemories[1]) || 
            string.IsNullOrEmpty(placedMemories[2]))
        {
            ShowFeedback("Fill all slots first!", Color.red);
            return;
        }

        List<string> order = new List<string> { placedMemories[0], placedMemories[1], placedMemories[2] };
        bool isCorrect = MemoryManager.Instance.CheckOrder(order);

        if (isCorrect)
            SolvePuzzle();
        else
            WrongAnswer();
    }

    void SolvePuzzle()
    {
        isSolved = true;
        puzzleOpen = false;

        if (correctSound != null && audioSource != null)
            audioSource.PlayOneShot(correctSound);

        if (boardLight != null)
        {
            boardLight.enabled = true;
            boardLight.color = Color.green;
        }

        if (boardRenderer != null && glowMaterial != null)
            boardRenderer.material = glowMaterial;

        ShowFeedback("CORRECT! Door unlocked!", Color.green);

        StartCoroutine(HidePuzzleSoon());

        DoorController[] doors = FindObjectsOfType<DoorController>();
        foreach (var door in doors)
            door.UnlockDoor();

        Debug.Log("PUZZLE SOLVED! Escape the School!");
    }

    void WrongAnswer()
    {
        if (wrongSound != null && audioSource != null)
            audioSource.PlayOneShot(wrongSound);

        StartCoroutine(RedFlash());

        ShowFeedback("WRONG! Your memories are still confused...", Color.red);

        // Reset
        for (int i = 0; i < 3; i++)
            placedMemories[i] = "";
        UpdateSlotDisplay();

        MemoryManager.Instance?.ResetPuzzle();

        // Beast speed up
        BeastAI beast = FindObjectOfType<BeastAI>();
        if (beast != null)
        {
            float original = beast.chaseSpeed;
            beast.chaseSpeed *= 1.5f;
            StartCoroutine(ResetBeastSpeed(beast, original));
        }
    }

    void ShowFeedback(string message, Color color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color;
        }
    }

    private System.Collections.IEnumerator HidePuzzleSoon()
    {
        yield return new WaitForSeconds(2f);
        ClosePuzzle();
    }

    private System.Collections.IEnumerator RedFlash()
    {
        if (boardLight != null)
        {
            boardLight.enabled = true;
            boardLight.color = Color.red;

            Vector3 originalPos = transform.position;
            float timer = 0f;

            while (timer < 0.5f)
            {
                timer += Time.deltaTime;
                transform.position = originalPos + Random.insideUnitSphere * 0.1f;
                yield return null;
            }

            transform.position = originalPos;
            boardLight.enabled = false;
        }
    }

    private System.Collections.IEnumerator ResetBeastSpeed(BeastAI beast, float originalSpeed)
    {
        yield return new WaitForSeconds(20f);
        if (beast != null)
            beast.chaseSpeed = originalSpeed;
        Debug.Log("Beast speed normal.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;
            if (!isSolved)
                Debug.Log("Press E to interact with Memory Board");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;
            if (puzzleOpen)
                ClosePuzzle();
        }
    }
}