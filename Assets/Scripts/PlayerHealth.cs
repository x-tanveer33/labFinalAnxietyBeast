using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI")]
    public Slider healthSlider;
    public Image healthFillImage;
    public Text healthText; // Regular UI Text

    [Header("Damage Settings")]
    public float beastAttackDamage = 25f;
    public float damageCooldown = 1f;
    private float lastDamageTime = 0f;

    [Header("Passive Decay Settings")]
    [Tooltip("How much health to lose per second passively.")]
    public float healthDecayRate = 5f; 
    private float logTimer = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (maxHealth <= 0f) maxHealth = 100f;
        if (healthDecayRate <= 0f) healthDecayRate = 5f; // Safety fallback for Unity serialization
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    void Update()
    {
        // Passively decay health over time
        if (currentHealth > 0f && healthDecayRate > 0f)
        {
            TakeDamage(healthDecayRate * Time.deltaTime, true);

            logTimer += Time.deltaTime;
            if (logTimer >= 1f)
            {
                logTimer = 0f;
                Debug.Log("[PlayerHealth] Current Health: " + currentHealth + "/" + maxHealth + " | Slider bound: " + (healthSlider != null) + " | Slider value: " + (healthSlider != null ? healthSlider.value.ToString("F3") : "N/A"));
            }
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, false);
    }

    public void TakeDamage(float damage, bool ignoreCooldown)
    {
        if (!ignoreCooldown && (Time.time - lastDamageTime < damageCooldown)) return;

        if (!ignoreCooldown)
        {
            lastDamageTime = Time.time;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        UpdateHealthUI();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void UpdateHealthUI()
    {
        float healthPercent = currentHealth / maxHealth;

        if (healthSlider != null)
        {
            healthSlider.value = healthPercent;
        }

        if (healthFillImage != null)
        {
            if (healthPercent > 0.5f)
                healthFillImage.color = Color.green;
            else if (healthPercent > 0.25f)
                healthFillImage.color = Color.yellow;
            else
                healthFillImage.color = Color.red;
        }

        if (healthText != null)
        {
            healthText.text = Mathf.RoundToInt(currentHealth).ToString();
        }
    }

    void Die()
    {
        Debug.Log("Player Died!");
        GameOverManager.Instance?.ShowGameOver();
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHealthUI();
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }
}