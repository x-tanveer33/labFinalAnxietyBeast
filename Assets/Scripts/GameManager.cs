using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Fragments (in collection order)")]
    public GameObject[] fragments;

    [Header("Win Point")]
    public GameObject winPoint; // the object that has WinPoint.cs on it

    [Header("UI")]
    public Text fragmentCountText;  // shows "Fragments: X / 3"
    public Text messageText;        // shows "Reach the final destination!"

    private int fragmentsCollected = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        for (int i = 0; i < fragments.Length; i++)
        {
            fragments[i].SetActive(i == 0);
        }

        if (winPoint != null)
            winPoint.SetActive(false);

        if (messageText != null)
            messageText.text = "";

        UpdateFragmentUI();
    }

    public void FragmentCollected(int fragmentIndex)
    {
        fragmentsCollected++;
        UpdateFragmentUI();

        int nextIndex = fragmentIndex + 1;
        if (nextIndex < fragments.Length)
        {
            fragments[nextIndex].SetActive(true);
        }

        if (fragmentsCollected >= fragments.Length)
        {
            if (winPoint != null)
                winPoint.SetActive(true);

            if (messageText != null)
                messageText.text = "All fragments found! Reach the final destination.";
        }
    }

    private void UpdateFragmentUI()
    {
        if (fragmentCountText != null)
            fragmentCountText.text = "Fragments: " + fragmentsCollected + " / " + fragments.Length;
    }
}