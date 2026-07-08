using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;      // For 2D
    // [SerializeField] private Rigidbody rb;       // Uncomment for 3D
    
    private Vector2 movementInput;
    
    void Update()
    {
        // Get input from WASD / Arrow keys (range: -1 to 1)
        movementInput.x = Input.GetAxisRaw("Horizontal");  // A/D or Left/Right
        movementInput.y = Input.GetAxisRaw("Vertical");    // W/S or Up/Down
        
        // Normalize to prevent faster diagonal movement
        movementInput = movementInput.normalized;
    }
    
    void FixedUpdate()
    {
        // Apply movement in physics update
        rb.linearVelocity = movementInput * moveSpeed;
    }
}