using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float range = 6f;
    [SerializeField] private float attackInterval = 0.5f;
    [SerializeField] private float damagePerShot = 1f;
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField, Min(1)] private int queryBufferSize = 32;

    [Header("Projectile")]
    [SerializeField] private bool useProjectiles = true;
    [SerializeField] private Vector3 projectileSpawnOffset = Vector3.up * 0.5f;

    private float cooldown;
    private Collider[] hitBuffer;

    private void Awake()
    {
        hitBuffer = new Collider[Mathf.Max(1, queryBufferSize)];
    }

    public float Range => range;
    public float AttackInterval => attackInterval;
    public float DamagePerShot => damagePerShot;
    public LayerMask TargetMask => targetMask;

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
            if (useProjectiles && ProjectileManager.Instance != null)
            {
                Vector3 spawnPos = transform.position + projectileSpawnOffset;
                ProjectileManager.Instance.FireProjectile(spawnPos, target, damagePerShot);
            }
            else
            {
                target.TakeDamage(damagePerShot);
            }
            cooldown = attackInterval;
        }
    }

    private Enemy FindTarget()
    {
        return TargetingUtils.FindClosestTarget<Enemy>(transform.position, range, targetMask, hitBuffer);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
