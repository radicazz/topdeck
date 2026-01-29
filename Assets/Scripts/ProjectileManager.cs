using UnityEngine;
using System.Collections.Generic;

public class ProjectileManager : MonoBehaviour {
    public static ProjectileManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int poolWarmSize = 10;

    private Queue<Projectile> projectilePool = new Queue<Projectile>();
    private HashSet<Projectile> activeProjectiles = new HashSet<Projectile>();

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start() {
        WarmPool();
    }

    private void WarmPool() {
        for (int i = 0; i < poolWarmSize; i++) {
            Projectile proj = CreateNewProjectile();
            proj.gameObject.SetActive(false);
            projectilePool.Enqueue(proj);
        }
    }

    private Projectile CreateNewProjectile() {
        GameObject go = Instantiate(projectilePrefab, transform);
        Projectile proj = go.GetComponent<Projectile>();
        if (proj == null) {
            proj = go.AddComponent<Projectile>();
        }
        return proj;
    }

    public Projectile FireProjectile(Vector3 origin, Enemy target, float damage) {
        if (target == null || !target.gameObject.activeSelf) {
            return null;
        }

        Projectile proj = GetProjectileFromPool();
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
