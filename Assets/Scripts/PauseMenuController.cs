using UnityEngine;
using UnityEngine.UIElements;

public class PauseMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument hudDocument;

    [Header("UI Element Names")]
    [SerializeField] private string pauseOverlayName = "pause-overlay";
    [SerializeField] private string resumeButtonName = "resume-button";
    [SerializeField] private string quitButtonName = "quit-button";

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;
    [SerializeField] private KeyCode alternateToggleKey = KeyCode.P;

    private VisualElement pauseOverlay;
    private Button resumeButton;
    private Button quitButton;
    private bool isPaused;
    private GameManager boundGameManager;

    private void Awake()
    {
        if (hudDocument == null)
        {
            hudDocument = FindFirstObjectByType<UIDocument>();
        }
    }

    private void OnEnable()
    {
        CacheUi();
        Bind();
    }

    private void OnDisable()
    {
        Unbind();
        if (isPaused)
        {
            SetPaused(false);
        }
    }

    private void Update()
    {
        if (GameManager.IsGameOver)
        {
            return;
        }

        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(alternateToggleKey))
        {
            TogglePause();
        }
    }

    private void Bind()
    {
        if (boundGameManager != null)
        {
            boundGameManager.GameOverTriggered -= HandleGameOver;
        }

        boundGameManager = GameManager.Instance;
        if (boundGameManager != null)
        {
            boundGameManager.GameOverTriggered += HandleGameOver;
        }

        SetOverlayVisible(isPaused);
    }

    private void Unbind()
    {
        if (boundGameManager != null)
        {
            boundGameManager.GameOverTriggered -= HandleGameOver;
            boundGameManager = null;
        }

        if (resumeButton != null)
        {
            resumeButton.clicked -= HandleResumeClicked;
        }

        if (quitButton != null)
        {
            quitButton.clicked -= HandleQuitClicked;
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

        pauseOverlay = root.Q<VisualElement>(pauseOverlayName);
        resumeButton = root.Q<Button>(resumeButtonName);
        quitButton = root.Q<Button>(quitButtonName);

        if (resumeButton != null)
        {
            resumeButton.clicked -= HandleResumeClicked;
            resumeButton.clicked += HandleResumeClicked;
        }

        if (quitButton != null)
        {
            quitButton.clicked -= HandleQuitClicked;
            quitButton.clicked += HandleQuitClicked;
        }

        SetOverlayVisible(isPaused);
    }

    private void TogglePause()
    {
        SetPaused(!isPaused);
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        SetOverlayVisible(paused);
    }

    private void SetOverlayVisible(bool isVisible)
    {
        if (pauseOverlay == null)
        {
            return;
        }
        pauseOverlay.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void HandleResumeClicked()
    {
        SetPaused(false);
    }

    private void HandleQuitClicked()
    {
        SetPaused(false);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HandleGameOver()
    {
        if (isPaused)
        {
            SetPaused(false);
        }
        else
        {
            SetOverlayVisible(false);
        }
    }
}
