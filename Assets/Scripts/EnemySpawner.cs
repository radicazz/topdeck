using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public ProceduralTerrainGenerator terrain;
    public TowerHealth tower;

    [Header("Spawn")]
    public float spawnInterval = 2f;
    public float enemySpeed = 2f;
    public float enemyMaxHealth = 5f;
    public float damageToTower = 1f;
    public float enemyHeightOffset = 0.5f;

    [Header("Visuals")]
    public Color enemyColor = new Color(1f, 0.3f, 0.3f);

    private readonly List<IReadOnlyList<Vector3>> paths = new List<IReadOnlyList<Vector3>>();
    private float timer;

    private void Start()
    {
        if (terrain == null)
        {
            terrain = FindObjectOfType<ProceduralTerrainGenerator>();
        }

        if (tower == null)
        {
            tower = FindObjectOfType<TowerHealth>();
        }

        if (terrain == null)
        {
            Debug.LogWarning("EnemySpawner: Terrain generator not found.");
            enabled = false;
            return;
        }

        CachePaths();
    }

    private void Update()
    {
        if (GameManager.IsGameOver)
        {
            return;
        }

        timer += Time.deltaTime;
        if (timer < spawnInterval)
        {
            return;
        }

        timer = 0f;
        SpawnWave();
    }

    private void CachePaths()
    {
        paths.Clear();
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
        }
    }

    private void SpawnWave()
    {
        if (paths.Count == 0)
        {
            CachePaths();
        }

        if (paths.Count == 0)
        {
            return;
        }

        foreach (var path in paths)
        {
            Enemy enemy = CreateEnemy();
            enemy.Initialize(path, tower, enemySpeed, enemyMaxHealth, damageToTower, enemyHeightOffset);
        }
    }

    private Enemy CreateEnemy()
    {
        GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemyObject.name = "Enemy";
        enemyObject.transform.localScale = Vector3.one * 0.6f;
        var renderer = enemyObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = enemyColor;
        }

        return enemyObject.AddComponent<Enemy>();
    }
}
