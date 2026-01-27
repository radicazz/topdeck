using UnityEngine;

public class DefenderHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 6f;
    [SerializeField] private float currentHealth;

    public float CurrentHealth => currentHealth;

    private void Awake()
    {
        if (currentHealth <= 0f)
        {
            currentHealth = maxHealth;
        }
    }

    public void Initialize(float health)
    {
        maxHealth = health;
        currentHealth = health;
    }

    public void TakeDamage(float amount)
    {
        if (GameManager.IsGameOver)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (currentHealth <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
