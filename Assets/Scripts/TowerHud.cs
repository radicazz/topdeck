using UnityEngine;
using UnityEngine.UI;

public class TowerHud : MonoBehaviour
{
    [Header("References")]
    public TowerHealth tower;
    public Text towerHealthText;
    public Text gameOverText;

    [Header("Layout")]
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);
    public Vector2 healthPadding = new Vector2(24f, 24f);
    public int healthFontSize = 32;
    public int gameOverFontSize = 72;

    private void Awake()
    {
        if (tower == null)
        {
            tower = FindObjectOfType<TowerHealth>();
        }

        if (towerHealthText == null || gameOverText == null)
        {
            BuildUi();
        }
    }

    private void Update()
    {
        if (towerHealthText != null && tower != null)
        {
            towerHealthText.text = "Tower HP: " + Mathf.CeilToInt(tower.CurrentHealth);
        }

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(GameManager.IsGameOver);
        }
    }

    private void BuildUi()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
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
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = "";
        return text;
    }
}
