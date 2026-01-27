using System;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float damageToTower = 1f;
    [SerializeField] private float heightOffset = 0.5f;

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
    private IReadOnlyList<Vector3> path;
    private int pathIndex;
    private TowerHealth tower;
    private float attackTimer;
    private DefenderHealth defenderTarget;
    private float towerAttackTimer;
    private bool reachedTower;
    private bool isDead;
    private Collider[] defenderHits;
    private Action<Enemy> releaseAction;

    public event Action<Enemy> Died;

    private void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
        defenderHits = new Collider[Mathf.Max(1, defenderQueryBufferSize)];
    }

    public void Initialize(IReadOnlyList<Vector3> pathPoints, TowerHealth towerRef, float speed, float health, float damage, float offset,
        float defenderRange, float defenderInterval, float defenderDamage, float towerRange, float towerInterval, LayerMask defenderMask)
    {
        path = pathPoints;
        tower = towerRef;
        moveSpeed = speed;
        maxHealth = health;
        currentHealth = health;
        damageToTower = damage;
        heightOffset = offset;
        defenderAttackRange = defenderRange;
        defenderAttackInterval = defenderInterval;
        damageToDefender = defenderDamage;
        towerAttackRange = towerRange;
        towerAttackInterval = towerInterval;
        defenderTargetMask = defenderMask;
        pathIndex = 0;
        attackTimer = 0f;
        towerAttackTimer = 0f;
        defenderTarget = null;
        reachedTower = false;
        isDead = false;

        if (path != null && path.Count > 0)
        {
            transform.position = path[0] + Vector3.up * heightOffset;
        }
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

        if (TryAttackTower())
        {
            return;
        }

        if (TryAttackDefender())
        {
            return;
        }

        Vector3 target = path[pathIndex] + Vector3.up * heightOffset;
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

        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            isDead = true;
            Died?.Invoke(this);
            GameManager.Instance?.OnEnemyKilled();
            Release();
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
