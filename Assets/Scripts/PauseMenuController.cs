using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PauseMenuController : MonoBehaviour
{
    private const Key DefaultToggleKey = Key.Escape;
    private const Key DefaultAlternateToggleKey = Key.P;

    [Header("References")]
    [SerializeField] private UIDocument hudDocument;

    [Header("UI Element Names")]
    [SerializeField] private string pauseOverlayName = "pause-overlay";
    [SerializeField] private string resumeButtonName = "resume-button";
    [SerializeField] private string quitButtonName = "quit-button";

    [Header("Input")]
    [SerializeField] private Key toggleKey = DefaultToggleKey;
    [SerializeField] private Key alternateToggleKey = DefaultAlternateToggleKey;

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

        NormalizeToggleKeys();
    }

    private void OnEnable()
    {
        CacheUi();
        Bind();
    }

    private void Start()
    {
        CacheUi();
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

        if (!GameManager.IsGameStarted)
        {
            return;
        }

        if (pauseOverlay == null)
        {
            CacheUi();
        }

        if (WasTogglePressed())
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

    private bool WasTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard[toggleKey].wasPressedThisFrame || keyboard[alternateToggleKey].wasPressedThisFrame)
            {
                return true;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            return true;
        }
#endif

        return false;
    }

    private void NormalizeToggleKeys()
    {
        if (toggleKey == (Key)KeyCode.Escape)
        {
            toggleKey = DefaultToggleKey;
        }

        if (alternateToggleKey == (Key)KeyCode.P)
        {
            alternateToggleKey = DefaultAlternateToggleKey;
        }

        if (toggleKey == Key.None)
        {
            toggleKey = DefaultToggleKey;
        }

        if (alternateToggleKey == Key.None)
        {
            alternateToggleKey = DefaultAlternateToggleKey;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        NormalizeToggleKeys();
    }
#endif

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
        pauseOverlay.EnableInClassList("hidden", !isVisible);
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
