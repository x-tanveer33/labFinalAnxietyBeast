using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI References")]
    public GameObject gameOverPanel;
    public Button retryButton;
    public Button mainMenuButton;

    public object HealthBar { get; private set; }


    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 1f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Wire buttons if assigned
        if (retryButton != null)
            retryButton.onClick.AddListener(Retry);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(MainMenu);
    }

    public void ShowGameOver()
    {
        Time.timeScale = 0f;

        // Unlock and show mouse cursor so the player can click UI buttons
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
           

            // If no retry button wired, auto-restart after 3 seconds (real time)
            if (retryButton == null)
                StartCoroutine(AutoRestart());
        }
        else
        {
            Debug.LogWarning("[GameOverManager] gameOverPanel is null! Auto-restarting in 3s.");
            StartCoroutine(AutoRestart());
        }
    }

    private System.Collections.IEnumerator AutoRestart()
    {
        yield return new WaitForSecondsRealtime(3f);
        Retry();
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        Instance = null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        Instance = null;
        SceneManager.LoadScene("Main Menu");
    }
}