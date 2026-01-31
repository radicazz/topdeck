using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool IsGameOver => Instance != null && Instance.isGameOver;

    [Header("References")]
    [SerializeField] private TowerHealth tower;
    [SerializeField] private TowerAttack towerAttack;
    [SerializeField] private EnemySpawner[] spawners;

    [Header("Economy")]
    [SerializeField] private int startingMoney = 200;
    [SerializeField] private int defenderCost = 100;
    [SerializeField] private int rewardPerKill = 50;

    [Header("Rounds")]
    [SerializeField] private int startingRound = 1;
    [SerializeField] private float roundStartDelay = 2f;
    [SerializeField] private int baseEnemiesPerRound = 3;
    [SerializeField] private int enemiesPerRoundIncrement = 2;
    [SerializeField] private float healthMultiplierPerRound = 0.15f;
    [SerializeField] private float speedMultiplierPerRound = 0.05f;
    [SerializeField] private float damageMultiplierPerRound = 0.1f;

    [Header("State")]
    [SerializeField] private bool isGameOver;
    [SerializeField] private int currentRound;
    [SerializeField] private int currentMoney;
    [SerializeField] private bool roundInProgress;

    public int CurrentRound => currentRound;
    public int CurrentMoney => currentMoney;
    public bool RoundInProgress => roundInProgress;

    private int spawnersCompleted;
    private bool roundQueued;
    private readonly List<EnemySpawner> activeSpawners = new List<EnemySpawner>();

    public event System.Action<int> MoneyChanged;
    public event System.Action<int, bool> RoundChanged;
    public event System.Action GameOverTriggered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (tower == null)
        {
            tower = FindFirstObjectByType<TowerHealth>();
        }

        if (towerAttack == null)
        {
            towerAttack = FindFirstObjectByType<TowerAttack>();
        }

        if (spawners == null || spawners.Length == 0)
        {
            spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        }

        currentMoney = startingMoney;
        currentRound = Mathf.Max(0, startingRound - 1);
    }

    private void Start()
    {
        HookSpawnerEvents();
        BeginNextRound();
    }

    private void OnDestroy()
    {
        CancelInvoke();
        if (spawners != null)
        {
            foreach (var spawner in spawners)
            {
                if (spawner != null)
                {
                    spawner.RoundCompleted -= OnSpawnerRoundComplete;
                }
            }
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void GameOver()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        roundInProgress = false;
        roundQueued = false;
        CancelInvoke(nameof(BeginNextRound));
        RoundChanged?.Invoke(currentRound, roundInProgress);

        if (towerAttack != null)
        {
            towerAttack.enabled = false;
        }

        if (spawners != null)
        {
            foreach (var spawner in spawners)
            {
                if (spawner != null)
                {
                    spawner.enabled = false;
                }
            }
        }

        GameOverTriggered?.Invoke();
        Debug.Log("Game Over: Tower destroyed.");
    }

    public bool TryPurchaseDefender()
    {
        return TrySpend(defenderCost);
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (currentMoney < amount)
        {
            return false;
        }

        currentMoney -= amount;
        MoneyChanged?.Invoke(currentMoney);
        return true;
    }

    public void OnEnemyKilled()
    {
        if (isGameOver)
        {
            return;
        }

        currentMoney += rewardPerKill;
        MoneyChanged?.Invoke(currentMoney);
    }

    private void BeginNextRound()
    {
        if (isGameOver)
        {
            return;
        }

        RefreshSpawners();
        HookSpawnerEvents();
        if (activeSpawners.Count == 0)
        {
            return;
        }

        currentRound = Mathf.Max(1, currentRound + 1);
        roundInProgress = true;
        roundQueued = false;
        spawnersCompleted = 0;
        RoundChanged?.Invoke(currentRound, roundInProgress);

        int totalEnemies = baseEnemiesPerRound + Mathf.Max(0, (currentRound - 1) * enemiesPerRoundIncrement);
        float healthMultiplier = 1f + Mathf.Max(0, (currentRound - 1) * healthMultiplierPerRound);
        float speedMultiplier = 1f + Mathf.Max(0, (currentRound - 1) * speedMultiplierPerRound);
        float damageMultiplier = 1f + Mathf.Max(0, (currentRound - 1) * damageMultiplierPerRound);

        int spawnerCount = activeSpawners.Count;
        int perSpawner = spawnerCount > 0 ? totalEnemies / spawnerCount : totalEnemies;
        int remainder = spawnerCount > 0 ? totalEnemies % spawnerCount : 0;

        for (int i = 0; i < spawnerCount; i++)
        {
            int spawnCount = perSpawner + (i < remainder ? 1 : 0);
            activeSpawners[i].StartRound(spawnCount, healthMultiplier, speedMultiplier, damageMultiplier);
        }
    }

    private void OnSpawnerRoundComplete(EnemySpawner spawner)
    {
        if (!this || !isActiveAndEnabled || isGameOver)
        {
            return;
        }

        spawnersCompleted++;
        if (spawnersCompleted < activeSpawners.Count)
        {
            return;
        }

        roundInProgress = false;
        RoundChanged?.Invoke(currentRound, roundInProgress);
        if (!roundQueued)
        {
            roundQueued = true;
            Invoke(nameof(BeginNextRound), roundStartDelay);
        }
    }

    private void HookSpawnerEvents()
    {
        if (spawners == null)
        {
            return;
        }

        foreach (var spawner in spawners)
        {
            if (spawner == null)
            {
                continue;
            }
            spawner.RoundCompleted -= OnSpawnerRoundComplete;
            if (spawner.enabled)
            {
                spawner.RoundCompleted += OnSpawnerRoundComplete;
            }
        }
    }

    private void RefreshSpawners()
    {
        if (spawners == null || spawners.Length == 0)
        {
            spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        }

        activeSpawners.Clear();
        if (spawners == null)
        {
            return;
        }

        foreach (var spawner in spawners)
        {
            if (spawner == null || !spawner.enabled)
            {
                continue;
            }
            activeSpawners.Add(spawner);
        }
    }
}
