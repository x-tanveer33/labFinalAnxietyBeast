using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public float openAngle = 90f;
    public float openSpeed = 2f;

    [Header("Lock Settings")]
    public bool isLocked = true;          // NEW: Door starts locked
    public string requiredKey = "";       // NEW: For key-based doors (optional)

    private bool playerNear = false;
    private bool isOpen = false;

    private Quaternion closedRotation;
    private Quaternion openedRotation;

    void Start()
    {
        closedRotation = transform.rotation;
        openedRotation = Quaternion.Euler(
            transform.eulerAngles.x,
            transform.eulerAngles.y + openAngle,
            transform.eulerAngles.z
        );
    }

    void Update()
    {
        // Only allow opening if not locked (or if already open)
        if (playerNear && Input.GetKeyDown(KeyCode.E))
        {
            if (isLocked)
            {
                Debug.Log("The door is locked. You need to solve the memory puzzle first.");
                return;
            }

            isOpen = !isOpen;
        }

        // Smooth door rotation
        if (isOpen)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                openedRotation,
                Time.deltaTime * openSpeed
            );
        }
        else
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                closedRotation,
                Time.deltaTime * openSpeed
            );
        }
    }

    // NEW: Called by MemoryBoard when puzzle is solved
    public void UnlockDoor()
    {
        isLocked = false;
        Debug.Log("Door unlocked! You can now escape.");
    }

    // NEW: Lock the door (if needed)
    public void LockDoor()
    {
        isLocked = true;
        isOpen = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;
            if (isLocked)
            {
                Debug.Log("Door is locked. Find the Memory Board.");
            }
            else
            {
                Debug.Log("Press E to open/close door.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;
        }
    }
}