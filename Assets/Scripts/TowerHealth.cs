using UnityEngine;

public class TowerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 20f;
    [SerializeField] private float currentHealth;
    private bool isDead;

    public event System.Action<float> HealthChanged;
    public event System.Action Died;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
        HealthChanged?.Invoke(currentHealth);
    }

    public void Initialize(float newMaxHealth, bool healToFull = true)
    {
        maxHealth = Mathf.Max(0.1f, newMaxHealth);
        currentHealth = healToFull ? maxHealth : Mathf.Min(currentHealth, maxHealth);
        isDead = currentHealth <= 0f;
        HealthChanged?.Invoke(currentHealth);
    }

    public void TakeDamage(float amount)
    {
        if (GameManager.IsGameOver)
        {
            return;
        }

        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        HealthChanged?.Invoke(currentHealth);
        if (currentHealth <= 0f)
        {
            isDead = true;
            Died?.Invoke();
            GameManager.Instance?.GameOver();
        }
    }
}
