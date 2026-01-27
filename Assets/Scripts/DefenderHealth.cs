using UnityEngine;

public class DefenderHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 6f;
    [SerializeField] private float currentHealth;
    private bool isDead;
    private System.Action<DefenderHealth> releaseAction;

    public event System.Action<DefenderHealth> Died;

    public float CurrentHealth => currentHealth;

    private void Awake()
    {
        if (currentHealth <= 0f)
        {
            currentHealth = maxHealth;
        }
        isDead = false;
    }

    public void Initialize(float health)
    {
        maxHealth = health;
        currentHealth = health;
        isDead = false;
    }

    public void SetReleaseAction(System.Action<DefenderHealth> release)
    {
        releaseAction = release;
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
        if (currentHealth <= 0f)
        {
            isDead = true;
            Died?.Invoke(this);
            if (releaseAction != null)
            {
                releaseAction(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        if (!isDead)
        {
            isDead = true;
            Died?.Invoke(this);
        }
    }
}
