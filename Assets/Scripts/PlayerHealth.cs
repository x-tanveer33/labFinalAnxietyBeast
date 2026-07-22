using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI")]
    public Slider healthSlider;
    public Image healthFillImage;
    public TextMeshProUGUI healthText;

    [Header("Damage Settings")]
    public float beastAttackDamage = 25f;
    public float damageCooldown = 1f;
    private float lastDamageTime;

    [Header("Passive Decay")]
    public float healthDecayRate = 5f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    void Update()
    {
        if (currentHealth > 0f)
        {
            TakeDamage(healthDecayRate * Time.deltaTime, true);
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, false);
    }

    public void TakeDamage(float damage, bool ignoreCooldown)
    {
        if (!ignoreCooldown && Time.time - lastDamageTime < damageCooldown)
            return;

        if (!ignoreCooldown)
            lastDamageTime = Time.time;

        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);

        UpdateHealthUI();

        if (currentHealth <= 0)
            Die();
    }

    void UpdateHealthUI()
    {
        float percent = currentHealth / maxHealth;

        if (healthSlider != null)
            healthSlider.value = percent;

        if (healthFillImage != null)
        {
            if (percent > 0.5f)
                healthFillImage.color = Color.green;
            else if (percent > 0.25f)
                healthFillImage.color = Color.yellow;
            else
                healthFillImage.color = Color.red;
        }

        if (healthText != null)
            healthText.text = Mathf.RoundToInt(currentHealth).ToString();
    }

    void Die()
    {
        // Hide health UI
        if (healthSlider != null)
            healthSlider.gameObject.SetActive(false);

        if (healthText != null)
            healthText.gameObject.SetActive(false);

        Debug.Log("Player Died!");

        if (GameOverManager.Instance != null)
            GameOverManager.Instance.ShowGameOver();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateHealthUI();
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }
}