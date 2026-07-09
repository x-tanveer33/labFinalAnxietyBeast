using UnityEngine;

public class WinPoint : MonoBehaviour
{
    public GameObject winPanel;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Time.timeScale = 0f;
            if (winPanel != null)
                winPanel.SetActive(true);
        }
    }
}