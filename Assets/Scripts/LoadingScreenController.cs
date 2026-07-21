using UnityEngine;
using UnityEngine.UI;
using TMPro;  // <-- Add this for TextMeshPro
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider loadingSlider;
    public TextMeshProUGUI loadingPercentText;  // <-- Changed to TMP
    public TextMeshProUGUI loadingText;         // <-- Changed to TMP (your "Loading..." text)

    [Header("Loading Settings")]
    public float loadDuration = 3f;    // How many seconds to fill the bar
    public string sceneToLoad = "Level1"; // Scene name to load after

    private float currentProgress = 0f;

    void Start()
    {
        if (sceneToLoad == "Level1")
        {
            sceneToLoad = "level 1";
        }

        // Ensure slider starts at 0
        if (loadingSlider != null)
        {
            loadingSlider.value = 0f;
        }

        // Update initial text
        UpdateUI(0f);
    }

    void Update()
    {
        // Gradually increase progress from 0 to 1
        if (currentProgress < 1f)
        {
            currentProgress += Time.deltaTime / loadDuration;

            // Clamp to maximum 1 (100%)
            if (currentProgress > 1f)
                currentProgress = 1f;

            // Update slider value (0 to 1)
            if (loadingSlider != null)
            {
                loadingSlider.value = currentProgress;
            }

            // Update UI text
            UpdateUI(currentProgress);
        }
        else
        {
            // Loading complete - load the next scene
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    void UpdateUI(float progress)
    {
        int percent = Mathf.RoundToInt(progress * 100f);

        // Update the percent text (e.g., "0%", "50%", "100%")
        if (loadingPercentText != null)
        {
            loadingPercentText.text = percent + "%";
        }

        // Update the loading text (e.g., "Loading... 50%")
        if (loadingText != null)
        {
            loadingText.text = "Loading... " + percent + "%";
        }
    }
}