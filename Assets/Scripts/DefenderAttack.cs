using UnityEngine;

public class DefenderAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float range = 4f;
    [SerializeField] private float attackInterval = 0.6f;
    [SerializeField] private float damagePerShot = 1f;
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField, Min(1)] private int queryBufferSize = 24;

    private float cooldown;
    private Collider[] hitBuffer;

    private void Awake()
    {
        hitBuffer = new Collider[Mathf.Max(1, queryBufferSize)];
    }

    public void Configure(float newRange, float interval, float damage, LayerMask mask)
    {
        range = newRange;
        attackInterval = interval;
        damagePerShot = damage;
        targetMask = mask;
        cooldown = 0f;
    }

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
        return TargetingUtils.FindClosestTarget<Enemy>(transform.position, range, targetMask, hitBuffer);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
