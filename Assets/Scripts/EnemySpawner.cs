using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProceduralTerrainGenerator terrain;
    [SerializeField] private TowerHealth tower;

    [Header("Spawn")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float enemySpeed = 2f;
    [SerializeField] private float enemyMaxHealth = 5f;
    [SerializeField] private float damageToTower = 1f;
    [SerializeField] private float enemyHeightOffset = 0.5f;

    [Header("Tower Attack")]
    [SerializeField] private float towerAttackRange = 1.4f;
    [SerializeField] private float towerAttackInterval = 0.8f;

    [Header("Spawn Locations")]
    [SerializeField] private bool spawnOnePerInterval = true;
    [SerializeField] private bool showSpawnMarkers = true;
    [SerializeField] private float spawnMarkerScale = 0.5f;
    [SerializeField] private Color spawnMarkerColor = new Color(0.85f, 0.2f, 0.9f);

    [Header("Rounds")]
    [SerializeField] private bool useRounds = true;

    [Header("Defender Attack")]
    [SerializeField] private float defenderAttackRange = 1.2f;
    [SerializeField] private float defenderAttackInterval = 0.6f;
    [SerializeField] private float damageToDefender = 1f;
    [SerializeField] private LayerMask defenderTargetMask = ~0;

    [Header("Visuals")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Color enemyColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private bool applyOverridesToPrefab = true;

    [Header("Pooling")]
    [SerializeField] private bool usePooling = true;
    [SerializeField, Min(0)] private int enemyPoolSize = 0;

    private readonly List<IReadOnlyList<Vector3>> paths = new List<IReadOnlyList<Vector3>>();
    private readonly List<Vector3> spawnLocations = new List<Vector3>();
    private readonly List<GameObject> spawnMarkers = new List<GameObject>();
    private readonly Queue<Enemy> enemyPool = new Queue<Enemy>();
    private Transform enemyContainer;
    private int enemyLayer = -1;
    private float timer;
    private int spawnIndex;
    private int enemiesToSpawn;
    private int enemiesSpawned;
    private int enemiesAlive;
    private bool roundActive;
    private float roundEnemyMaxHealth;
    private float roundEnemySpeed;
    private float roundDamageToTower;
    private float roundDamageToDefender;

    public event Action<EnemySpawner> RoundCompleted;

    private void Awake()
    {
        ResolveReferences();
        Transform existing = transform.Find("Enemies");
        enemyContainer = existing != null ? existing : new GameObject("Enemies").transform;
        enemyContainer.SetParent(transform, false);
        enemyLayer = LayerMask.NameToLayer("Enemy");
        WarmPool();
    }

    private void Start()
    {
        ResolveReferences();

        if (terrain == null)
        {
            Debug.LogWarning("EnemySpawner: Terrain generator not found.");
            enabled = false;
            return;
        }

        CachePaths();
    }

    private void ResolveReferences()
    {
        if (terrain == null)
        {
            terrain = FindFirstObjectByType<ProceduralTerrainGenerator>();
        }

        if (tower == null)
        {
            tower = FindFirstObjectByType<TowerHealth>();
        }
    }

    private void Update()
    {
        if (GameManager.IsGameOver)
        {
            return;
        }

        if (useRounds)
        {
            if (!roundActive)
            {
                return;
            }

            timer += Time.deltaTime;
            if (timer < spawnInterval)
            {
                return;
            }

            timer = 0f;
            SpawnRoundTick();
        }
        else
        {
            timer += Time.deltaTime;
            if (timer < spawnInterval)
            {
                return;
            }

            timer = 0f;
            SpawnContinuousEnemies();
        }
    }

    private void CachePaths()
    {
        paths.Clear();
        spawnLocations.Clear();
        ClearSpawnMarkers();
        if (terrain == null)
        {
            ResolveReferences();
            if (terrain == null)
            {
                return;
            }
        }
        var source = terrain.PathsWorld;
        if (source == null)
        {
            return;
        }

        foreach (var path in source)
        {
            if (path == null || path.Count == 0)
            {
                continue;
            }
            paths.Add(path);
            spawnLocations.Add(path[0]);
        }

        if (showSpawnMarkers)
        {
            for (int i = 0; i < spawnLocations.Count; i++)
            {
                CreateSpawnMarker(spawnLocations[i]);
            }
        }
    }

    public void StartRound(int enemyCount, float healthMultiplier, float speedMultiplier, float damageMultiplier)
    {
        CachePaths();
        enemiesToSpawn = Mathf.Max(0, enemyCount);
        enemiesSpawned = 0;
        enemiesAlive = 0;
        spawnIndex = 0;
        roundEnemyMaxHealth = enemyMaxHealth * healthMultiplier;
        roundEnemySpeed = enemySpeed * speedMultiplier;
        roundDamageToTower = damageToTower * damageMultiplier;
        roundDamageToDefender = damageToDefender * damageMultiplier;
        roundActive = enemiesToSpawn > 0;
        timer = 0f;

        if (!roundActive)
        {
            RoundCompleted?.Invoke(this);
        }
    }

    private void SpawnRoundTick()
    {
        if (paths.Count == 0)
        {
            CachePaths();
        }

        if (paths.Count == 0)
        {
            return;
        }

        int remaining = enemiesToSpawn - enemiesSpawned;
        if (remaining <= 0)
        {
            CheckRoundComplete();
            return;
        }

        int spawnCount = spawnOnePerInterval ? 1 : paths.Count;
        spawnCount = Mathf.Min(spawnCount, remaining);

        for (int i = 0; i < spawnCount; i++)
        {
            spawnIndex = Mathf.Clamp(spawnIndex, 0, paths.Count - 1);
            IReadOnlyList<Vector3> path = paths[spawnIndex];
            SpawnEnemy(path, roundEnemySpeed, roundEnemyMaxHealth, roundDamageToTower, roundDamageToDefender, true);
            spawnIndex = (spawnIndex + 1) % paths.Count;
        }

        CheckRoundComplete();
    }

    private void SpawnContinuousEnemies()
    {
        if (paths.Count == 0)
        {
            CachePaths();
        }

        if (paths.Count == 0)
        {
            return;
        }

        if (spawnOnePerInterval)
        {
            spawnIndex = Mathf.Clamp(spawnIndex, 0, paths.Count - 1);
            IReadOnlyList<Vector3> path = paths[spawnIndex];
            SpawnEnemy(path, enemySpeed, enemyMaxHealth, damageToTower, damageToDefender, false);
            spawnIndex = (spawnIndex + 1) % paths.Count;
        }
        else
        {
            foreach (var path in paths)
            {
                SpawnEnemy(path, enemySpeed, enemyMaxHealth, damageToTower, damageToDefender, false);
            }
        }
    }

    private void SpawnEnemy(IReadOnlyList<Vector3> path, float speed, float health, float towerDamage, float defenderDamage, bool trackRound)
    {
        Enemy enemy = GetEnemy();
        if (usePooling)
        {
            enemy.gameObject.SetActive(false);
        }
        enemy.SetReleaseAction(usePooling ? ReleaseEnemy : null);
        if (trackRound)
        {
            enemy.Died += HandleEnemyDied;
            enemiesAlive += 1;
            enemiesSpawned += 1;
        }

        enemy.Initialize(path, tower, speed, health, towerDamage, enemyHeightOffset,
            defenderAttackRange, defenderAttackInterval, defenderDamage, towerAttackRange, towerAttackInterval, defenderTargetMask);
        if (usePooling)
        {
            enemy.gameObject.SetActive(true);
        }
    }

    private void WarmPool()
    {
        if (!usePooling || enemyPoolSize <= 0)
        {
            return;
        }

        for (int i = 0; i < enemyPoolSize; i++)
        {
            Enemy enemy = CreateEnemyInstance();
            ReleaseEnemy(enemy);
        }
    }

    private Enemy GetEnemy()
    {
        if (usePooling && enemyPool.Count > 0)
        {
            return enemyPool.Dequeue();
        }

        return CreateEnemyInstance();
    }

    private Enemy CreateEnemyInstance()
    {
        GameObject enemyObject = enemyPrefab != null
            ? Instantiate(enemyPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        enemyObject.name = "Enemy";
        enemyObject.transform.SetParent(enemyContainer, false);
        LayerUtils.SetLayerRecursive(enemyObject, enemyLayer);

        if (enemyObject.GetComponentInChildren<Collider>() == null)
        {
            enemyObject.AddComponent<BoxCollider>();
        }

        Enemy enemy = ComponentUtils.GetOrAddComponent<Enemy>(enemyObject);

        if (enemyPrefab == null || applyOverridesToPrefab)
        {
            enemyObject.transform.localScale = Vector3.one * 0.6f;
            var renderer = enemyObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                RendererUtils.SetColor(renderer, enemyColor);
            }
        }

        return enemy;
    }

    private void ReleaseEnemy(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        if (!usePooling)
        {
            Destroy(enemy.gameObject);
            return;
        }

        enemy.Died -= HandleEnemyDied;
        enemy.transform.SetParent(enemyContainer, false);
        enemy.gameObject.SetActive(false);
        enemyPool.Enqueue(enemy);
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        if (enemy != null)
        {
            enemy.Died -= HandleEnemyDied;
        }

        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        CheckRoundComplete();
    }

    private void CheckRoundComplete()
    {
        if (!roundActive)
        {
            return;
        }

        if (enemiesSpawned >= enemiesToSpawn && enemiesAlive <= 0)
        {
            roundActive = false;
            RoundCompleted?.Invoke(this);
        }
    }

    private void CreateSpawnMarker(Vector3 position)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "SpawnMarker";
        marker.transform.SetParent(transform, false);
        marker.transform.position = position + Vector3.up * 0.1f;
        marker.transform.localScale = new Vector3(spawnMarkerScale, 0.05f, spawnMarkerScale);
        var renderer = marker.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            RendererUtils.SetColor(renderer, spawnMarkerColor);
        }
        spawnMarkers.Add(marker);
    }

    private void ClearSpawnMarkers()
    {
        for (int i = 0; i < spawnMarkers.Count; i++)
        {
            if (spawnMarkers[i] != null)
            {
                Destroy(spawnMarkers[i]);
            }
        }
        spawnMarkers.Clear();
    }
}
