using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public GameObject optionsPanel;
    public AudioSource menuMusic;
    public AudioSource uiAudioSource;
    public AudioClip buttonClickSound;

    public void PlayGame()
    {
        PlayButtonSound();
        SceneManager.LoadScene("LoadingScreen");
    }

    public void OpenOptions()
    {
        PlayButtonSound();
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        PlayButtonSound();
        optionsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        PlayButtonSound();
        Debug.Log("Game Quit");
        Application.Quit();
    }

    public void SetVolume(float volume)
    {
        if (menuMusic != null)
        {
            menuMusic.volume = volume;
        }
    }

    public void ToggleMusic(bool isOn)
    {
        if (menuMusic != null)
        {
            menuMusic.mute = !isOn;
        }
    }

    private void PlayButtonSound()
    {
        if (uiAudioSource != null && buttonClickSound != null)
        {
            uiAudioSource.PlayOneShot(buttonClickSound);
        }
    }
}