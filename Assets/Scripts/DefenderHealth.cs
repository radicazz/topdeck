using UnityEngine;

public class DefenderHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 6f;
    [SerializeField] private float currentHealth;
    [Header("Health Bar")]
    [SerializeField] private float healthBarExtraHeight = 0.3f;
    private bool isDead;
    private System.Action<DefenderHealth> releaseAction;
    private EnemyHealthBar healthBar;

    public event System.Action<DefenderHealth> Died;

    public float CurrentHealth => currentHealth;

    private void Awake()
    {
        if (currentHealth <= 0f)
        {
            currentHealth = maxHealth;
        }
        isDead = false;
        ConfigureHealthBar();
    }

    public void Initialize(float health)
    {
        maxHealth = health;
        currentHealth = health;
        isDead = false;
        ConfigureHealthBar();
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
        UpdateHealthBar();
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

    private void ConfigureHealthBar()
    {
        EnsureHealthBar();
        if (healthBar == null)
        {
            return;
        }

        float barHeight = CalculateHealthBarHeight();
        healthBar.SetOffset(barHeight);
        healthBar.Initialize(maxHealth);
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }
    }

    private void EnsureHealthBar()
    {
        if (healthBar != null)
        {
            return;
        }

        Transform existing = transform.Find("HealthBar");
        if (existing != null)
        {
            healthBar = existing.GetComponent<EnemyHealthBar>();
            if (healthBar == null)
            {
                healthBar = existing.gameObject.AddComponent<EnemyHealthBar>();
            }
            LayerUtils.SetLayerRecursive(healthBar.gameObject, gameObject.layer);
            return;
        }

        GameObject barObject = new GameObject("HealthBar");
        barObject.transform.SetParent(transform, false);
        healthBar = barObject.AddComponent<EnemyHealthBar>();
        LayerUtils.SetLayerRecursive(barObject, gameObject.layer);
    }

    private float CalculateHealthBarHeight()
    {
        float height = healthBarExtraHeight;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return height;
        }

        if (HealthBarUtils.TryGetMaxLocalY(transform, renderers, out float localTop))
        {
            height = Mathf.Max(height, localTop + 0.2f);
        }

        return height;
    }
}
