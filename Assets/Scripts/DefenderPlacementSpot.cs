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

    public void TryPlace()
    {
        if (occupied || manager == null || GameManager.IsGameOver || !GameManager.IsGameStarted)
        {
            return;
        }

        defender = manager.SpawnDefender(transform.position);
        AssignDefender(defender);
    }

    private void AssignDefender(DefenderHealth newDefender)
    {
        if (defender != null)
        {
            defender.Died -= HandleDefenderDied;
        }

        defender = newDefender;
        occupied = defender != null;
        SetColor(occupied ? occupiedColor : availableColor);

        if (defender != null)
        {
            defender.Died += HandleDefenderDied;
        }
    }

    private void HandleDefenderDied(DefenderHealth deadDefender)
    {
        if (defender == deadDefender)
        {
            defender.Died -= HandleDefenderDied;
            defender = null;
            occupied = false;
            SetColor(availableColor);
        }
    }

    private void OnDisable()
    {
        if (defender != null)
        {
            defender.Died -= HandleDefenderDied;
        }
    }

    private void SetColor(Color color)
    {
        if (cachedRenderer != null)
        {
            RendererUtils.SetColor(cachedRenderer, color);
        }
    }
}
