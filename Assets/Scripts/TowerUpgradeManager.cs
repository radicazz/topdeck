using System.Collections.Generic;
using UnityEngine;

public class TowerUpgradeManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TowerHealth towerHealth;
    [SerializeField] private TowerAttack towerAttack;
    [SerializeField] private Transform visualRoot;

    [Header("Base Visual")]
    [SerializeField] private GameObject basePrefab;
    [SerializeField] private bool forceBaseVisualOnStart = true;

    [Header("Upgrade Prefabs")]
    [SerializeField] private GameObject healthUpgradePrefab;
    [SerializeField] private GameObject miscUpgradePrefab;
    [SerializeField] private GameObject fullUpgradePrefab;
    [SerializeField] private GameObject finalUpgradePrefab;

    [Header("Upgrade Costs")]
    [SerializeField] private int healthUpgradeCost = 80;
    [SerializeField] private int miscUpgradeCost = 110;
    [SerializeField] private int fullUpgradeCost = 160;
    [SerializeField] private int finalUpgradeCost = 220;

    [Header("Upgrade Multipliers")]
    [SerializeField] private float healthHealthMultiplier = 1.5f;
    [SerializeField] private float healthDamageMultiplier = 1.1f;
    [SerializeField] private float miscHealthMultiplier = 1.2f;
    [SerializeField] private float miscDamageMultiplier = 1.4f;
    [SerializeField] private float fullHealthMultiplier = 1.7f;
    [SerializeField] private float fullDamageMultiplier = 1.6f;
    [SerializeField] private float finalHealthMultiplier = 2.0f;
    [SerializeField] private float finalDamageMultiplier = 2.0f;

    [Header("State")]
    [SerializeField] private int upgradeLevel;

    private readonly List<DefenderUpgradeStep> upgradeSteps = new List<DefenderUpgradeStep>();
    private GameObject currentVisual;
    private float baseMaxHealth;
    private float baseRange;
    private float baseAttackInterval;
    private float baseDamage;

    public int UpgradeLevel => upgradeLevel;

    private void Awake()
    {
        if (towerHealth == null)
        {
            towerHealth = GetComponent<TowerHealth>();
        }

        if (towerAttack == null)
        {
            towerAttack = GetComponent<TowerAttack>();
        }

        if (visualRoot == null)
        {
            visualRoot = transform;
        }
    }

    private void Start()
    {
        CacheBaseStats();
        EnsureUpgradeSteps();

        if (forceBaseVisualOnStart && basePrefab != null)
        {
            ReplaceVisual(basePrefab);
        }
        else if (visualRoot != null && visualRoot.childCount > 0)
        {
            currentVisual = visualRoot.GetChild(0).gameObject;
        }

        ApplyStats(baseMaxHealth, baseDamage);
    }

    public bool CanUpgrade()
    {
        return GetNextUpgradeStep() != null;
    }

    public int GetUpgradeCost()
    {
        DefenderUpgradeStep step = GetNextUpgradeStep();
        return step != null ? step.Cost : 0;
    }

    public string GetUpgradeLabel()
    {
        DefenderUpgradeStep step = GetNextUpgradeStep();
        return step != null && !string.IsNullOrEmpty(step.Label) ? step.Label : "Upgrade";
    }

    public bool TryUpgradeTower()
    {
        DefenderUpgradeStep step = GetNextUpgradeStep();
        if (step == null)
        {
            return false;
        }

        if (GameManager.Instance != null && !GameManager.Instance.TrySpend(step.Cost))
        {
            return false;
        }

        ApplyUpgradeStep(step);
        upgradeLevel = Mathf.Clamp(upgradeLevel + 1, 0, upgradeSteps.Count);
        return true;
    }

    private void CacheBaseStats()
    {
        if (towerHealth != null)
        {
            baseMaxHealth = towerHealth.MaxHealth;
        }

        if (towerAttack != null)
        {
            baseRange = towerAttack.Range;
            baseAttackInterval = towerAttack.AttackInterval;
            baseDamage = towerAttack.DamagePerShot;
        }
    }

    private void EnsureUpgradeSteps()
    {
        upgradeSteps.Clear();
        AddUpgradeStep("Upgrade: Health", healthUpgradePrefab, healthUpgradeCost, healthHealthMultiplier, healthDamageMultiplier);
        AddUpgradeStep("Upgrade: Misc", miscUpgradePrefab, miscUpgradeCost, miscHealthMultiplier, miscDamageMultiplier);
        AddUpgradeStep("Upgrade: Full", fullUpgradePrefab, fullUpgradeCost, fullHealthMultiplier, fullDamageMultiplier);
        AddUpgradeStep("Upgrade: Sniper", finalUpgradePrefab, finalUpgradeCost, finalHealthMultiplier, finalDamageMultiplier);
    }

    private void AddUpgradeStep(string label, GameObject prefab, int cost, float healthMultiplier, float damageMultiplier)
    {
        if (prefab == null)
        {
            return;
        }

        upgradeSteps.Add(new DefenderUpgradeStep
        {
            Label = label,
            Prefab = prefab,
            Cost = cost,
            HealthMultiplier = healthMultiplier,
            DamageMultiplier = damageMultiplier
        });
    }

    private DefenderUpgradeStep GetNextUpgradeStep()
    {
        if (upgradeLevel < 0 || upgradeLevel >= upgradeSteps.Count)
        {
            return null;
        }

        return upgradeSteps[upgradeLevel];
    }

    private void ApplyUpgradeStep(DefenderUpgradeStep step)
    {
        if (step == null)
        {
            return;
        }

        ReplaceVisual(step.Prefab);

        float newMaxHealth = Mathf.Max(0.1f, baseMaxHealth * step.HealthMultiplier);
        float newDamage = Mathf.Max(0f, baseDamage * step.DamageMultiplier);
        ApplyStats(newMaxHealth, newDamage);
    }

    private void ApplyStats(float maxHealth, float damage)
    {
        if (towerHealth != null)
        {
            towerHealth.Initialize(maxHealth);
        }

        if (towerAttack != null)
        {
            towerAttack.Configure(baseRange, baseAttackInterval, damage, towerAttack.TargetMask);
        }
    }

    private void ReplaceVisual(GameObject prefab)
    {
        if (prefab == null || visualRoot == null)
        {
            return;
        }

        if (currentVisual != null)
        {
            Destroy(currentVisual);
        }

        GameObject instance = Instantiate(prefab, visualRoot);
        instance.name = "TowerVisual";
        Transform instanceTransform = instance.transform;
        instanceTransform.localPosition = Vector3.zero;
        instanceTransform.localRotation = Quaternion.identity;
        instanceTransform.localScale = Vector3.one;
        currentVisual = instance;
    }
}
