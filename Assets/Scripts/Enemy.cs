using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 5f;
    public float moveSpeed = 2f;
    public float damageToTower = 1f;
    public float heightOffset = 0.5f;

    private float currentHealth;
    private IReadOnlyList<Vector3> path;
    private int pathIndex;
    private TowerHealth tower;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void Initialize(IReadOnlyList<Vector3> pathPoints, TowerHealth towerRef, float speed, float health, float damage, float offset)
    {
        path = pathPoints;
        tower = towerRef;
        moveSpeed = speed;
        maxHealth = health;
        currentHealth = health;
        damageToTower = damage;
        heightOffset = offset;
        pathIndex = 0;

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

        Vector3 target = path[pathIndex] + Vector3.up * heightOffset;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if ((transform.position - target).sqrMagnitude < 0.01f)
        {
            pathIndex++;
            if (pathIndex >= path.Count)
            {
                if (tower != null)
                {
                    tower.TakeDamage(damageToTower);
                }
                Destroy(gameObject);
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
            Destroy(gameObject);
        }
    }
}
