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

    [Header("Defender")]
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

    [Header("Defender Movement")]
    [SerializeField] private float defenderMoveRadius = 0.6f;
    [SerializeField] private float defenderMoveSpeed = 1.5f;
    [SerializeField] private float defenderTurnSpeed = 10f;

    [Header("Pooling")]
    [SerializeField] private bool usePooling = true;
    [SerializeField, Min(0)] private int defenderPoolSize = 0;

    private readonly List<DefenderPlacementSpot> spots = new List<DefenderPlacementSpot>();
    private readonly Queue<DefenderHealth> defenderPool = new Queue<DefenderHealth>();
    private Camera mainCamera;
    private Transform defenderContainer;
    private int defenderLayer = -1;
    private int placementLayer = -1;

    private void Awake()
    {
        mainCamera = Camera.main;
        Transform existing = transform.Find("Defenders");
        defenderContainer = existing != null ? existing : new GameObject("Defenders").transform;
        defenderContainer.SetParent(transform, false);
        defenderLayer = LayerMask.NameToLayer("Defender");
        placementLayer = LayerMask.NameToLayer("Placement");
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
        WarmPool();
    }

    private void Update()
    {
        if (GameManager.IsGameOver)
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

    private void BuildPlacementSpots()
    {
        ClearSpots();

        List<Vector2Int> candidates = new List<Vector2Int>();
        int width = terrain.Width;
        int height = terrain.Height;
        Vector2Int center = new Vector2Int(width / 2, height / 2);

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

                candidates.Add(new Vector2Int(x, y));
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("DefenderPlacementManager: No valid placement spots found.");
            return;
        }

        var random = new System.Random(terrain.LastSeedUsed + 1337);
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            Vector2Int temp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = temp;
        }

        int count = Mathf.Min(placementCount, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            Vector2Int cell = candidates[i];
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

        for (int i = 0; i < defenderPoolSize; i++)
        {
            DefenderHealth defender = CreateDefenderInstance();
            ReleaseDefender(defender);
        }
    }

    private DefenderHealth GetDefender()
    {
        if (usePooling && defenderPool.Count > 0)
        {
            return defenderPool.Dequeue();
        }

        return CreateDefenderInstance();
    }

    private DefenderHealth CreateDefenderInstance()
    {
        GameObject defenderObject = defenderPrefab != null
            ? Instantiate(defenderPrefab)
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

        return health;
    }

    private void ReleaseDefender(DefenderHealth defender)
    {
        if (defender == null)
        {
            return;
        }

        if (!usePooling)
        {
            Destroy(defender.gameObject);
            return;
        }

        defender.transform.SetParent(defenderContainer, false);
        defender.gameObject.SetActive(false);
        defenderPool.Enqueue(defender);
    }

    public DefenderHealth SpawnDefender(Vector3 spotPosition)
    {
        if (GameManager.Instance != null && !GameManager.Instance.TryPurchaseDefender())
        {
            return null;
        }

        DefenderHealth health = GetDefender();
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
        Vector3 spawnPosition = spotPosition + Vector3.up * defenderHeightOffset;
        defenderObject.transform.position = spawnPosition;

        if (defenderPrefab == null || applyOverridesToPrefab)
        {
            defenderObject.transform.localScale = defenderScale;
            var renderer = defenderObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                RendererUtils.SetColor(renderer, defenderColor);
            }
        }

        health.Initialize(defenderMaxHealth);
        health.SetReleaseAction(usePooling ? ReleaseDefender : null);

        DefenderAttack attack = ComponentUtils.GetOrAddComponent<DefenderAttack>(defenderObject);
        attack.Configure(defenderRange, defenderAttackInterval, defenderDamage, enemyTargetMask);
        attack.ConfigureMovement(spawnPosition, defenderMoveRadius, defenderMoveSpeed, defenderTurnSpeed);

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
