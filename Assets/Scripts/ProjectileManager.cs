using UnityEngine;
using System.Collections.Generic;

public class ProjectileManager : MonoBehaviour {
    public static ProjectileManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Min(0)] private int poolWarmSize = 10;

    private Queue<Projectile> projectilePool = new Queue<Projectile>();
    private HashSet<Projectile> activeProjectiles = new HashSet<Projectile>();
    private bool warnedMissingPrefab;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start() {
        if (projectilePrefab == null) {
            Debug.LogError("ProjectileManager: Missing projectile prefab. Disabling projectile manager.");
            enabled = false;
            return;
        }
        WarmPool();
    }

    private void WarmPool() {
        if (projectilePrefab == null) {
            return;
        }
        for (int i = 0; i < poolWarmSize; i++) {
            Projectile proj = CreateNewProjectile();
            if (proj == null) {
                return;
            }
            proj.gameObject.SetActive(false);
            projectilePool.Enqueue(proj);
        }
    }

    private Projectile CreateNewProjectile() {
        if (projectilePrefab == null) {
            if (!warnedMissingPrefab) {
                Debug.LogError("ProjectileManager: Missing projectile prefab. Cannot create projectiles.");
                warnedMissingPrefab = true;
            }
            return null;
        }
        GameObject go = Instantiate(projectilePrefab, transform);
        Projectile proj = go.GetComponent<Projectile>();
        if (proj == null) {
            proj = go.AddComponent<Projectile>();
        }
        return proj;
    }

    public Projectile FireProjectile(Vector3 origin, Enemy target, float damage) {
        if (!enabled) {
            return null;
        }

        if (target == null || !target.gameObject.activeSelf) {
            return null;
        }

        Projectile proj = GetProjectileFromPool();
        if (proj == null) {
            return null;
        }
        proj.transform.position = origin;
        proj.gameObject.SetActive(true);
        proj.Initialize(target, damage, ReleaseProjectile);
        activeProjectiles.Add(proj);
        return proj;
    }

    private Projectile GetProjectileFromPool() {
        if (projectilePool.Count > 0) {
            return projectilePool.Dequeue();
        }
        return CreateNewProjectile();
    }

    public void ReleaseProjectile(Projectile proj) {
        if (proj == null) return;

        activeProjectiles.Remove(proj);
        proj.gameObject.SetActive(false);
        projectilePool.Enqueue(proj);
    }

    private void OnDestroy() {
        if (Instance == this) {
            Instance = null;
        }
    }
}
