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

    [Header("Pooling")]
    [SerializeField] private bool usePooling = true;
    [SerializeField, Min(0)] private int defenderPoolSize = 0;

    private readonly List<DefenderPlacementSpot> spots = new List<DefenderPlacementSpot>();
    private readonly Dictionary<DefenderDefinition, Queue<DefenderHealth>> defenderPools = new Dictionary<DefenderDefinition, Queue<DefenderHealth>>();
    private readonly Dictionary<DefenderHealth, DefenderDefinition> defenderDefinitions = new Dictionary<DefenderHealth, DefenderDefinition>();
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

    private void Awake()
    {
        mainCamera = Camera.main;
        Transform existing = transform.Find("Defenders");
        defenderContainer = existing != null ? existing : new GameObject("Defenders").transform;
        defenderContainer.SetParent(transform, false);
        defenderLayer = LayerMask.NameToLayer("Defender");
        placementLayer = LayerMask.NameToLayer("Placement");
        EnsureDefenderTypes();
        SelectDefender(GetDefaultDefender());
    }

    private void Start()
    {
        if (terrain == null)
        {
            terrain = FindFirstObjectByType<ProceduralTerrainGenerator>();
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

        Camera cameraToUse = mainCamera != null ? mainCamera : Camera.main;
        if (cameraToUse == null)
        {
            return;
        }

        Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            DefenderPlacementSpot spot = hit.collider.GetComponent<DefenderPlacementSpot>();
            if (spot != null)
            {
                spot.TryPlace();
            }
        }
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

    private DefenderDefinition CreateBasicFallback()
    {
        return new DefenderDefinition
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
    }

    private DefenderDefinition CreateAdvancedFallback(DefenderDefinition basic)
    {
        if (basic == null)
        {
            basic = CreateBasicFallback();
        }

        return basic.CloneWithOverrides(
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
            Queue<DefenderHealth> pool = GetPool(definition);
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
        }

        return CreateDefenderInstance(definition);
    }

    private Queue<DefenderHealth> GetPool(DefenderDefinition definition)
    {
        if (!defenderPools.TryGetValue(definition, out Queue<DefenderHealth> pool))
        {
            pool = new Queue<DefenderHealth>();
            defenderPools.Add(definition, pool);
        }

        return pool;
    }

    private DefenderHealth CreateDefenderInstance(DefenderDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        GameObject defenderObject = definition.Prefab != null
            ? Instantiate(definition.Prefab)
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
        defenderDefinitions[health] = definition;

        return health;
    }

    private void ReleaseDefender(DefenderHealth defender)
    {
        if (defender == null)
        {
            return;
        }

        defenderDefinitions.TryGetValue(defender, out DefenderDefinition definition);

        if (!usePooling)
        {
            defenderDefinitions.Remove(defender);
            Destroy(defender.gameObject);
            return;
        }

        if (definition == null)
        {
            Destroy(defender.gameObject);
            return;
        }

        defender.transform.SetParent(defenderContainer, false);
        defender.gameObject.SetActive(false);
        GetPool(definition).Enqueue(defender);
    }

    public DefenderHealth SpawnDefender(Vector3 spotPosition)
    {
        EnsureDefenderTypes();
        if (selectedDefender == null)
        {
            SelectDefender(GetDefaultDefender());
        }

        DefenderDefinition definition = selectedDefender;
        if (definition == null)
        {
            return null;
        }

        if (GameManager.Instance != null && !GameManager.Instance.TryPurchaseDefender(definition.Cost))
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
