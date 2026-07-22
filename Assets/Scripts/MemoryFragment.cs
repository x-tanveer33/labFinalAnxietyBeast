using UnityEngine;

public class MemoryFragment : MonoBehaviour
{
    [Header("Fragment Settings")]
    public int fragmentID;           // 0, 1, 2, 3 (order in puzzle)
    public string fragmentName;      // e.g., "The Exam", "The Presentation"
    
    [Header("Visuals")]
    public float rotateSpeed = 50f;
    public float floatSpeed = 1f;
    public float floatHeight = 0.3f;
    
    [Header("Audio")]
    public AudioClip pickupSound;
    
    private Vector3 startPos;
    private bool isCollected = false;
    
    void Start()
    {
        startPos = transform.position;
    }
    
    void Update()
    {
        if (isCollected) return;
        
        // Floating + rotating animation
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
        transform.position = startPos + new Vector3(0f, Mathf.Sin(Time.time * floatSpeed) * floatHeight, 0f);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        if (!other.CompareTag("Player")) return;
        
        // Try to add to player's inventory
        FragmentCollector collector = other.GetComponent<FragmentCollector>();
        if (collector != null && collector.CollectFragment(this))
        {
            isCollected = true;
            
            // Play sound
            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            
            // Hide object
            gameObject.SetActive(false);
        }
    }
}