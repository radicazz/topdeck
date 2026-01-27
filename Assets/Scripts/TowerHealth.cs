using UnityEngine;

public class TowerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 20f;
    [SerializeField] private float currentHealth;

    public float CurrentHealth => currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
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
            GameManager.Instance?.GameOver();
        }
    }
}
