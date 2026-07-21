using UnityEngine;
using UnityEngine.Events;

// Attach this to whatever GameObject replaces the coin.
// Requires a trigger Collider on the same object (auto-set via Reset()).
[RequireComponent(typeof(Collider))]
public class BoxBreathingInteractable : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject interactPromptUI; // e.g. "Press E" world-space or screen prompt
    [SerializeField] private bool usableOnlyOnce = true;

    [Header("References")]
    [SerializeField] private BoxBreathingController breathingController;

    [Header("Events")]
    public UnityEvent onInteract;          // fires the moment player presses E
    public UnityEvent onBreathingFinished; // fires after the full breathing sequence ends

    private bool playerInRange = false;
    private bool hasBeenUsed = false;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (playerInRange && !(usableOnlyOnce && hasBeenUsed) && Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (interactPromptUI != null) interactPromptUI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
    }

    private void Interact()
    {
        if (breathingController == null)
        {
            Debug.LogWarning($"{name}: No BoxBreathingController assigned in Inspector.");
            return;
        }

        hasBeenUsed = true;
        if (interactPromptUI != null) interactPromptUI.SetActive(false);

        onInteract.Invoke();
        breathingController.StartBreathing(OnBreathingComplete);
    }

    private void OnBreathingComplete()
    {
        onBreathingFinished.Invoke();
        // Optional: make the object disappear like a collected coin.
        // gameObject.SetActive(false);
    }
}
