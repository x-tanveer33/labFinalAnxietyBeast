using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class FragmentCollector : MonoBehaviour
{
    [Header("Collection")]
    public List<MemoryFragment> collectedFragments = new List<MemoryFragment>();
    public int maxFragments = 3;

    [Header("Sequential Unlock")]
    public GameObject[] fragmentsInOrder; // drag Fragment_1, Fragment_2, Fragment_3 here, in order

    [Header("Win Point")]
    public GameObject winPoint;

    [Header("UI")]
    public bool showCollectionUI = true;
    public TMP_Text fragmentCountText;   // "Fragments: X / 3"
    public TMP_Text messageText;         // "Found: The Exam" / "Reach the final destination"

    [Header("Audio")]
    public AudioClip collectSound;

    private void Start()
    {
        // Only the first fragment is active at scene start
        for (int i = 0; i < fragmentsInOrder.Length; i++)
        {
            fragmentsInOrder[i].SetActive(i == 0);
        }

        if (winPoint != null)
            winPoint.SetActive(false);

        if (messageText != null)
        messageText.text = "";
        UpdateUI();
    }

    public bool CollectFragment(MemoryFragment fragment)
    {
        if (collectedFragments.Count >= maxFragments)
        {
            Debug.Log("Inventory full! Place fragments at puzzle board first.");
            return false;
        }

        collectedFragments.Add(fragment);

        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        Debug.Log($"Collected: {fragment.fragmentName} (ID: {fragment.fragmentID}) | Total: {collectedFragments.Count}/{maxFragments}");

        UpdateUI();

        if (messageText != null)
            messageText.text = "Found: " + fragment.fragmentName;

        // Activate the next fragment in the sequence, if any
        int nextIndex = collectedFragments.Count;
        if (nextIndex < fragmentsInOrder.Length)
        {
            fragmentsInOrder[nextIndex].SetActive(true);
        }

        // If this was the last fragment, unlock the win point
        if (HasAllFragments())
        {
            if (winPoint != null)
                winPoint.SetActive(true);

            if (messageText != null)
                messageText.text = "All fragments found! Reach the Winning Point.";
        }

        return true;
    }

    public bool HasAllFragments()
    {
        return collectedFragments.Count >= maxFragments;
    }

    public void ClearFragments()
    {
        collectedFragments.Clear();
    }

    public void RemoveFragment(MemoryFragment fragment)
    {
        collectedFragments.Remove(fragment);
    }

    private void UpdateUI()
    {
        if (fragmentCountText != null)
            fragmentCountText.text = "Fragments: " + collectedFragments.Count + " / " + maxFragments;
    }
}