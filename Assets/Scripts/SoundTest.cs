using UnityEngine;

public class SoundTest : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip testClip;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (audioSource != null && testClip != null)
            {
                audioSource.clip = testClip;
                audioSource.Play();
                Debug.Log("Sound played!");
            }
            else
            {
                Debug.Log("Missing audioSource or testClip!");
            }
        }
    }
}