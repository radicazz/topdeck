using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool IsGameOver => Instance != null && Instance.isGameOver;

    [Header("References")]
    public TowerHealth tower;
    public TowerAttack towerAttack;
    public EnemySpawner[] spawners;

    [Header("Economy")]
    public int startingMoney = 200;
    public int defenderCost = 100;
    public int rewardPerKill = 50;

    [Header("Rounds")]
    public int startingRound = 1;
    public float roundStartDelay = 2f;
    public int baseEnemiesPerRound = 3;
    public int enemiesPerRoundIncrement = 2;
    public float healthMultiplierPerRound = 0.15f;
    public float speedMultiplierPerRound = 0.05f;
    public float damageMultiplierPerRound = 0.1f;

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

        Debug.Log("Game Over: Tower destroyed.");
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
        return true;
    }

    public void OnEnemyKilled()
    {
        if (isGameOver)
        {
            return;
        }

        currentMoney += rewardPerKill;
    }

    private void BeginNextRound()
    {
        if (isGameOver)
        {
            return;
        }

        if (spawners == null || spawners.Length == 0)
        {
            spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        }

        HookSpawnerEvents();

        if (spawners == null || spawners.Length == 0)
        {
            return;
        }

        currentRound = Mathf.Max(1, currentRound + 1);
        roundInProgress = true;
        roundQueued = false;
        spawnersCompleted = 0;

        int totalEnemies = baseEnemiesPerRound + Mathf.Max(0, (currentRound - 1) * enemiesPerRoundIncrement);
        float healthMultiplier = 1f + Mathf.Max(0, (currentRound - 1) * healthMultiplierPerRound);
        float speedMultiplier = 1f + Mathf.Max(0, (currentRound - 1) * speedMultiplierPerRound);
        float damageMultiplier = 1f + Mathf.Max(0, (currentRound - 1) * damageMultiplierPerRound);

        int spawnerCount = spawners.Length;
        int perSpawner = spawnerCount > 0 ? totalEnemies / spawnerCount : totalEnemies;
        int remainder = spawnerCount > 0 ? totalEnemies % spawnerCount : 0;

        for (int i = 0; i < spawnerCount; i++)
        {
            int spawnCount = perSpawner + (i < remainder ? 1 : 0);
            spawners[i].StartRound(spawnCount, healthMultiplier, speedMultiplier, damageMultiplier);
        }
    }

    private void OnSpawnerRoundComplete(EnemySpawner spawner)
    {
        spawnersCompleted++;
        if (spawnersCompleted < spawners.Length)
        {
            return;
        }

        roundInProgress = false;
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
            spawner.RoundCompleted += OnSpawnerRoundComplete;
        }
    }
}
