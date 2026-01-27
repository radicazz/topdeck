using System;
using System.Collections.Generic;
using UnityEngine;

public class DefenderPlacementManager : MonoBehaviour
{
    [Header("References")]
    public ProceduralTerrainGenerator terrain;

    [Header("Placement Spots")]
    [Min(1)] public int placementCount = 12;
    public float spotScale = 0.4f;
    public float spotHeightOffset = 0.1f;
    public Color spotAvailableColor = new Color(0.2f, 0.5f, 1f);
    public Color spotOccupiedColor = new Color(0.35f, 0.35f, 0.35f);

    [Header("Defender")]
    public float defenderMaxHealth = 6f;
    public float defenderRange = 4f;
    public float defenderAttackInterval = 0.6f;
    public float defenderDamage = 1f;
    public float defenderHeightOffset = 0.5f;
    public Vector3 defenderScale = new Vector3(0.8f, 0.8f, 0.8f);
    public Color defenderColor = new Color(0.2f, 0.8f, 0.2f);

    private readonly List<DefenderPlacementSpot> spots = new List<DefenderPlacementSpot>();

    private void Start()
    {
        if (terrain == null)
        {
            terrain = FindObjectOfType<ProceduralTerrainGenerator>();
        }

        if (terrain == null)
        {
            Debug.LogWarning("DefenderPlacementManager: Terrain generator not found.");
            enabled = false;
            return;
        }

        BuildPlacementSpots();
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

        var renderer = spotObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = spotAvailableColor;
        }

        var spot = spotObject.AddComponent<DefenderPlacementSpot>();
        spot.Initialize(this, spotAvailableColor, spotOccupiedColor);
        spots.Add(spot);
    }

    public DefenderHealth SpawnDefender(Vector3 spotPosition)
    {
        GameObject defenderObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        defenderObject.name = "Defender";
        defenderObject.transform.position = spotPosition + Vector3.up * defenderHeightOffset;
        defenderObject.transform.localScale = defenderScale;

        var renderer = defenderObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = defenderColor;
        }

        var health = defenderObject.AddComponent<DefenderHealth>();
        health.Initialize(defenderMaxHealth);

        var attack = defenderObject.AddComponent<DefenderAttack>();
        attack.range = defenderRange;
        attack.attackInterval = defenderAttackInterval;
        attack.damagePerShot = defenderDamage;

        return health;
    }
}
