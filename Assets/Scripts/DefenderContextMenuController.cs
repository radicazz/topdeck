using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DefenderContextMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument hudDocument;
    [SerializeField] private DefenderPlacementManager placementManager;

    [Header("UI Element Names")]
    [SerializeField] private string menuName = "defender-menu";
    [SerializeField] private string menuTitleName = "defender-menu-title";
    [SerializeField] private string menuButtonsName = "defender-menu-buttons";
    [SerializeField] private bool logMenuEvents;

    private VisualElement menuRoot;
    private Label menuTitle;
    private VisualElement menuButtons;
    private DefenderPlacementSpot activeSpot;
    private DefenderHealth activeDefender;
    private TowerUpgradeManager activeTower;
    private GameManager boundGameManager;
    private Vector2 lastScreenPosition;
    private bool callbacksRegistered;
    private bool warnedMissingHud;
    private bool warnedMissingMenu;
    private bool warnedMissingPanel;
    private bool retryShowQueued;
    private Vector2 retryScreenPosition;
    private int menuShownFrame = -1;
    private float menuShownTime = -1f;

#if UNITY_EDITOR
    [Header("Debug (Editor Only)")]
    [SerializeField] private bool showDebugOverlay = true;
    [SerializeField] private Vector2 debugOverlayOffset = new Vector2(16f, 16f);
    private const string DebugOverlayPrefKey = "Topdeck.DefenderMenuDebugOverlay";
#endif

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
#if UNITY_EDITOR
        SyncDebugOverlayPref();
#endif
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
            if (!warnedMissingHud)
            {
                Debug.LogWarning("DefenderContextMenuController: HUD UIDocument reference is missing.");
                warnedMissingHud = true;
            }
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

        if (menuRoot == null)
        {
            UIDocument fallback = FindHudDocumentWithMenu();
            if (fallback != null && fallback != hudDocument)
            {
                hudDocument = fallback;
                root = hudDocument.rootVisualElement;
                if (root != null)
                {
                    menuRoot = root.Q<VisualElement>(menuName);
                    menuTitle = root.Q<Label>(menuTitleName);
                    menuButtons = root.Q<VisualElement>(menuButtonsName);
                }
            }
        }

        if ((menuRoot == null || menuTitle == null || menuButtons == null) && !warnedMissingMenu)
        {
            Debug.LogWarning($"DefenderContextMenuController: Could not find UI elements ({menuName}, {menuTitleName}, {menuButtonsName}) on UIDocument '{hudDocument.name}'.");
            warnedMissingMenu = true;
        }

        if (!callbacksRegistered)
        {
            root.RegisterCallback<PointerDownEvent>(HandleRootPointerDown, TrickleDown.TrickleDown);
            callbacksRegistered = true;
        }
    }

#if UNITY_EDITOR
    private void SyncDebugOverlayPref()
    {
        showDebugOverlay = EditorPrefs.GetBool(DebugOverlayPrefKey, showDebugOverlay);
    }

    private void OnValidate()
    {
        EditorPrefs.SetBool(DebugOverlayPrefKey, showDebugOverlay);
    }
