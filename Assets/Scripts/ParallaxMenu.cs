using UnityEngine;

public class ParallaxMenu : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField] private float intensity = 30f;    // How far it moves
    [SerializeField] private float smoothing = 5f;    // Higher = snappier, Lower = floatier
    
    private Vector3 _startPos;
    private Vector2 _mouseOffset;

    void Start()
    {
        _startPos = transform.localPosition;
    }

    void Update()
    {
        // 1. Get Mouse Position in normalized coordinates (-0.5 to 0.5)
        float x = (Input.mousePosition.x / Screen.width) - 0.5f;
        float y = (Input.mousePosition.y / Screen.height) - 0.5f;

        // 2. Calculate target position
        Vector3 targetPos = new Vector3(
            _startPos.x + (x * intensity),
            _startPos.y + (y * intensity),
            _startPos.z
        );

        // 3. Smoothly move toward the target position (Lerp)
        transform.localPosition = Vector3.Lerp(
            transform.localPosition, 
            targetPos, 
            Time.deltaTime * smoothing
        );
    }
}