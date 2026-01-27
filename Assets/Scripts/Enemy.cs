using System;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 5f;
    public float moveSpeed = 2f;
    public float damageToTower = 1f;
    public float heightOffset = 0.5f;

    [Header("Tower Attack")]
    public float towerAttackRange = 1.4f;
    public float towerAttackInterval = 0.8f;

    [Header("Defender Attack")]
    public float defenderAttackRange = 1.2f;
    public float defenderAttackInterval = 0.6f;
    public float damageToDefender = 1f;

    private float currentHealth;
    private IReadOnlyList<Vector3> path;
    private int pathIndex;
    private TowerHealth tower;
    private float attackTimer;
    private DefenderHealth defenderTarget;
    private float towerAttackTimer;
    private bool reachedTower;
    private bool isDead;

    public event Action<Enemy> Died;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void Initialize(IReadOnlyList<Vector3> pathPoints, TowerHealth towerRef, float speed, float health, float damage, float offset,
        float defenderRange, float defenderInterval, float defenderDamage, float towerRange, float towerInterval)
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
        pathIndex = 0;
        reachedTower = false;

        if (path != null && path.Count > 0)
        {
            transform.position = path[0] + Vector3.up * heightOffset;
        }
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

        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            Died?.Invoke(this);
            GameManager.Instance?.OnEnemyKilled();
            Destroy(gameObject);
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
        Collider[] hits = Physics.OverlapSphere(transform.position, defenderAttackRange);
        DefenderHealth closest = null;
        float bestDistance = float.PositiveInfinity;

        foreach (var hit in hits)
        {
            DefenderHealth defender = hit.GetComponent<DefenderHealth>();
            if (defender == null)
            {
                continue;
            }

            float distance = (defender.transform.position - transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                closest = defender;
            }
        }

        return closest;
    }
}