#endif
    private UIDocument FindHudDocumentWithMenu()
    {
        UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        if (documents == null)
        {
            return null;
        }

        for (int i = 0; i < documents.Length; i++)
        {
            UIDocument document = documents[i];
            if (document == null)
            {
                continue;
            }

            VisualElement root = document.rootVisualElement;
            if (root == null)
            {
                continue;
            }

            if (root.Q<VisualElement>(menuName) != null)
            {
                return document;
            }
        }

        return null;
    }

    public void ShowPlacementMenu(DefenderPlacementSpot spot, Vector2 screenPosition)
    {
        if (menuRoot == null || placementManager == null || spot == null)
        {
            if (menuRoot == null && !warnedMissingMenu)
            {
                Debug.LogWarning("DefenderContextMenuController: Menu root is missing; cannot show placement menu.");
                warnedMissingMenu = true;
            }
            return;
        }

        activeSpot = spot;
        activeDefender = null;
        activeTower = null;
        if (logMenuEvents)
        {
            Debug.Log($"DefenderContextMenuController: Showing placement menu for spot '{spot.name}'.");
        }

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
            if (menuRoot == null && !warnedMissingMenu)
            {
                Debug.LogWarning("DefenderContextMenuController: Menu root is missing; cannot show upgrade menu.");
                warnedMissingMenu = true;
            }
            return;
        }

        activeSpot = null;
        activeDefender = defender;
        activeTower = null;
        if (logMenuEvents)
        {
            Debug.Log($"DefenderContextMenuController: Showing upgrade menu for defender '{defender.name}'.");
        }

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

    public void ShowTowerUpgradeMenu(TowerUpgradeManager tower, Vector2 screenPosition)
    {
        if (menuRoot == null || tower == null)
        {
            if (menuRoot == null && !warnedMissingMenu)
            {
                Debug.LogWarning("DefenderContextMenuController: Menu root is missing; cannot show tower upgrade menu.");
                warnedMissingMenu = true;
            }
            return;
        }

        activeSpot = null;
        activeDefender = null;
        activeTower = tower;
        if (logMenuEvents)
        {
            Debug.Log($"DefenderContextMenuController: Showing upgrade menu for tower '{tower.name}'.");
        }

        if (menuTitle != null)
        {
            menuTitle.text = "Tower Options";
        }

        menuButtons?.Clear();
        if (menuButtons != null)
        {
            Button upgradeButton = new Button(HandleUpgradeClicked);
            if (!tower.CanUpgrade())
            {
                upgradeButton.text = "Upgrade (Max)";
                upgradeButton.SetEnabled(false);
            }
            else
            {
                int upgradeCost = tower.GetUpgradeCost();
                string label = tower.GetUpgradeLabel();
                upgradeButton.text = $"{label} ${upgradeCost}";
                bool canAfford = boundGameManager == null || boundGameManager.CanAfford(upgradeCost);
                upgradeButton.SetEnabled(canAfford);
            }
            upgradeButton.AddToClassList("menu-button");
            upgradeButton.AddToClassList("menu-button--primary");
            menuButtons.Add(upgradeButton);
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

        if (logMenuEvents && !menuRoot.ClassListContains("hidden"))
        {
            Debug.Log("DefenderContextMenuController: Hiding menu.");
        }

        menuRoot.EnableInClassList("hidden", true);
        activeSpot = null;
        activeDefender = null;
        activeTower = null;
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

        if (Time.frameCount == menuShownFrame || Time.unscaledTime - menuShownTime < 0.05f)
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
        if (activeTower == null && (activeDefender == null || placementManager == null))
        {
            return;
        }

        if (activeTower != null)
        {
            if (activeTower.TryUpgradeTower())
            {
                Debug.Log("Tower upgraded.");
                HideMenu();
            }
            else
            {
                Debug.LogWarning("Failed to upgrade tower.");
                RefreshButtons();
            }
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
        if (activeTower != null)
        {
            return;
        }

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

        if (activeTower != null)
        {
            ShowTowerUpgradeMenu(activeTower, lastScreenPosition);
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
            if (!warnedMissingPanel)
            {
                Debug.LogWarning("DefenderContextMenuController: UI panel not ready; deferring menu display.");
                warnedMissingPanel = true;
            }

            if (!retryShowQueued)
            {
                retryShowQueued = true;
                retryScreenPosition = screenPosition;
                menuRoot.schedule.Execute(() =>
                {
                    retryShowQueued = false;
                    ShowMenuAt(retryScreenPosition);
                }).ExecuteLater(0);
            }
            return;
        }

        Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
        menuRoot.style.left = panelPosition.x + 12f;
        menuRoot.style.top = panelPosition.y + 12f;
        menuRoot.EnableInClassList("hidden", false);
        menuShownFrame = Time.frameCount;
        menuShownTime = Time.unscaledTime;

        menuRoot.schedule.Execute(() => ClampToPanel(panel)).ExecuteLater(0);
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!showDebugOverlay)
        {
            return;
        }

        string spotName = activeSpot != null ? activeSpot.name : "None";
        string defenderName = activeDefender != null ? activeDefender.name : "None";
        string menuState = menuRoot != null && !menuRoot.ClassListContains("hidden") ? "Visible" : "Hidden";
        string gameState = $"Started={GameManager.IsGameStarted} Over={GameManager.IsGameOver}";
        string text =
            $"Defender Menu: {menuState}\n" +
            $"{gameState}\n" +
            $"Active Spot: {spotName}\n" +
            $"Active Defender: {defenderName}\n" +
            $"Last Screen Pos: {lastScreenPosition}\n" +
            $"HUD Document: {(hudDocument != null ? hudDocument.name : "Missing")}\n" +
            $"Menu Root: {(menuRoot != null ? "OK" : "Missing")}";

        Rect rect = new Rect(debugOverlayOffset.x, debugOverlayOffset.y, 320f, 120f);
        GUI.Box(rect, "Defender Menu Debug");
        GUI.Label(new Rect(rect.x + 8f, rect.y + 20f, rect.width - 16f, rect.height - 24f), text);
    }
#endif

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
