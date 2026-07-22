using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;  // <-- ADD THIS LINE

public class WinScreenManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject winPanel;
    public TextMeshProUGUI levelNameText;
    
    [Header("Button References")]
    public GameObject nextLevelButton;
    
    [Header("Level Data")]
    public string currentLevelName;
    public string nextLevelName;
    
    private void Start()
    {
        winPanel.SetActive(false);
    }
    
    public void ShowWinScreen()
    {
        winPanel.SetActive(true);
        levelNameText.text = currentLevelName + " Complete!";
        
        if (string.IsNullOrEmpty(nextLevelName))
        {
            nextLevelButton.SetActive(false);
        }
        
        Time.timeScale = 0f;
    }
    
    public void OnNextLevelClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextLevelName);
    }
    
    public void OnRetryClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }
}