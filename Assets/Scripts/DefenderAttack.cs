using UnityEngine;

public class DefenderAttack : MonoBehaviour
{
    [Header("Attack")]
    public float range = 4f;
    public float attackInterval = 0.6f;
    public float damagePerShot = 1f;

    private float cooldown;

    private void Update()
    {
        if (GameManager.IsGameOver)
        {
            return;
        }

        cooldown -= Time.deltaTime;
        if (cooldown > 0f)
        {
            return;
        }

        Enemy target = FindTarget();
        if (target != null)
        {
            target.TakeDamage(damagePerShot);
            cooldown = attackInterval;
        }
    }

    private Enemy FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        Enemy closest = null;
        float bestDistance = float.PositiveInfinity;

        foreach (var hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null)
            {
                continue;
            }

            float distance = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                closest = enemy;
            }
        }

        return closest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
