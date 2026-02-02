using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyAttackPriority
{
    TowerFirst,
    DefenderFirst
}

public struct EnemyConfig
{
    public int TypeId;
    public float Speed;
    public float MaxHealth;
    public float DamageToTower;
    public float HeightOffset;
    public float DefenderAttackRange;
    public float DefenderAttackInterval;
    public float DamageToDefender;
    public float TowerAttackRange;
    public float TowerAttackInterval;
    public LayerMask DefenderTargetMask;
    public EnemyAttackPriority AttackPriority;
    public float DamageTakenMultiplier;
    public bool EnrageOnLowHealth;
    public float EnrageHealthFraction;
    public float EnrageSpeedMultiplier;
    public float EnrageDamageMultiplier;
    public float EnrageAttackIntervalMultiplier;
}

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float damageToTower = 1f;
    [SerializeField] private float heightOffset = 0.5f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float healthBarExtraHeight = 0.3f;

    [Header("Tower Attack")]
    [SerializeField] private float towerAttackRange = 1.4f;
    [SerializeField] private float towerAttackInterval = 0.8f;

    [Header("Defender Attack")]
    [SerializeField] private float defenderAttackRange = 1.2f;
    [SerializeField] private float defenderAttackInterval = 0.6f;
    [SerializeField] private float damageToDefender = 1f;
    [SerializeField] private LayerMask defenderTargetMask = ~0;
    [SerializeField, Min(1)] private int defenderQueryBufferSize = 12;

    private float currentHealth;
    private float baseMoveSpeed;
    private float baseDamageToTower;
    private float baseDamageToDefender;
    private float baseTowerAttackInterval;
    private float baseDefenderAttackInterval;
    private IReadOnlyList<Vector3> path;
    private int pathIndex;
    private TowerHealth tower;
    private float attackTimer;
    private DefenderHealth defenderTarget;
    private float towerAttackTimer;
    private bool reachedTower;
    private bool isDead;
    private bool isEnraged;
    private Collider[] defenderHits;
    private Action<Enemy> releaseAction;
    private EnemyAttackPriority attackPriority;
    private float damageTakenMultiplier = 1f;
    private bool enrageOnLowHealth;
    private float enrageHealthFraction;
    private float enrageSpeedMultiplier;
    private float enrageDamageMultiplier;
    private float enrageAttackIntervalMultiplier;
    private int typeId = -1;
    private EnemyHealthBar healthBar;

    public event Action<Enemy> Died;
    public int TypeId => typeId;

    public void AssignTypeId(int id)
    {
        typeId = id;
    }

    private void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
        defenderHits = new Collider[Mathf.Max(1, defenderQueryBufferSize)];
    }

    public void Initialize(IReadOnlyList<Vector3> pathPoints, TowerHealth towerRef, EnemyConfig config)
    {
        path = pathPoints;
        tower = towerRef;
        typeId = config.TypeId;
        moveSpeed = config.Speed;
        maxHealth = config.MaxHealth;
        currentHealth = config.MaxHealth;
        damageToTower = config.DamageToTower;
        heightOffset = config.HeightOffset;
        defenderAttackRange = config.DefenderAttackRange;
        defenderAttackInterval = config.DefenderAttackInterval;
        damageToDefender = config.DamageToDefender;
        towerAttackRange = config.TowerAttackRange;
        towerAttackInterval = config.TowerAttackInterval;
        defenderTargetMask = config.DefenderTargetMask;
        attackPriority = config.AttackPriority;
        damageTakenMultiplier = Mathf.Max(0.05f, config.DamageTakenMultiplier);
        enrageOnLowHealth = config.EnrageOnLowHealth;
        enrageHealthFraction = Mathf.Clamp01(config.EnrageHealthFraction);
        enrageSpeedMultiplier = Mathf.Max(0.1f, config.EnrageSpeedMultiplier);
        enrageDamageMultiplier = Mathf.Max(0.1f, config.EnrageDamageMultiplier);
        enrageAttackIntervalMultiplier = Mathf.Max(0.1f, config.EnrageAttackIntervalMultiplier);
        baseMoveSpeed = moveSpeed;
        baseDamageToTower = damageToTower;
        baseDamageToDefender = damageToDefender;
        baseTowerAttackInterval = towerAttackInterval;
        baseDefenderAttackInterval = defenderAttackInterval;
        pathIndex = 0;
        attackTimer = 0f;
        towerAttackTimer = 0f;
        defenderTarget = null;
        reachedTower = false;
        isDead = false;
        isEnraged = false;

        if (path != null && path.Count > 0)
        {
            transform.position = path[0] + Vector3.up * heightOffset;
        }

        ConfigureHealthBar();
    }

    public void SetReleaseAction(Action<Enemy> release)
    {
        releaseAction = release;
    }

    private void Update()
    {
        if (path == null || path.Count == 0 || GameManager.IsGameOver)
        {
            return;
        }

        UpdateEnrageIfNeeded();

        if (attackPriority == EnemyAttackPriority.DefenderFirst)
        {
            if (TryAttackDefender())
            {
                return;
            }

            if (TryAttackTower())
            {
                return;
            }
        }
        else
        {
            if (TryAttackTower())
            {
                return;
            }

            if (TryAttackDefender())
            {
                return;
            }
        }

        Vector3 target = path[pathIndex] + Vector3.up * heightOffset;
        FaceTowards(target);
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if ((transform.position - target).sqrMagnitude < 0.01f)
        {
            pathIndex++;
            if (pathIndex >= path.Count)
            {
                reachedTower = true;
            }
        }
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

        float adjusted = amount * damageTakenMultiplier;
        currentHealth -= adjusted;
        UpdateHealthBar();
        if (currentHealth <= 0f)
        {
            isDead = true;
            Died?.Invoke(this);
            GameManager.Instance?.OnEnemyKilled();
            Release();
        }
        else
        {
            UpdateEnrageIfNeeded();
        }
    }

    private bool TryAttackDefender()
    {
        if (defenderTarget == null)
        {
            defenderTarget = FindDefenderInRange();
        }

        if (defenderTarget == null)
        {
            return false;
        }

        float sqrDistance = (defenderTarget.transform.position - transform.position).sqrMagnitude;
        float range = defenderAttackRange * defenderAttackRange;
        if (sqrDistance > range)
        {
            defenderTarget = null;
            return false;
        }

        FaceTowards(defenderTarget.transform.position);
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            defenderTarget.TakeDamage(damageToDefender);
            attackTimer = defenderAttackInterval;
        }

        return true;
    }

    private bool TryAttackTower()
    {
        if (tower == null)
        {
            return false;
        }

        float sqrDistance = (tower.transform.position - transform.position).sqrMagnitude;
        float range = towerAttackRange * towerAttackRange;
        if (!reachedTower && sqrDistance > range)
        {
            return false;
        }

        FaceTowards(tower.transform.position);
        towerAttackTimer -= Time.deltaTime;
        if (towerAttackTimer <= 0f)
        {
            tower.TakeDamage(damageToTower);
            towerAttackTimer = towerAttackInterval;
        }

        return true;
    }

    private DefenderHealth FindDefenderInRange()
    {
        return TargetingUtils.FindClosestTarget<DefenderHealth>(transform.position, defenderAttackRange, defenderTargetMask, defenderHits);
    }

    private void FaceTowards(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void UpdateEnrageIfNeeded()
    {
        if (!enrageOnLowHealth || isEnraged || maxHealth <= 0f)
        {
            return;
        }

        if (currentHealth / maxHealth > enrageHealthFraction)
        {
            return;
        }

        isEnraged = true;
        moveSpeed = baseMoveSpeed * enrageSpeedMultiplier;
        damageToTower = baseDamageToTower * enrageDamageMultiplier;
        damageToDefender = baseDamageToDefender * enrageDamageMultiplier;
        towerAttackInterval = baseTowerAttackInterval * enrageAttackIntervalMultiplier;
        defenderAttackInterval = baseDefenderAttackInterval * enrageAttackIntervalMultiplier;
    }

    private void ConfigureHealthBar()
    {
        float barHeight = CalculateHealthBarHeight();
        EnsureHealthBar();
        if (healthBar == null)
        {
            return;
        }

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
        float height = heightOffset + healthBarExtraHeight;
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

    private void Release()
    {
        if (releaseAction != null)
        {
            releaseAction(this);
        }
        else
        {
            Destroy(gameObject);
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
