using UnityEngine;

public class DefenderAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float range = 4f;
    [SerializeField] private float attackInterval = 0.6f;
    [SerializeField] private float damagePerShot = 1f;
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField, Min(1)] private int queryBufferSize = 24;

    [Header("Tracking")]
    [SerializeField] private bool rotateTowardsTarget = true;
    [SerializeField] private float turnSpeed = 10f;

    [Header("Movement")]
    [SerializeField] private bool allowMovement = true;
    [SerializeField] private float moveRadius = 0.6f;
    [SerializeField] private float moveSpeed = 1.5f;

    [Header("Projectile")]
    [SerializeField] private bool useProjectiles = true;
    [SerializeField] private Vector3 projectileSpawnOffset = Vector3.up * 0.5f;

    private float cooldown;
    private Collider[] hitBuffer;
    private Enemy currentTarget;
    private Vector3 anchorPosition;
    private bool anchorSet;
    private ProceduralTerrainGenerator terrain;

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
        currentTarget = null;
    }

    public void ConfigureMovement(Vector3 anchor, float radius, float speed, float rotationSpeed, ProceduralTerrainGenerator terrainRef)
    {
        anchorPosition = anchor;
        anchorSet = true;
        moveRadius = Mathf.Max(0f, radius);
        moveSpeed = Mathf.Max(0f, speed);
        turnSpeed = Mathf.Max(0f, rotationSpeed);
        terrain = terrainRef;
    }

    private void Update()
    {
        if (GameManager.IsGameOver)
        {
            return;
        }

        UpdateTarget();
        UpdateMovementAndFacing();

        cooldown -= Time.deltaTime;
        if (cooldown > 0f)
        {
            return;
        }

        if (currentTarget != null)
        {
            if (useProjectiles && ProjectileManager.Instance != null)
            {
                Vector3 spawnPos = transform.position + projectileSpawnOffset;
                ProjectileManager.Instance.FireProjectile(spawnPos, currentTarget, damagePerShot);
            }
            else
            {
                currentTarget.TakeDamage(damagePerShot);
            }
            cooldown = attackInterval;
        }
    }

    private void UpdateTarget()
    {
        if (currentTarget != null)
        {
            if (!currentTarget.gameObject.activeInHierarchy)
            {
                currentTarget = null;
            }
            else
            {
                float sqrDistance = (currentTarget.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance > range * range)
                {
                    currentTarget = null;
                }
            }
        }

        if (currentTarget == null)
        {
            currentTarget = FindTarget();
        }
    }

    private void UpdateMovementAndFacing()
    {
        if (!anchorSet)
        {
            anchorPosition = transform.position;
            anchorSet = true;
        }

        Vector3 desiredPosition = anchorPosition;
        if (allowMovement && moveRadius > 0f && currentTarget != null)
        {
            Vector3 toTarget = currentTarget.transform.position - anchorPosition;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                desiredPosition = anchorPosition + toTarget.normalized * moveRadius;
            }
        }

        if (allowMovement && terrain != null && moveRadius > 0f && terrain.IsWorldPositionOnPath(desiredPosition))
        {
            desiredPosition = FindValidPositionAroundAnchor(desiredPosition - anchorPosition);
        }

        if (allowMovement && moveSpeed > 0f)
        {
            desiredPosition.y = anchorPosition.y;
            if ((transform.position - desiredPosition).sqrMagnitude > 0.0001f)
            {
                Vector3 next = Vector3.MoveTowards(transform.position, desiredPosition, moveSpeed * Time.deltaTime);
                next.y = anchorPosition.y;
                if (terrain == null || !terrain.IsWorldPositionOnPath(next))
                {
                    transform.position = next;
                }
            }
        }

        if (!rotateTowardsTarget)
        {
            return;
        }

        Vector3 lookTarget;
        if (currentTarget != null)
        {
            lookTarget = currentTarget.transform.position;
        }
        else if ((transform.position - anchorPosition).sqrMagnitude > 0.0001f)
        {
            lookTarget = anchorPosition;
        }
        else
        {
            return;
        }

        Vector3 direction = lookTarget - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private Enemy FindTarget()
    {
        return TargetingUtils.FindClosestTarget<Enemy>(transform.position, range, targetMask, hitBuffer);
    }

    private Vector3 FindValidPositionAroundAnchor(Vector3 preferredDirection)
    {
        if (terrain == null || moveRadius <= 0f)
        {
            return anchorPosition;
        }

        Vector3 direction = preferredDirection;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.forward;
            direction.y = 0f;
        }

        float baseAngle = Mathf.Atan2(direction.z, direction.x);
        const int samples = 12;
        const float twoPi = Mathf.PI * 2f;

        for (int i = 0; i < samples; i++)
        {
            float angle = baseAngle + twoPi * (i / (float)samples);
            Vector3 candidate = anchorPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * moveRadius;
            if (!terrain.IsWorldPositionOnPath(candidate))
            {
                return candidate;
            }
        }

        return anchorPosition;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
