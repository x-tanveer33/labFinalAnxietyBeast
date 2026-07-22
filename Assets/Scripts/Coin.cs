using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin Settings")]
    public float healthValue = 15f;
    public GameObject collectEffect; // optional particle/sound effect prefab

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.Heal(healthValue);
            }

            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}