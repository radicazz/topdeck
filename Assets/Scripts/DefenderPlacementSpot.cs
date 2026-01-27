using UnityEngine;

public class DefenderPlacementSpot : MonoBehaviour
{
    private DefenderPlacementManager manager;
    private bool occupied;
    private DefenderHealth defender;
    private Color availableColor;
    private Color occupiedColor;
    private MeshRenderer cachedRenderer;

    public void Initialize(DefenderPlacementManager owner, Color available, Color occupiedColorValue)
    {
        manager = owner;
        availableColor = available;
        occupiedColor = occupiedColorValue;
        cachedRenderer = GetComponent<MeshRenderer>();
        SetColor(availableColor);
    }

    private void Update()
    {
        if (occupied && defender == null)
        {
            occupied = false;
            SetColor(availableColor);
        }
    }

    private void OnMouseDown()
    {
        if (occupied || manager == null || GameManager.IsGameOver)
        {
            return;
        }

        defender = manager.SpawnDefender(transform.position);
        if (defender != null)
        {
            occupied = true;
            SetColor(occupiedColor);
        }
    }

    private void SetColor(Color color)
    {
        if (cachedRenderer != null)
        {
            cachedRenderer.material.color = color;
        }
    }
}
