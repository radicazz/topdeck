using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DefenderPlacementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProceduralTerrainGenerator terrain;
    [SerializeField] private DefenderContextMenuController menuController;

    [Header("Input")]
    [SerializeField] private LayerMask clickRaycastMask = ~0;

    [Header("Placement Spots")]
    [SerializeField, Min(1)] private int placementCount = 12;
    [SerializeField] private float spotScale = 0.4f;
    [SerializeField] private float spotHeightOffset = 0.1f;
    [SerializeField] private Color spotAvailableColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color spotOccupiedColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField, Range(0.2f, 1f)] private float placementRadiusFactor = 0.55f;

    [Header("Defender Types")]
    [SerializeField] private List<DefenderDefinition> defenderTypes = new List<DefenderDefinition>();
    [SerializeField, Min(0)] private int defaultDefenderIndex;

    [Header("Basic Defender (Fallback)")]
    [SerializeField] private int defenderCost = 100;
    [SerializeField] private GameObject defenderPrefab;
    [SerializeField] private float defenderMaxHealth = 6f;
    [SerializeField] private float defenderRange = 4f;
    [SerializeField] private float defenderAttackInterval = 0.6f;
    [SerializeField] private float defenderDamage = 1f;
    [SerializeField] private float defenderHeightOffset = 0.5f;
    [SerializeField] private Vector3 defenderScale = new Vector3(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color defenderColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private bool applyOverridesToPrefab = true;
    [SerializeField] private LayerMask enemyTargetMask = ~0;

    [Header("Basic Defender Movement (Fallback)")]
    [SerializeField] private float defenderMoveRadius = 0.8f;
    [SerializeField] private float defenderMoveSpeed = 1.5f;
    [SerializeField] private float defenderTurnSpeed = 10f;

    [Header("Advanced Defender (Fallback)")]
    [SerializeField] private string advancedDefenderName = "Advanced Ally";
    [SerializeField] private int advancedDefenderCost = 160;
    [SerializeField] private float advancedHealthMultiplier = 1.4f;
    [SerializeField] private float advancedRangeMultiplier = 1.15f;
    [SerializeField] private float advancedAttackIntervalMultiplier = 0.8f;
    [SerializeField] private float advancedDamageMultiplier = 1.6f;
    [SerializeField] private float advancedMoveRadiusMultiplier = 1.1f;
    [SerializeField] private float advancedMoveSpeedMultiplier = 1.15f;
    [SerializeField] private float advancedTurnSpeedMultiplier = 1.1f;
    [SerializeField] private Vector3 advancedScale = new Vector3(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color advancedColor = new Color(0.95f, 0.85f, 0.25f);
    [SerializeField] private GameObject advancedDefenderPrefab;
    [SerializeField] private bool applyOverridesToAdvancedPrefab = true;

    [Header("Basic Upgrade Prefabs (Fallback)")]
    [SerializeField] private GameObject basicMiscUpgradePrefab;
    [SerializeField] private GameObject basicHealthUpgradePrefab;
    [SerializeField] private GameObject basicFullUpgradePrefab;

    [Header("Basic Upgrade Stats (Fallback)")]
    [SerializeField] private int basicMiscUpgradeCost = 60;
    [SerializeField] private int basicHealthUpgradeCost = 90;
    [SerializeField] private int basicFullUpgradeCost = 140;
    [SerializeField] private float basicMiscHealthMultiplier = 1.2f;
    [SerializeField] private float basicMiscDamageMultiplier = 1.2f;
    [SerializeField] private float basicHealthHealthMultiplier = 1.4f;
    [SerializeField] private float basicHealthDamageMultiplier = 1.35f;
    [SerializeField] private float basicFullHealthMultiplier = 1.7f;
    [SerializeField] private float basicFullDamageMultiplier = 1.6f;

    [Header("Advanced Upgrade Prefabs (Fallback)")]
    [SerializeField] private GameObject advancedMiscUpgradePrefab;
    [SerializeField] private GameObject advancedHealthUpgradePrefab;
    [SerializeField] private GameObject advancedFullUpgradePrefab;

    [Header("Advanced Upgrade Stats (Fallback)")]
    [SerializeField] private int advancedMiscUpgradeCost = 100;
    [SerializeField] private int advancedHealthUpgradeCost = 150;
    [SerializeField] private int advancedFullUpgradeCost = 220;
    [SerializeField] private float advancedMiscHealthMultiplier = 1.25f;
    [SerializeField] private float advancedMiscDamageMultiplier = 1.3f;
    [SerializeField] private float advancedHealthHealthMultiplier = 1.55f;
    [SerializeField] private float advancedHealthDamageMultiplier = 1.5f;
    [SerializeField] private float advancedFullHealthMultiplier = 1.85f;
    [SerializeField] private float advancedFullDamageMultiplier = 1.8f;

    [Header("Pooling")]
    [SerializeField] private bool usePooling = true;
    [SerializeField, Min(0)] private int defenderPoolSize = 0;

    [Header("VFX")]
    [SerializeField] private GameObject upgradeVfxPrefab;
    [SerializeField, Min(0.1f)] private float upgradeVfxLifetime = 2.5f;

    private readonly List<DefenderPlacementSpot> spots = new List<DefenderPlacementSpot>();
    private readonly Dictionary<DefenderPoolKey, Queue<DefenderHealth>> defenderPools = new Dictionary<DefenderPoolKey, Queue<DefenderHealth>>();
    private readonly Dictionary<DefenderHealth, DefenderPoolKey> defenderPoolKeys = new Dictionary<DefenderHealth, DefenderPoolKey>();
    private readonly Dictionary<DefenderHealth, DefenderInstanceData> defenderInstances = new Dictionary<DefenderHealth, DefenderInstanceData>();
    private readonly Dictionary<DefenderHealth, DefenderPlacementSpot> defenderSpots = new Dictionary<DefenderHealth, DefenderPlacementSpot>();
    private Camera mainCamera;
    private Transform defenderContainer;
    private int defenderLayer = -1;
    private int placementLayer = -1;
    private DefenderDefinition selectedDefender;

    public IReadOnlyList<DefenderDefinition> DefenderTypes
    {
        get
        {
            EnsureDefenderTypes();
            return defenderTypes;
        }
    }
    public DefenderDefinition SelectedDefender => selectedDefender;
    public event Action<DefenderDefinition> DefenderSelectionChanged;

    private class DefenderInstanceData
    {
        public DefenderDefinition Definition;
        public int UpgradeLevel;
        public int InvestedCost;
    }

    private readonly struct DefenderPoolKey : IEquatable<DefenderPoolKey>
    {
        public readonly DefenderDefinition Definition;
        public readonly GameObject Prefab;

        public DefenderPoolKey(DefenderDefinition definition, GameObject prefab)
        {
            Definition = definition;
            Prefab = prefab;
        }

        public bool Equals(DefenderPoolKey other)
        {
            return Definition == other.Definition && Prefab == other.Prefab;
        }

        public override bool Equals(object obj)
        {
            return obj is DefenderPoolKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Definition != null ? Definition.GetHashCode() : 0;
                hash = (hash * 397) ^ (Prefab != null ? Prefab.GetHashCode() : 0);
                return hash;
            }
        }
    }

    private void Awake()
    {
        mainCamera = Camera.main;
        Transform existing = transform.Find("Defenders");
        defenderContainer = existing != null ? existing : new GameObject("Defenders").transform;
        defenderContainer.SetParent(transform, false);
        defenderLayer = LayerMask.NameToLayer("Defender");
        placementLayer = LayerMask.NameToLayer("Placement");
        if (menuController == null)
        {
            menuController = FindFirstObjectByType<DefenderContextMenuController>();
        }
        EnsureDefenderTypes();
        SelectDefender(GetDefaultDefender());
    }

    private void Start()
    {
        if (terrain == null)
        {
            terrain = FindFirstObjectByType<ProceduralTerrainGenerator>();
        }

        if (menuController == null)
        {
            menuController = FindFirstObjectByType<DefenderContextMenuController>();
        }

        if (terrain == null)
        {
            Debug.LogWarning("DefenderPlacementManager: Terrain generator not found.");
            enabled = false;
            return;
        }

        BuildPlacementSpots();
        EnsureDefenderTypes();
        WarmPool();
    }

    private void Update()
    {
        if (GameManager.IsGameOver)
        {
            return;
        }

        if (!GameManager.IsGameStarted)
        {
            return;
        }

        if (!IsPrimaryClickDown())
        {
            return;
        }

        if (!TryGetPointerPosition(out Vector2 screenPosition))
        {
            return;
        }

        if (menuController != null && menuController.IsMenuVisible)
        {
            return;
        }

        Camera cameraToUse = mainCamera != null ? mainCamera : Camera.main;
        if (cameraToUse == null)
        {
            return;
        }

        Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, clickRaycastMask);
        if (hits != null && hits.Length > 0)
        {
            DefenderHealth defender = null;
            float defenderDistance = float.MaxValue;
            TowerUpgradeManager tower = null;
            float towerDistance = float.MaxValue;
            DefenderPlacementSpot spot = null;
            float spotDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                DefenderHealth hitDefender = hit.collider.GetComponentInParent<DefenderHealth>();
                if (hitDefender != null && hit.distance < defenderDistance)
                {
                    defender = hitDefender;
                    defenderDistance = hit.distance;
                }

                TowerUpgradeManager hitTower = hit.collider.GetComponentInParent<TowerUpgradeManager>();
                if (hitTower != null && hit.distance < towerDistance)
                {
                    tower = hitTower;
                    towerDistance = hit.distance;
                }

                DefenderPlacementSpot hitSpot = hit.collider.GetComponent<DefenderPlacementSpot>();
                if (hitSpot != null && hit.distance < spotDistance)
                {
                    spot = hitSpot;
                    spotDistance = hit.distance;
                }
            }

            if (defender != null)
            {
                menuController?.ShowUpgradeMenu(defender, screenPosition);
                return;
            }

            if (tower != null)
            {
                menuController?.ShowTowerUpgradeMenu(tower, screenPosition);
                return;
            }

            if (spot != null)
            {
                ShowPlacementMenu(spot, screenPosition);
                return;
            }
        }

        menuController?.HideMenu();
    }

    private void EnsureDefenderTypes()
    {
        if (defenderTypes == null)
        {
            defenderTypes = new List<DefenderDefinition>();
        }

        defenderTypes.RemoveAll(definition => definition == null);

        if (defenderTypes.Count == 0)
        {
            DefenderDefinition basic = CreateBasicFallback();
            DefenderDefinition advanced = CreateAdvancedFallback(basic);
            defenderTypes.Add(basic);
            defenderTypes.Add(advanced);
        }
        else
        {
            DefenderDefinition basic = FindDefinition("basic") ?? defenderTypes[0];
            if (basic != null && basic.Prefab == null && defenderPrefab != null)
            {
                basic.Prefab = defenderPrefab;
            }
            if (basic != null && (basic.UpgradeSteps == null || basic.UpgradeSteps.Count == 0))
            {
                basic.SetUpgradeSteps(BuildUpgradeSteps(
                    basicMiscUpgradePrefab,
                    basicHealthUpgradePrefab,
                    basicFullUpgradePrefab,
                    basicMiscUpgradeCost,
                    basicHealthUpgradeCost,
                    basicFullUpgradeCost,
                    basicMiscHealthMultiplier,
                    basicMiscDamageMultiplier,
                    basicHealthHealthMultiplier,
                    basicHealthDamageMultiplier,
                    basicFullHealthMultiplier,
                    basicFullDamageMultiplier));
            }

            DefenderDefinition advanced = FindDefinition("advanced");
            if (advanced == null && defenderTypes.Count > 1)
            {
                advanced = defenderTypes[1];
            }

            if (advanced != null)
            {
                if (advanced.Prefab == null || (advancedDefenderPrefab != null && advanced.Prefab == defenderPrefab))
                {
                    if (advancedDefenderPrefab != null)
                    {
                        advanced.Prefab = advancedDefenderPrefab;
                    }
                }

                EnsureAdvancedDiffers(basic, advanced);
                if (advanced.UpgradeSteps == null || advanced.UpgradeSteps.Count == 0)
                {
                    advanced.SetUpgradeSteps(BuildUpgradeSteps(
                        advancedMiscUpgradePrefab,
                        advancedHealthUpgradePrefab,
                        advancedFullUpgradePrefab,
                        advancedMiscUpgradeCost,
                        advancedHealthUpgradeCost,
                        advancedFullUpgradeCost,
                        advancedMiscHealthMultiplier,
                        advancedMiscDamageMultiplier,
                        advancedHealthHealthMultiplier,
                        advancedHealthDamageMultiplier,
                        advancedFullHealthMultiplier,
                        advancedFullDamageMultiplier));
                }
            }
        }

        defaultDefenderIndex = Mathf.Clamp(defaultDefenderIndex, 0, defenderTypes.Count - 1);

        if (selectedDefender == null && defenderTypes.Count > 0)
        {
            SelectDefender(GetDefaultDefender());
        }
    }

    private DefenderDefinition FindDefinition(string id)
    {
        if (defenderTypes == null || string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (int i = 0; i < defenderTypes.Count; i++)
        {
            DefenderDefinition definition = defenderTypes[i];
            if (definition == null)
            {
                continue;
            }

            if (string.Equals(definition.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return definition;
            }
        }

        return null;
    }

    private void EnsureAdvancedDiffers(DefenderDefinition basic, DefenderDefinition advanced)
    {
        if (basic == null || advanced == null)
        {
            return;
        }

        bool sameStats =
            Mathf.Approximately(advanced.MaxHealth, basic.MaxHealth) &&
            Mathf.Approximately(advanced.Range, basic.Range) &&
            Mathf.Approximately(advanced.AttackInterval, basic.AttackInterval) &&
            Mathf.Approximately(advanced.Damage, basic.Damage);

        if (!sameStats)
        {
            return;
        }

        advanced.MaxHealth = Mathf.Max(0.1f, basic.MaxHealth * advancedHealthMultiplier);
        advanced.Range = Mathf.Max(0.1f, basic.Range * advancedRangeMultiplier);
        advanced.AttackInterval = Mathf.Max(0.05f, basic.AttackInterval * advancedAttackIntervalMultiplier);
        advanced.Damage = Mathf.Max(0f, basic.Damage * advancedDamageMultiplier);
        advanced.MoveRadius = Mathf.Max(0f, basic.MoveRadius * advancedMoveRadiusMultiplier);
        advanced.MoveSpeed = Mathf.Max(0f, basic.MoveSpeed * advancedMoveSpeedMultiplier);
        advanced.TurnSpeed = Mathf.Max(0f, basic.TurnSpeed * advancedTurnSpeedMultiplier);
    }

    private DefenderDefinition GetDefaultDefender()
    {
        if (defenderTypes == null || defenderTypes.Count == 0)
        {
            return null;
        }

        int index = Mathf.Clamp(defaultDefenderIndex, 0, defenderTypes.Count - 1);
        return defenderTypes[index];
    }

    public bool SelectDefender(DefenderDefinition definition)
    {
        if (definition == null || defenderTypes == null || defenderTypes.Count == 0)
        {
            return false;
        }

        if (!defenderTypes.Contains(definition))
        {
            return false;
        }

        if (selectedDefender == definition)
        {
            return true;
        }

        selectedDefender = definition;
        DefenderSelectionChanged?.Invoke(selectedDefender);
        return true;
    }

    public void HandleSpotClicked(DefenderPlacementSpot spot)
    {
        if (spot == null)
        {
            return;
        }

        Camera cameraToUse = mainCamera != null ? mainCamera : Camera.main;
        if (cameraToUse == null)
        {
            return;
        }

        Vector3 screenPosition = cameraToUse.WorldToScreenPoint(spot.transform.position);
        ShowPlacementMenu(spot, screenPosition);
    }

    public void ShowPlacementMenu(DefenderPlacementSpot spot, Vector2 screenPosition)
    {
        if (spot == null || menuController == null)
        {
            return;
        }

        if (spot.HasDefender)
        {
            DefenderHealth defender = spot.CurrentDefender;
            if (defender != null)
            {
                menuController.ShowUpgradeMenu(defender, screenPosition);
            }
            return;
        }

        menuController.ShowPlacementMenu(spot, screenPosition);
    }

    public bool TryPlaceDefender(DefenderPlacementSpot spot, DefenderDefinition definition)
    {
        if (spot == null || definition == null)
        {
            Debug.LogWarning("TryPlaceDefender failed: missing spot or definition.");
            return false;
        }

        if (spot.HasDefender)
        {
            Debug.LogWarning("TryPlaceDefender failed: spot already occupied.");
            return false;
        }

        if (GameManager.Instance != null && !GameManager.Instance.TryPurchaseDefender(definition.Cost))
        {
            Debug.LogWarning("TryPlaceDefender failed: insufficient funds.");
            return false;
        }

        DefenderHealth health = SpawnDefender(spot.transform.position, definition);
        if (health == null)
        {
            Debug.LogWarning("TryPlaceDefender failed: SpawnDefender returned null.");
            return false;
        }

        spot.SetDefender(health);
        RegisterDefender(health, definition, spot, 0, definition.Cost);
        return true;
    }

    public bool TryUpgradeDefender(DefenderHealth defender)
    {
        if (!TryGetInstanceData(defender, out DefenderInstanceData data))
        {
            return false;
        }

        DefenderUpgradeStep step = GetNextUpgradeStep(data);
        if (step == null)
        {
            return false;
        }

        if (GameManager.Instance != null && !GameManager.Instance.TryPurchaseDefender(step.Cost))
        {
            return false;
        }

        DefenderPlacementSpot spot = GetSpotForDefender(defender);
        if (spot == null)
        {
            return false;
        }

        DefenderHealth upgraded = CreateUpgradedDefender(data.Definition, step, spot.transform.position);
        if (upgraded == null)
        {
            return false;
        }

        ReplaceDefender(defender, upgraded, spot);
        SpawnUpgradeVfx(upgraded.transform.position);

        int newLevel = data.UpgradeLevel + 1;
        RegisterDefender(upgraded, data.Definition, spot, newLevel, data.InvestedCost + step.Cost);
        return true;
    }

    public int GetUpgradeCost(DefenderHealth defender)
    {
        if (!TryGetInstanceData(defender, out DefenderInstanceData data))
        {
            return 0;
        }

        DefenderUpgradeStep step = GetNextUpgradeStep(data);
        return step != null ? step.Cost : 0;
    }

    public string GetUpgradeLabel(DefenderHealth defender)
    {
        if (!TryGetInstanceData(defender, out DefenderInstanceData data))
        {
            return "Upgrade";
        }

        DefenderUpgradeStep step = GetNextUpgradeStep(data);
        return step != null && !string.IsNullOrEmpty(step.Label) ? step.Label : "Upgrade";
    }

    public bool CanUpgrade(DefenderHealth defender)
    {
        if (!TryGetInstanceData(defender, out DefenderInstanceData data))
        {
            return false;
        }

        return GetNextUpgradeStep(data) != null;
    }

    public int GetSellRefund(DefenderHealth defender)
    {
        if (!TryGetInstanceData(defender, out DefenderInstanceData data))
        {
            return 0;
        }

        return Mathf.RoundToInt(data.InvestedCost * 0.5f);
    }

    public bool TrySellDefender(DefenderHealth defender)
    {
        if (!TryGetInstanceData(defender, out DefenderInstanceData data))
        {
            return false;
        }

        int refund = Mathf.RoundToInt(data.InvestedCost * 0.5f);
        if (refund > 0)
        {
            GameManager.Instance?.AddMoney(refund);
        }

        DefenderPlacementSpot spot = GetSpotForDefender(defender);
        if (spot != null)
        {
            spot.SetDefender(null);
        }

        ReleaseAndCleanupDefender(defender);
        return true;
    }

    private void RegisterDefender(DefenderHealth defender, DefenderDefinition definition, DefenderPlacementSpot spot, int upgradeLevel, int investedCost)
    {
        if (defender == null || definition == null || spot == null)
        {
            return;
        }

        defender.Died -= HandleDefenderDied;
        defender.Died += HandleDefenderDied;

        defenderInstances[defender] = new DefenderInstanceData
        {
            Definition = definition,
            UpgradeLevel = Mathf.Max(0, upgradeLevel),
            InvestedCost = Mathf.Max(0, investedCost)
        };
        defenderSpots[defender] = spot;
    }

    private bool TryGetInstanceData(DefenderHealth defender, out DefenderInstanceData data)
    {
        if (defender == null)
        {
            data = null;
            return false;
        }

        return defenderInstances.TryGetValue(defender, out data);
    }

    private DefenderUpgradeStep GetNextUpgradeStep(DefenderInstanceData data)
    {
        if (data == null || data.Definition == null)
        {
            return null;
        }

        IReadOnlyList<DefenderUpgradeStep> steps = data.Definition.UpgradeSteps;
        if (steps == null)
        {
            return null;
        }

        if (data.UpgradeLevel < 0 || data.UpgradeLevel >= steps.Count)
        {
            return null;
        }

        return steps[data.UpgradeLevel];
    }

    private DefenderHealth CreateUpgradedDefender(DefenderDefinition definition, DefenderUpgradeStep step, Vector3 spotPosition)
    {
        if (definition == null || step == null)
        {
            return null;
        }

        DefenderDefinition upgradedDefinition = definition;
        GameObject prefabOverride = step.Prefab;

        DefenderHealth health = CreateDefenderInstance(upgradedDefinition, prefabOverride);
        if (health == null)
        {
            return null;
        }

        Vector3 spawnPosition = spotPosition + Vector3.up * upgradedDefinition.HeightOffset;
        GameObject defenderObject = health.gameObject;
        defenderObject.transform.SetParent(defenderContainer, false);
        defenderObject.transform.position = spawnPosition;

        if (prefabOverride == null || upgradedDefinition.ApplyOverridesToPrefab)
        {
            upgradedDefinition.ApplyVisualOverrides(defenderObject);
        }

        health.SetReleaseAction(usePooling ? ReleaseDefender : null);

        float upgradedHealth = Mathf.Max(0.1f, upgradedDefinition.MaxHealth * step.HealthMultiplier);
        float upgradedDamage = Mathf.Max(0f, upgradedDefinition.Damage * step.DamageMultiplier);

        health.Initialize(upgradedHealth);

        DefenderAttack attack = ComponentUtils.GetOrAddComponent<DefenderAttack>(defenderObject);
        attack.Configure(upgradedDefinition.Range, upgradedDefinition.AttackInterval, upgradedDamage, enemyTargetMask);
        attack.ConfigureMovement(spawnPosition, upgradedDefinition.MoveRadius, upgradedDefinition.MoveSpeed, upgradedDefinition.TurnSpeed, terrain);

        if (usePooling)
        {
            defenderObject.SetActive(true);
        }

        return health;
    }

    private void ReplaceDefender(DefenderHealth current, DefenderHealth replacement, DefenderPlacementSpot spot)
    {
        if (spot == null || replacement == null)
        {
            return;
        }

        spot.SetDefender(replacement);
        ReleaseAndCleanupDefender(current);
    }

    private DefenderPlacementSpot GetSpotForDefender(DefenderHealth defender)
    {
        if (defender == null)
        {
            return null;
        }

        defenderSpots.TryGetValue(defender, out DefenderPlacementSpot spot);
        return spot;
    }

    private void ReleaseAndCleanupDefender(DefenderHealth defender)
    {
        if (defender == null)
        {
            return;
        }

        defender.Died -= HandleDefenderDied;
        defenderInstances.Remove(defender);
        defenderSpots.Remove(defender);
        ReleaseDefender(defender);
        menuController?.NotifyDefenderRemoved(defender);
    }

    private void HandleDefenderDied(DefenderHealth defender)
    {
        if (defender == null)
        {
            return;
        }

        defenderInstances.Remove(defender);
        defenderSpots.Remove(defender);
        menuController?.NotifyDefenderRemoved(defender);
    }

    private void SpawnUpgradeVfx(Vector3 position)
    {
        if (upgradeVfxPrefab == null)
        {
            return;
        }

        GameObject instance = Instantiate(upgradeVfxPrefab, position, Quaternion.identity);
        if (instance == null)
        {
            return;
        }

        ParticleSystem particle = instance.GetComponentInChildren<ParticleSystem>();
        if (particle == null)
        {
            Destroy(instance, upgradeVfxLifetime);
            return;
        }

        float lifetime = upgradeVfxLifetime;
        ParticleSystem.MainModule main = particle.main;
        if (!main.loop)
        {
            lifetime = main.duration + main.startLifetime.constantMax;
        }

        Destroy(instance, Mathf.Max(0.1f, lifetime));
    }

    private DefenderDefinition CreateBasicFallback()
    {
        DefenderDefinition basic = new DefenderDefinition
        {
            Id = "basic",
            DisplayName = "Basic Ally",
            Cost = Mathf.Max(0, defenderCost),
            Prefab = defenderPrefab,
            ApplyOverridesToPrefab = applyOverridesToPrefab,
            Scale = defenderScale,
            Color = defenderColor,
            HeightOffset = defenderHeightOffset,
            MaxHealth = defenderMaxHealth,
            Range = defenderRange,
            AttackInterval = defenderAttackInterval,
            Damage = defenderDamage,
            MoveRadius = defenderMoveRadius,
            MoveSpeed = defenderMoveSpeed,
            TurnSpeed = defenderTurnSpeed
        };

        if (basic.UpgradeSteps == null || basic.UpgradeSteps.Count == 0)
        {
            basic.SetUpgradeSteps(BuildUpgradeSteps(
                basicMiscUpgradePrefab,
                basicHealthUpgradePrefab,
                basicFullUpgradePrefab,
                basicMiscUpgradeCost,
                basicHealthUpgradeCost,
                basicFullUpgradeCost,
                basicMiscHealthMultiplier,
                basicMiscDamageMultiplier,
                basicHealthHealthMultiplier,
                basicHealthDamageMultiplier,
                basicFullHealthMultiplier,
                basicFullDamageMultiplier));
        }

        return basic;
    }

    private DefenderDefinition CreateAdvancedFallback(DefenderDefinition basic)
    {
        if (basic == null)
        {
            basic = CreateBasicFallback();
        }

        DefenderDefinition advanced = basic.CloneWithOverrides(
            "advanced",
            advancedDefenderName,
            Mathf.Max(0, advancedDefenderCost),
            advancedHealthMultiplier,
            advancedRangeMultiplier,
            advancedAttackIntervalMultiplier,
            advancedDamageMultiplier,
            advancedMoveRadiusMultiplier,
            advancedMoveSpeedMultiplier,
            advancedTurnSpeedMultiplier,
            advancedScale,
            advancedColor,
            advancedDefenderPrefab,
            applyOverridesToAdvancedPrefab);

        if (advanced != null && (advanced.UpgradeSteps == null || advanced.UpgradeSteps.Count == 0))
        {
            advanced.SetUpgradeSteps(BuildUpgradeSteps(
                advancedMiscUpgradePrefab,
                advancedHealthUpgradePrefab,
                advancedFullUpgradePrefab,
                advancedMiscUpgradeCost,
                advancedHealthUpgradeCost,
                advancedFullUpgradeCost,
                advancedMiscHealthMultiplier,
                advancedMiscDamageMultiplier,
                advancedHealthHealthMultiplier,
                advancedHealthDamageMultiplier,
                advancedFullHealthMultiplier,
                advancedFullDamageMultiplier));
        }

        return advanced;
    }

    private List<DefenderUpgradeStep> BuildUpgradeSteps(
        GameObject miscPrefab,
        GameObject healthPrefab,
        GameObject fullPrefab,
        int miscCost,
        int healthCost,
        int fullCost,
        float miscHealthMultiplier,
        float miscDamageMultiplier,
        float healthHealthMultiplier,
        float healthDamageMultiplier,
        float fullHealthMultiplier,
        float fullDamageMultiplier)
    {
        var steps = new List<DefenderUpgradeStep>();

        if (miscPrefab != null)
        {
            steps.Add(new DefenderUpgradeStep
            {
                Label = "Upgrade: Misc",
                Prefab = miscPrefab,
                Cost = miscCost,
                HealthMultiplier = miscHealthMultiplier,
                DamageMultiplier = miscDamageMultiplier
            });
        }

        if (healthPrefab != null)
        {
            steps.Add(new DefenderUpgradeStep
            {
                Label = "Upgrade: Health",
                Prefab = healthPrefab,
                Cost = healthCost,
                HealthMultiplier = healthHealthMultiplier,
                DamageMultiplier = healthDamageMultiplier
            });
        }

        if (fullPrefab != null)
        {
            steps.Add(new DefenderUpgradeStep
            {
                Label = "Upgrade: Full",
                Prefab = fullPrefab,
                Cost = fullCost,
                HealthMultiplier = fullHealthMultiplier,
                DamageMultiplier = fullDamageMultiplier
            });
        }

        return steps;
    }

    private void BuildPlacementSpots()
    {
        ClearSpots();

        List<Vector2Int> candidates = new List<Vector2Int>();
        List<Vector2Int> preferred = new List<Vector2Int>();
        List<Vector2Int> fallback = new List<Vector2Int>();
        int width = terrain.Width;
        int height = terrain.Height;
        Vector2Int center = new Vector2Int(width / 2, height / 2);
        float halfMin = Mathf.Min(width, height) * 0.5f;
        float maxRadius = Mathf.Max(1f, halfMin * Mathf.Clamp(placementRadiusFactor, 0.2f, 1f));
        float maxRadiusSqr = maxRadius * maxRadius;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == center.x && y == center.y)
                {
                    continue;
                }

                if (terrain.IsPathCell(x, y))
                {
                    continue;
                }

                Vector2Int cell = new Vector2Int(x, y);
                candidates.Add(cell);

                Vector2Int delta = cell - center;
                float sqrDistance = delta.x * delta.x + delta.y * delta.y;
                if (sqrDistance <= maxRadiusSqr)
                {
                    preferred.Add(cell);
                }
                else
                {
                    fallback.Add(cell);
                }
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("DefenderPlacementManager: No valid placement spots found.");
            return;
        }

        var random = new System.Random(terrain.LastSeedUsed + 1337);
        for (int i = preferred.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            Vector2Int temp = preferred[i];
            preferred[i] = preferred[j];
            preferred[j] = temp;
        }

        if (preferred.Count < placementCount)
        {
            fallback.Sort((a, b) =>
            {
                int dxA = a.x - center.x;
                int dyA = a.y - center.y;
                int dxB = b.x - center.x;
                int dyB = b.y - center.y;
                float distA = dxA * dxA + dyA * dyA;
                float distB = dxB * dxB + dyB * dyB;
                return distA.CompareTo(distB);
            });

            int needed = Mathf.Min(placementCount - preferred.Count, fallback.Count);
            for (int i = 0; i < needed; i++)
            {
                preferred.Add(fallback[i]);
            }
        }

        int count = Mathf.Min(placementCount, preferred.Count);
        for (int i = 0; i < count; i++)
        {
            Vector2Int cell = preferred[i];
            if (!terrain.TryGetCellWorldPosition(cell.x, cell.y, out Vector3 worldPosition))
            {
                continue;
            }

            CreateSpot(worldPosition);
        }
    }

    private void ClearSpots()
    {
        foreach (var spot in spots)
        {
            if (spot != null)
            {
                Destroy(spot.gameObject);
            }
        }

        spots.Clear();
    }

    private void CreateSpot(Vector3 worldPosition)
    {
        GameObject spotObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spotObject.name = "PlacementSpot";
        spotObject.transform.SetParent(transform, false);
        spotObject.transform.position = worldPosition + Vector3.up * spotHeightOffset;
        spotObject.transform.localScale = new Vector3(spotScale, spotScale * 0.2f, spotScale);
        LayerUtils.SetLayerRecursive(spotObject, placementLayer);

        var renderer = spotObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            RendererUtils.SetColor(renderer, spotAvailableColor);
        }

        var spot = spotObject.AddComponent<DefenderPlacementSpot>();
        spot.Initialize(this, spotAvailableColor, spotOccupiedColor);
        spots.Add(spot);
    }

    private void WarmPool()
    {
        if (!usePooling || defenderPoolSize <= 0)
        {
            return;
        }

        EnsureDefenderTypes();
        if (defenderTypes == null || defenderTypes.Count == 0)
        {
            return;
        }

        int perType = Mathf.Max(0, defenderPoolSize / defenderTypes.Count);
        int remainder = Mathf.Max(0, defenderPoolSize - (perType * defenderTypes.Count));

        for (int i = 0; i < defenderTypes.Count; i++)
        {
            int count = perType + (i < remainder ? 1 : 0);
            WarmPoolForType(defenderTypes[i], count);
        }
    }

    private void WarmPoolForType(DefenderDefinition definition, int count)
    {
        if (definition == null || count <= 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            DefenderHealth defender = CreateDefenderInstance(definition);
            ReleaseDefender(defender);
        }
    }

    private DefenderHealth GetDefender(DefenderDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        if (usePooling)
        {
            DefenderPoolKey key = new DefenderPoolKey(definition, definition.Prefab);
            Queue<DefenderHealth> pool = GetPool(key);
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
        }

        return CreateDefenderInstance(definition);
    }

    private Queue<DefenderHealth> GetPool(DefenderPoolKey key)
    {
        if (!defenderPools.TryGetValue(key, out Queue<DefenderHealth> pool))
        {
            pool = new Queue<DefenderHealth>();
            defenderPools.Add(key, pool);
        }

        return pool;
    }

    private DefenderHealth CreateDefenderInstance(DefenderDefinition definition)
    {
        return CreateDefenderInstance(definition, definition != null ? definition.Prefab : null);
    }

    private DefenderHealth CreateDefenderInstance(DefenderDefinition definition, GameObject prefabOverride)
    {
        if (definition == null)
        {
            return null;
        }

        GameObject defenderObject = prefabOverride != null
            ? Instantiate(prefabOverride)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        defenderObject.name = "Defender";
        defenderObject.transform.SetParent(defenderContainer, false);
        LayerUtils.SetLayerRecursive(defenderObject, defenderLayer);

        if (defenderObject.GetComponentInChildren<Collider>() == null)
        {
            defenderObject.AddComponent<BoxCollider>();
        }

        DefenderHealth health = ComponentUtils.GetOrAddComponent<DefenderHealth>(defenderObject);
        ComponentUtils.GetOrAddComponent<DefenderAttack>(defenderObject);
        defenderPoolKeys[health] = new DefenderPoolKey(definition, prefabOverride);

        return health;
    }

    private void ReleaseDefender(DefenderHealth defender)
    {
        if (defender == null)
        {
            return;
        }

        defenderPoolKeys.TryGetValue(defender, out DefenderPoolKey key);

        if (!usePooling)
        {
            defenderPoolKeys.Remove(defender);
            Destroy(defender.gameObject);
            return;
        }

        if (key.Definition == null)
        {
            defenderPoolKeys.Remove(defender);
            Destroy(defender.gameObject);
            return;
        }

        defender.transform.SetParent(defenderContainer, false);
        defender.gameObject.SetActive(false);
        GetPool(key).Enqueue(defender);
    }

    private DefenderHealth SpawnDefender(Vector3 spotPosition, DefenderDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        DefenderHealth health = GetDefender(definition);
        if (health == null)
        {
            return null;
        }

        GameObject defenderObject = health.gameObject;
        if (usePooling)
        {
            defenderObject.SetActive(false);
        }
        defenderObject.transform.SetParent(defenderContainer, false);
        Vector3 spawnPosition = spotPosition + Vector3.up * definition.HeightOffset;
        defenderObject.transform.position = spawnPosition;

        if (definition.Prefab == null || definition.ApplyOverridesToPrefab)
        {
            definition.ApplyVisualOverrides(defenderObject);
        }

        health.SetReleaseAction(usePooling ? ReleaseDefender : null);

        DefenderAttack attack = ComponentUtils.GetOrAddComponent<DefenderAttack>(defenderObject);
        definition.Configure(health, attack, spawnPosition, terrain, enemyTargetMask);

        if (usePooling)
        {
            defenderObject.SetActive(true);
        }
        return health;
    }

    private bool IsPrimaryClickDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.wasPressedThisFrame;
        }
#endif
        return Input.GetMouseButtonDown(0);
    }

    private bool TryGetPointerPosition(out Vector2 position)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }
#endif
        if (Input.mousePresent)
        {
            position = Input.mousePosition;
            return true;
        }

        position = default;
        return false;
    }
}
