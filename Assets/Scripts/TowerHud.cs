using UnityEngine;
using UnityEngine.UIElements;

public class TowerHud : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TowerHealth tower;
    [SerializeField] private UIDocument hudDocument;
    [SerializeField] private VisualTreeAsset hudLayout;
    [SerializeField] private PanelSettings panelSettings;

    [Header("UI Element Names")]
    [SerializeField] private string towerHealthLabelName = "tower-health";
    [SerializeField] private string moneyLabelName = "money";
    [SerializeField] private string roundLabelName = "round";
    [SerializeField] private string menuOverlayName = "menu-overlay";
    [SerializeField] private string gameOverLabelName = "game-over";

    private GameManager boundGameManager;
    private TowerHealth boundTower;
    private Label towerHealthLabel;
    private Label moneyLabel;
    private Label roundLabel;
    private VisualElement menuOverlay;
    private Label gameOverLabel;

    private void Awake()
    {
        if (tower == null)
        {
            tower = FindFirstObjectByType<TowerHealth>();
        }
        EnsureDocument();
    }

    private void OnEnable()
    {
        EnsureDocument();
        CacheUi();
        Bind();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Bind()
    {
        if (tower == null)
        {
            tower = FindFirstObjectByType<TowerHealth>();
        }

        if (boundTower != null)
        {
            boundTower.HealthChanged -= HandleTowerHealthChanged;
        }

        boundTower = tower;
        if (boundTower != null)
        {
            boundTower.HealthChanged += HandleTowerHealthChanged;
        }

        if (boundGameManager != null)
        {
            boundGameManager.MoneyChanged -= HandleMoneyChanged;
            boundGameManager.RoundChanged -= HandleRoundChanged;
            boundGameManager.GameOverTriggered -= HandleGameOverTriggered;
        }

        boundGameManager = GameManager.Instance;
        if (boundGameManager != null)
        {
            boundGameManager.MoneyChanged += HandleMoneyChanged;
            boundGameManager.RoundChanged += HandleRoundChanged;
            boundGameManager.GameOverTriggered += HandleGameOverTriggered;
        }

        RefreshAll();
    }

    private void Unbind()
    {
        if (boundTower != null)
        {
            boundTower.HealthChanged -= HandleTowerHealthChanged;
            boundTower = null;
        }

        if (boundGameManager != null)
        {
            boundGameManager.MoneyChanged -= HandleMoneyChanged;
            boundGameManager.RoundChanged -= HandleRoundChanged;
            boundGameManager.GameOverTriggered -= HandleGameOverTriggered;
            boundGameManager = null;
        }
    }

    private void RefreshAll()
    {
        if (tower != null)
        {
            HandleTowerHealthChanged(tower.CurrentHealth);
        }

        if (boundGameManager != null)
        {
            HandleMoneyChanged(boundGameManager.CurrentMoney);
            HandleRoundChanged(boundGameManager.CurrentRound, boundGameManager.RoundInProgress);
        }

        SetMenuVisible(GameManager.IsGameOver);
    }

    private void HandleTowerHealthChanged(float health)
    {
        if (towerHealthLabel == null)
        {
            return;
        }
        towerHealthLabel.text = "Tower HP: " + Mathf.CeilToInt(health);
    }

    private void HandleMoneyChanged(int money)
    {
        if (moneyLabel == null)
        {
            return;
        }
        moneyLabel.text = "Money: $" + money;
    }

    private void HandleRoundChanged(int round, bool inProgress)
    {
        if (roundLabel == null)
        {
            return;
        }
        string stateLabel = inProgress ? "" : " (prep)";
        roundLabel.text = "Round " + round + stateLabel;
    }

    private void HandleGameOverTriggered()
    {
        SetMenuVisible(true);
    }

    private void EnsureDocument()
    {
        if (hudDocument == null)
        {
            hudDocument = FindFirstObjectByType<UIDocument>();
        }

        if (hudDocument == null)
        {
            GameObject documentObject = new GameObject("HUDDocument");
            hudDocument = documentObject.AddComponent<UIDocument>();
        }

        if (panelSettings != null && hudDocument.panelSettings == null)
        {
            hudDocument.panelSettings = panelSettings;
        }

        if (hudLayout != null && hudDocument.visualTreeAsset == null)
        {
            hudDocument.visualTreeAsset = hudLayout;
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

        towerHealthLabel = root.Q<Label>(towerHealthLabelName);
        moneyLabel = root.Q<Label>(moneyLabelName);
        roundLabel = root.Q<Label>(roundLabelName);
        menuOverlay = root.Q<VisualElement>(menuOverlayName);
        gameOverLabel = root.Q<Label>(gameOverLabelName);

        if (gameOverLabel != null && string.IsNullOrEmpty(gameOverLabel.text))
        {
            gameOverLabel.text = "GAME OVER";
        }

        SetMenuVisible(GameManager.IsGameOver);
    }

    private void SetMenuVisible(bool isVisible)
    {
        if (menuOverlay == null)
        {
            return;
        }
        menuOverlay.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
