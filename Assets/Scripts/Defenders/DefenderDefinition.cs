using System;
using UnityEngine;

[Serializable]
public class DefenderDefinition
{
    [Header("Identity")]
    [SerializeField] private string id = "basic";
    [SerializeField] private string displayName = "Basic Ally";
    [SerializeField, Min(0)] private int cost = 100;

    [Header("Visuals")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private bool applyOverridesToPrefab = true;
    [SerializeField] private Vector3 scale = new Vector3(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color color = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private float heightOffset = 0.5f;

    [Header("Combat")]
    [SerializeField, Min(0.1f)] private float maxHealth = 6f;
    [SerializeField, Min(0.1f)] private float range = 4f;
    [SerializeField, Min(0.05f)] private float attackInterval = 0.6f;
    [SerializeField, Min(0f)] private float damage = 1f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveRadius = 0.8f;
    [SerializeField, Min(0f)] private float moveSpeed = 1.5f;
    [SerializeField, Min(0f)] private float turnSpeed = 10f;

    public string Id
    {
        get => id;
        set => id = value;
    }

    public string DisplayName
    {
        get => displayName;
        set => displayName = value;
    }

    public int Cost
    {
        get => cost;
        set => cost = value;
    }

    public GameObject Prefab
    {
        get => prefab;
        set => prefab = value;
    }

    public bool ApplyOverridesToPrefab
    {
        get => applyOverridesToPrefab;
        set => applyOverridesToPrefab = value;
    }

    public Vector3 Scale
    {
        get => scale;
        set => scale = value;
    }

    public Color Color
    {
        get => color;
        set => color = value;
    }

    public float HeightOffset
    {
        get => heightOffset;
        set => heightOffset = value;
    }

    public float MaxHealth
    {
        get => maxHealth;
        set => maxHealth = value;
    }

    public float Range
    {
        get => range;
        set => range = value;
    }

    public float AttackInterval
    {
        get => attackInterval;
        set => attackInterval = value;
    }

    public float Damage
    {
        get => damage;
        set => damage = value;
    }

    public float MoveRadius
    {
        get => moveRadius;
        set => moveRadius = value;
    }

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    public float TurnSpeed
    {
        get => turnSpeed;
        set => turnSpeed = value;
    }

    public void ApplyVisualOverrides(GameObject defenderObject)
    {
        if (defenderObject == null)
        {
            return;
        }

        defenderObject.transform.localScale = scale;
        var renderer = defenderObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            RendererUtils.SetColor(renderer, color);
        }
    }

    public void Configure(DefenderHealth health, DefenderAttack attack, Vector3 anchor, ProceduralTerrainGenerator terrain, LayerMask targetMask)
    {
        if (health != null)
        {
            health.Initialize(maxHealth);
        }

        if (attack != null)
        {
            attack.Configure(range, attackInterval, damage, targetMask);
            attack.ConfigureMovement(anchor, moveRadius, moveSpeed, turnSpeed, terrain);
        }
    }

    public DefenderDefinition CloneWithOverrides(
        string newId,
        string newDisplayName,
        int newCost,
        float healthMultiplier,
        float rangeMultiplier,
        float attackIntervalMultiplier,
        float damageMultiplier,
        float moveRadiusMultiplier,
        float moveSpeedMultiplier,
        float turnSpeedMultiplier,
        Vector3 newScale,
        Color newColor,
        GameObject newPrefab,
        bool newApplyOverrides)
    {
        return new DefenderDefinition
        {
            id = newId,
            displayName = newDisplayName,
            cost = newCost,
            prefab = newPrefab != null ? newPrefab : prefab,
            applyOverridesToPrefab = newApplyOverrides,
            scale = newScale,
            color = newColor,
            heightOffset = heightOffset,
            maxHealth = Mathf.Max(0.1f, maxHealth * healthMultiplier),
            range = Mathf.Max(0.1f, range * rangeMultiplier),
            attackInterval = Mathf.Max(0.05f, attackInterval * attackIntervalMultiplier),
            damage = Mathf.Max(0f, damage * damageMultiplier),
            moveRadius = Mathf.Max(0f, moveRadius * moveRadiusMultiplier),
            moveSpeed = Mathf.Max(0f, moveSpeed * moveSpeedMultiplier),
            turnSpeed = Mathf.Max(0f, turnSpeed * turnSpeedMultiplier)
        };
    }
}
