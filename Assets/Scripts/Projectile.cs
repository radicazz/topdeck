using UnityEngine;

public class Projectile : MonoBehaviour {
    [Header("Movement")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float arcHeight = 0.5f;

    private Enemy targetEnemy;
    private float damage;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float journeyProgress;
    private System.Action<Projectile> releaseCallback;

    public void Initialize(Enemy target, float dmg, System.Action<Projectile> release) {
        targetEnemy = target;
        damage = dmg;
        releaseCallback = release;
        startPosition = transform.position;
        targetPosition = target != null ? target.transform.position : startPosition;
        journeyProgress = 0f;
    }

    private void Update() {
        if (GameManager.IsGameOver) {
            return;
        }

        // Update target position if enemy is still alive and moving
        if (targetEnemy != null && !targetEnemy.gameObject.activeSelf) {
            targetEnemy = null;
        }
        else if (targetEnemy != null) {
            targetPosition = targetEnemy.transform.position;
        }

        // Calculate movement progress
        float distance = Vector3.Distance(startPosition, targetPosition);
        if (distance > 0.01f) {
            float stepSize = speed * Time.deltaTime / distance;
            journeyProgress += stepSize;
        } else {
            journeyProgress = 1f;
        }

        // Check if arrived
        if (journeyProgress >= 1f) {
            OnArrival();
            return;
        }

        // Ballistic arc movement
        Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, journeyProgress);
        currentPos.y += arcHeight * Mathf.Sin(journeyProgress * Mathf.PI);
        transform.position = currentPos;

        // Rotate to face movement direction
        if (journeyProgress < 1f) {
            Vector3 nextPos = Vector3.Lerp(startPosition, targetPosition, journeyProgress + 0.01f);
            nextPos.y += arcHeight * Mathf.Sin((journeyProgress + 0.01f) * Mathf.PI);
            Vector3 direction = nextPos - currentPos;
            if (direction.sqrMagnitude > 0.001f) {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    private void OnArrival() {
        // Deal damage if target is still alive
        if (targetEnemy != null && targetEnemy.gameObject.activeSelf) {
            targetEnemy.TakeDamage(damage);
        }

        // Spawn impact VFX (will be implemented in commit 2)
        // For now, just return to pool

        // Return to pool
        releaseCallback?.Invoke(this);
    }
}
