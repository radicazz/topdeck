using UnityEngine;
using UnityEngine.UIElements;

public class DefenderContextMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument hudDocument;
    [SerializeField] private DefenderPlacementManager placementManager;

    [Header("UI Element Names")]
    [SerializeField] private string menuName = "defender-menu";
    [SerializeField] private string menuTitleName = "defender-menu-title";
    [SerializeField] private string menuButtonsName = "defender-menu-buttons";

    private VisualElement menuRoot;
    private Label menuTitle;
    private VisualElement menuButtons;
    private DefenderPlacementSpot activeSpot;
    private DefenderHealth activeDefender;
    private GameManager boundGameManager;
    private Vector2 lastScreenPosition;
    private bool callbacksRegistered;

    public bool IsMenuVisible => menuRoot != null && !menuRoot.ClassListContains("hidden");

    private void Awake()
    {
        if (hudDocument == null)
        {
            hudDocument = FindFirstObjectByType<UIDocument>();
        }

        if (placementManager == null)
        {
            placementManager = FindFirstObjectByType<DefenderPlacementManager>();
        }
    }

    private void OnEnable()
    {
        CacheUi();
        Bind();
        HideMenu();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Bind()
    {
        if (boundGameManager != null)
        {
            boundGameManager.MoneyChanged -= HandleMoneyChanged;
        }

        boundGameManager = GameManager.Instance;
        if (boundGameManager != null)
        {
            boundGameManager.MoneyChanged += HandleMoneyChanged;
        }
    }

    private void Unbind()
    {
        if (boundGameManager != null)
        {
            boundGameManager.MoneyChanged -= HandleMoneyChanged;
            boundGameManager = null;
        }
    }

    private void CacheUi()
    {
        if (hudDocument == null)
        {
            return;
        }

        VisualElement root = hudDocument.rootVisualElement;
        if (root == null)
        {
            return;
        }

        menuRoot = root.Q<VisualElement>(menuName);
        menuTitle = root.Q<Label>(menuTitleName);
        menuButtons = root.Q<VisualElement>(menuButtonsName);

        if (!callbacksRegistered)
        {
            root.RegisterCallback<PointerDownEvent>(HandleRootPointerDown, TrickleDown.TrickleDown);
            callbacksRegistered = true;
        }
    }

    public void ShowPlacementMenu(DefenderPlacementSpot spot, Vector2 screenPosition)
    {
        if (menuRoot == null || placementManager == null || spot == null)
        {
            return;
        }

        activeSpot = spot;
        activeDefender = null;

        if (menuTitle != null)
        {
            menuTitle.text = "Choose Defender";
        }

        menuButtons?.Clear();
        if (menuButtons != null)
        {
            foreach (DefenderDefinition definition in placementManager.DefenderTypes)
            {
                if (definition == null)
                {
                    continue;
                }

                Button button = new Button(() => HandlePlaceClicked(definition));
                button.text = $"{definition.DisplayName} ${definition.Cost}";
                button.AddToClassList("menu-button");
                button.AddToClassList("menu-button--primary");
                bool canAfford = boundGameManager == null || boundGameManager.CanAfford(definition.Cost);
                button.SetEnabled(canAfford);
                menuButtons.Add(button);
            }
        }

        lastScreenPosition = screenPosition;
        ShowMenuAt(screenPosition);
    }

    public void ShowUpgradeMenu(DefenderHealth defender, Vector2 screenPosition)
    {
        if (menuRoot == null || placementManager == null || defender == null)
        {
            return;
        }

        activeSpot = null;
        activeDefender = defender;

        if (menuTitle != null)
        {
            menuTitle.text = "Defender Options";
        }

        menuButtons?.Clear();
        if (menuButtons != null)
        {
            Button upgradeButton = new Button(HandleUpgradeClicked);
            if (!placementManager.CanUpgrade(defender))
            {
                upgradeButton.text = "Upgrade (Max)";
                upgradeButton.SetEnabled(false);
            }
            else
            {
                int upgradeCost = placementManager.GetUpgradeCost(defender);
                string label = placementManager.GetUpgradeLabel(defender);
                upgradeButton.text = $"{label} ${upgradeCost}";
                bool canAfford = boundGameManager == null || boundGameManager.CanAfford(upgradeCost);
                upgradeButton.SetEnabled(canAfford);
            }
            upgradeButton.AddToClassList("menu-button");
            upgradeButton.AddToClassList("menu-button--primary");
            menuButtons.Add(upgradeButton);

            int refund = placementManager.GetSellRefund(defender);
            Button sellButton = new Button(HandleSellClicked)
            {
                text = $"Sell +${refund}"
            };
            sellButton.AddToClassList("menu-button");
            sellButton.AddToClassList("menu-button--secondary");
            menuButtons.Add(sellButton);
        }

        lastScreenPosition = screenPosition;
        ShowMenuAt(screenPosition);
    }

    public void HideMenu()
    {
        if (menuRoot == null)
        {
            return;
        }

        menuRoot.EnableInClassList("hidden", true);
        activeSpot = null;
        activeDefender = null;
    }

    public bool IsPointerOverMenu(Vector2 screenPosition)
    {
        if (menuRoot == null || menuRoot.ClassListContains("hidden"))
        {
            return false;
        }

        IPanel panel = menuRoot.panel;
        if (panel == null)
        {
            return false;
        }

        Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
        VisualElement picked = panel.Pick(panelPosition);
        if (picked == null)
        {
            return false;
        }

        return menuRoot.Contains(picked);
    }

    private void HandleRootPointerDown(PointerDownEvent evt)
    {
        if (menuRoot == null || menuRoot.ClassListContains("hidden"))
        {
            return;
        }

        if (menuRoot.worldBound.Contains(evt.position))
        {
            return;
        }

        HideMenu();
    }

    public void NotifyDefenderRemoved(DefenderHealth defender)
    {
        if (activeDefender == defender)
        {
            HideMenu();
        }
    }

    private void HandlePlaceClicked(DefenderDefinition definition)
    {
        if (activeSpot == null || placementManager == null)
        {
            return;
        }

        if (placementManager.TryPlaceDefender(activeSpot, definition))
        {
            Debug.Log($"Placed defender '{definition.DisplayName}' at {activeSpot.transform.position}.");
            HideMenu();
        }
        else
        {
            Debug.LogWarning($"Failed to place defender '{definition.DisplayName}'.");
            RefreshButtons();
        }
    }

    private void HandleUpgradeClicked()
    {
        if (activeDefender == null || placementManager == null)
        {
            return;
        }

        if (placementManager.TryUpgradeDefender(activeDefender))
        {
            Debug.Log("Defender upgraded.");
            HideMenu();
        }
        else
        {
            Debug.LogWarning("Failed to upgrade defender.");
            RefreshButtons();
        }
    }

    private void HandleSellClicked()
    {
        if (activeDefender == null || placementManager == null)
        {
            return;
        }

        if (placementManager.TrySellDefender(activeDefender))
        {
            Debug.Log("Defender sold.");
            HideMenu();
        }
        else
        {
            Debug.LogWarning("Failed to sell defender.");
            RefreshButtons();
        }
    }

    private void HandleMoneyChanged(int money)
    {
        if (menuRoot == null || menuRoot.ClassListContains("hidden"))
        {
            return;
        }

        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (activeSpot != null)
        {
            ShowPlacementMenu(activeSpot, lastScreenPosition);
            return;
        }

        if (activeDefender != null)
        {
            ShowUpgradeMenu(activeDefender, lastScreenPosition);
        }
    }

    private void ShowMenuAt(Vector2 screenPosition)
    {
        if (menuRoot == null)
        {
            return;
        }

        IPanel panel = menuRoot.panel;
        if (panel == null)
        {
            return;
        }

        Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
        menuRoot.style.left = panelPosition.x + 12f;
        menuRoot.style.top = panelPosition.y + 12f;
        menuRoot.EnableInClassList("hidden", false);

        menuRoot.schedule.Execute(() => ClampToPanel(panel)).ExecuteLater(0);
    }

    private void ClampToPanel(IPanel panel)
    {
        if (menuRoot == null || panel == null)
        {
            return;
        }

        Rect panelRect = panel.visualTree.layout;
        Rect bounds = menuRoot.layout;
        float x = menuRoot.resolvedStyle.left;
        float y = menuRoot.resolvedStyle.top;

        if (bounds.width <= 0f || bounds.height <= 0f)
        {
            return;
        }

        float maxX = panelRect.width - bounds.width - 8f;
        float maxY = panelRect.height - bounds.height - 8f;
        x = Mathf.Clamp(x, 8f, maxX);
        y = Mathf.Clamp(y, 8f, maxY);

        menuRoot.style.left = x;
        menuRoot.style.top = y;
    }
}
