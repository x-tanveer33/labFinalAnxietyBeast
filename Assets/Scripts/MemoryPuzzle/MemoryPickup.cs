using UnityEngine;

public class MemoryPickup : MonoBehaviour
{
    [Header("Memory Info")]
    public string memoryName;           // e.g. "Presentation"
    public string memoryDescription;    // e.g. "My first presentation."

    [Header("Visual Effects")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.2f;
    public float rotateSpeed = 50f;

    [Header("Audio")]
    public AudioClip collectSound;
    private AudioSource audioSource;

    private Vector3 startPos;
    private bool isCollected = false;
    private bool playerInRange = false;

    void Start()
    {
        startPos = transform.position;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (isCollected) return;

        // Floating bob effect
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // Slow rotation
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

        // Check for E press while player is in range
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            CollectMemory();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("Press E to Collect Memory: " + memoryName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    void CollectMemory()
    {
        isCollected = true;
        playerInRange = false;

        // Play sound
        if (collectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectSound);
        }

        // Add to manager
        if (MemoryManager.Instance != null)
        {
            MemoryManager.Instance.CollectMemory(this);
        }

        // Visual feedback - scale down and destroy
        StartCoroutine(DestroyAfterSound());
    }

    private System.Collections.IEnumerator DestroyAfterSound()
    {
        // Small delay to let sound play
        yield return new WaitForSeconds(0.3f);

        // Scale down effect
        float timer = 0f;
        Vector3 originalScale = transform.localScale;

        while (timer < 0.5f)
        {
            timer += Time.deltaTime;
            float t = timer / 0.5f;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        // Disable visual but keep object briefly for sound
        MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
        if (mr != null) mr.gameObject.SetActive(false);
        
        Light light = GetComponent<Light>();
        if (light != null) light.enabled = false;

        yield return new WaitForSeconds(1f);

        Destroy(gameObject);
    }
}