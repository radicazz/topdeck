using UnityEngine;
using UnityEngine.UI;

public class TowerHud : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TowerHealth tower;
    [SerializeField] private Text towerHealthText;
    [SerializeField] private Text moneyText;
    [SerializeField] private Text roundText;
    [SerializeField] private Text gameOverText;

    [Header("Layout")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private Vector2 healthPadding = new Vector2(24f, 24f);
    [SerializeField] private Vector2 moneyPadding = new Vector2(24f, 70f);
    [SerializeField] private Vector2 roundPadding = new Vector2(24f, 24f);
    [SerializeField] private int healthFontSize = 32;
    [SerializeField] private int moneyFontSize = 28;
    [SerializeField] private int roundFontSize = 28;
    [SerializeField] private int gameOverFontSize = 72;

    private GameManager boundGameManager;
    private TowerHealth boundTower;

    private void Awake()
    {
        if (tower == null)
        {
            tower = FindFirstObjectByType<TowerHealth>();
        }

        if (towerHealthText == null || moneyText == null || roundText == null || gameOverText == null)
        {
            BuildUi();
        }
    }

    private void OnEnable()
    {
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

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(GameManager.IsGameOver);
        }
    }

    private void HandleTowerHealthChanged(float health)
    {
        if (towerHealthText != null)
        {
            towerHealthText.text = "Tower HP: " + Mathf.CeilToInt(health);
        }
    }

    private void HandleMoneyChanged(int money)
    {
        if (moneyText != null)
        {
            moneyText.text = "Money: $" + money;
        }
    }

    private void HandleRoundChanged(int round, bool inProgress)
    {
        if (roundText != null)
        {
            string stateLabel = inProgress ? "" : " (prep)";
            roundText.text = "Round " + round + stateLabel;
        }
    }

    private void HandleGameOverTriggered()
    {
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
        }
    }

    private void BuildUi()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("HUDCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (towerHealthText == null)
        {
            towerHealthText = CreateText(canvas.transform, "TowerHealthText");
            towerHealthText.fontSize = healthFontSize;
            towerHealthText.alignment = TextAnchor.UpperLeft;
            towerHealthText.color = Color.white;

            RectTransform rect = towerHealthText.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(healthPadding.x, -healthPadding.y);
            rect.sizeDelta = new Vector2(400f, 80f);
        }

        if (moneyText == null)
        {
            moneyText = CreateText(canvas.transform, "MoneyText");
            moneyText.fontSize = moneyFontSize;
            moneyText.alignment = TextAnchor.UpperLeft;
            moneyText.color = Color.white;

            RectTransform rect = moneyText.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(moneyPadding.x, -moneyPadding.y);
            rect.sizeDelta = new Vector2(400f, 80f);
        }

        if (roundText == null)
        {
            roundText = CreateText(canvas.transform, "RoundText");
            roundText.fontSize = roundFontSize;
            roundText.alignment = TextAnchor.UpperRight;
            roundText.color = Color.white;

            RectTransform rect = roundText.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-roundPadding.x, -roundPadding.y);
            rect.sizeDelta = new Vector2(300f, 80f);
        }

        if (gameOverText == null)
        {
            gameOverText = CreateText(canvas.transform, "GameOverText");
            gameOverText.fontSize = gameOverFontSize;
            gameOverText.alignment = TextAnchor.MiddleCenter;
            gameOverText.color = new Color(1f, 0.2f, 0.2f);
            gameOverText.text = "GAME OVER";

            RectTransform rect = gameOverText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(800f, 200f);

            gameOverText.gameObject.SetActive(false);
        }
    }

    private Text CreateText(Transform parent, string objectName)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "";
        return text;
    }
}
