using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private Text scoreText;
    private Text playerNameText;
    private Text levelStatusText;
    private GameObject levelPanel;
    private Text levelTitle;
    private Text levelSummary;
    private Text levelButtonText;
    private GameObject pauseButton;
    private Text pauseButtonText;
    private GameObject pausePanel;
    private GameObject gameOverPanel;
    private GameObject winPanel;

    private float displayedScore;
    private int lastMilestone;
    private int shownLevelComplete;

    private static readonly Color PanelBg = new Color(0f, 0f, 0f, 0.78f);
    private static readonly Color WinBg = new Color(0f, 0.12f, 0.04f, 0.82f);

    void Start()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) { enabled = false; return; }

        BuildScoreLabel(canvas);
        BuildPlayerSidebar(canvas);
        BuildPauseButton(canvas);
        BuildPausePanel(canvas);
        BuildLevelPanel(canvas);
        BuildGameOverPanel(canvas);
        BuildWinPanel(canvas);

        if (FindAnyObjectByType<PlayerNameInput>() == null)
            new GameObject("PlayerNameInput").AddComponent<PlayerNameInput>();

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLevelComplete += ShowLevelComplete;
            GameStateManager.Instance.OnPauseChanged += ShowPauseChanged;
            GameStateManager.Instance.OnGameOver += ShowGameOver;
            GameStateManager.Instance.OnWin += ShowWin;
        }
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLevelComplete -= ShowLevelComplete;
            GameStateManager.Instance.OnPauseChanged -= ShowPauseChanged;
            GameStateManager.Instance.OnGameOver -= ShowGameOver;
            GameStateManager.Instance.OnWin -= ShowWin;
        }
    }

    void Update()
    {
        if (GameStateManager.Instance == null) return;

        if (playerNameText != null)
            playerNameText.text = PlayerNameInput.GetPlayerName();

        if (levelStatusText != null)
        {
            levelStatusText.text = "LEVEL " + GameStateManager.Instance.CurrentLevel;
        }

        if (GameStateManager.Instance.IsLevelPaused && shownLevelComplete != GameStateManager.Instance.CurrentLevel)
            ShowLevelComplete(GameStateManager.Instance.CurrentLevel);

        UpdatePauseButton();

        if (scoreText == null || GameStateManager.Instance.IsGameOver || GameStateManager.Instance.IsWin) return;

        float target = GameStateManager.Instance.Score;
        displayedScore = Mathf.Lerp(displayedScore, target, Time.unscaledDeltaTime * 8f);
        int shown = Mathf.RoundToInt(displayedScore);
        scoreText.text = shown + " m";

        int milestone = shown / 50;
        if (milestone > lastMilestone)
        {
            lastMilestone = milestone;
            Tween.PunchScale(this, scoreText.transform, Vector3.one * 0.18f, 0.25f);
        }
    }

    void BuildScoreLabel(Canvas c)
    {
        var root = MakeRect("ScoreRoot", c.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -18f), new Vector2(240f, 62f));
        var bg = MakeRect("Bg", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        scoreText = AddLabel(root, "0 m", 36, Color.white, Vector2.zero, new Vector2(240f, 62f));
        root.transform.localScale = Vector3.zero;
        Tween.Scale(this, root.transform, Vector3.one, 0.45f, Tween.Ease.OutBack, delay: 0.2f);
    }

    void BuildPlayerSidebar(Canvas c)
    {
        var panel = MakeRect("PlayerSidebar", c.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(126f, -82f), new Vector2(230f, 92f));
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

        AddLabel(panel, "PLAYER", 18, new Color(1f, 0.85f, 0.1f), new Vector2(0f, 24f), new Vector2(210f, 26f));
        playerNameText = AddLabel(panel, PlayerNameInput.GetPlayerName(), 26, Color.white, new Vector2(0f, -2f), new Vector2(210f, 34f));
        levelStatusText = AddLabel(panel, "LEVEL 1", 18, new Color(0.65f, 0.95f, 1f), new Vector2(0f, -30f), new Vector2(210f, 26f));
    }

    void BuildPauseButton(Canvas c)
    {
        pauseButton = MakeRect("PauseBtn", c.transform,
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-78f, -54f), new Vector2(112f, 56f));
        pauseButton.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        var btn = pauseButton.AddComponent<Button>();
        btn.targetGraphic = pauseButton.GetComponent<Image>();
        btn.onClick.AddListener(() => GameStateManager.Instance?.TogglePause());
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
        pauseButtonText = AddLabel(pauseButton, "PAUSE", 22, Color.white, Vector2.zero, new Vector2(112f, 56f));
        pauseButton.SetActive(false);
    }

    void BuildPausePanel(Canvas c)
    {
        pausePanel = MakeRect("PausePanel", c.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        pausePanel.AddComponent<Image>().color = PanelBg;
        AddLabel(pausePanel, "PAUSED", 64, new Color(1f, 0.85f, 0.1f), new Vector2(0f, 92f), new Vector2(760f, 90f));
        AddLabel(pausePanel, "Press P or Esc to resume", 26, Color.white, new Vector2(0f, 28f), new Vector2(680f, 48f));
        AddActionButton(pausePanel, "ResumeBtn", "RESUME", new Color(0.1f, 0.65f, 0.18f), new Vector2(0f, -54f), new Vector2(240f, 62f), () => GameStateManager.Instance?.ResumeGame());
        AddActionButton(pausePanel, "MenuBtn", "MAIN MENU", new Color(0.22f, 0.22f, 0.55f), new Vector2(0f, -134f), new Vector2(240f, 62f), () => GameStateManager.Instance?.GoToMainMenu());
        pausePanel.SetActive(false);
    }

    void UpdatePauseButton()
    {
        if (pauseButton == null || GameStateManager.Instance == null) return;
        bool canShow = !PlayerNameInput.IsOpen &&
            !GameStateManager.Instance.IsGameOver &&
            !GameStateManager.Instance.IsWin &&
            !GameStateManager.Instance.IsLevelPaused;
        pauseButton.SetActive(canShow);
        if (pauseButtonText != null)
            pauseButtonText.text = GameStateManager.Instance.IsPaused ? "RESUME" : "PAUSE";
    }

    void ShowPauseChanged(bool paused)
    {
        if (pausePanel != null)
            pausePanel.SetActive(paused);
        UpdatePauseButton();
    }

    void BuildLevelPanel(Canvas c)
    {
        levelPanel = MakeRect("LevelCompletePanel", c.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        levelPanel.AddComponent<Image>().color = new Color(0.03f, 0.05f, 0.12f, 0.86f);

        levelTitle = AddLabel(levelPanel, "LEVEL 1 COMPLETE", 62, new Color(1f, 0.85f, 0.1f), new Vector2(0f, 100f), new Vector2(900f, 90f));
        levelSummary = AddLabel(levelPanel, "1000 m reached", 28, Color.white, new Vector2(0f, 26f), new Vector2(760f, 88f));
        AddActionButton(levelPanel, "NextLevelBtn", "PLAY LEVEL 2", new Color(0.1f, 0.65f, 0.18f), new Vector2(0f, -82f), new Vector2(280f, 66f), ContinueLevel);
        levelButtonText = levelPanel.transform.Find("NextLevelBtn/Txt")?.GetComponent<Text>();
        levelPanel.SetActive(false);
    }

    void ShowLevelComplete(int level)
    {
        if (levelPanel == null || level <= 0) return;
        if (pausePanel != null) pausePanel.SetActive(false);
        shownLevelComplete = level;
        levelTitle.text = "LEVEL " + level + " COMPLETE";
        if (GameStateManager.Instance != null)
            levelSummary.text = "Level Score: " + Mathf.RoundToInt(GameStateManager.Instance.LastCompletedLevelScore)
                + " m\nTotal Score: " + Mathf.RoundToInt(GameStateManager.Instance.CompletedTotalScore) + " m";
        if (levelButtonText != null) levelButtonText.text = "PLAY LEVEL " + (level + 1);
        levelPanel.SetActive(true);
        Tween.PunchScale(this, levelTitle.transform, Vector3.one * 0.16f, 0.35f);
    }

    void ContinueLevel()
    {
        if (levelPanel != null) levelPanel.SetActive(false);
        shownLevelComplete = 0;
        GameStateManager.Instance?.ContinueToNextLevel();
    }

    void BuildGameOverPanel(Canvas c)
    {
        gameOverPanel = MakeRect("GameOverPanel", c.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        gameOverPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        AddLabel(gameOverPanel, "GAME OVER", 64, new Color(1f, 0.22f, 0.22f), new Vector2(0f, 90f), new Vector2(800f, 90f));
        AddLabel(gameOverPanel, "", 28, Color.white, new Vector2(0f, 20f), new Vector2(700f, 60f)).gameObject.name = "GOSummary";
        AddActionButton(gameOverPanel, "TryAgainBtn", "TRY AGAIN", new Color(0.18f, 0.55f, 0.18f), new Vector2(0f, -60f), new Vector2(240f, 62f), () => GameStateManager.Instance?.Restart());
        AddActionButton(gameOverPanel, "MenuBtn", "MAIN MENU", new Color(0.22f, 0.22f, 0.55f), new Vector2(0f, -140f), new Vector2(240f, 62f), () => GameStateManager.Instance?.GoToMainMenu());
        gameOverPanel.SetActive(false);
    }

    void ShowGameOver()
    {
        if (gameOverPanel == null) return;
        if (pausePanel != null) pausePanel.SetActive(false);
        gameOverPanel.SetActive(true);
        var summary = gameOverPanel.transform.Find("GOSummary")?.GetComponent<Text>();
        if (summary != null && GameStateManager.Instance != null)
            summary.text = "Level " + GameStateManager.Instance.CurrentLevel + " Score: "
                + Mathf.RoundToInt(GameStateManager.Instance.Score) + " m\nTotal Score: "
                + Mathf.RoundToInt(GameStateManager.Instance.TotalScore) + " m";
        Tween.Color(this, gameOverPanel.GetComponent<Image>(), PanelBg, 0.5f);
    }

    void BuildWinPanel(Canvas c)
    {
        winPanel = MakeRect("WinPanel", c.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        winPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        AddLabel(winPanel, "LEVEL 4 COMPLETE", 60, new Color(0.2f, 1f, 0.3f), new Vector2(0f, 100f), new Vector2(900f, 90f));
        AddLabel(winPanel, "", 28, Color.white, new Vector2(0f, 28f), new Vector2(760f, 60f)).gameObject.name = "WinSummary";
        AddActionButton(winPanel, "PlayAgainBtn", "PLAY AGAIN", new Color(0.1f, 0.65f, 0.18f), new Vector2(0f, -60f), new Vector2(260f, 66f), () => GameStateManager.Instance?.RestartFromLevelOne());
        AddActionButton(winPanel, "MenuBtn", "MAIN MENU", new Color(0.22f, 0.22f, 0.55f), new Vector2(0f, -145f), new Vector2(260f, 66f), () => GameStateManager.Instance?.GoToMainMenu());
        winPanel.SetActive(false);
    }

    void ShowWin()
    {
        if (winPanel == null) return;
        if (pausePanel != null) pausePanel.SetActive(false);
        winPanel.SetActive(true);
        var summary = winPanel.transform.Find("WinSummary")?.GetComponent<Text>();
        if (summary != null && GameStateManager.Instance != null)
            summary.text = "All 4 levels cleared!\nFinal Score: "
                + Mathf.RoundToInt(GameStateManager.Instance.CompletedTotalScore) + " m";
        Tween.Color(this, winPanel.GetComponent<Image>(), WinBg, 0.5f);
    }

    static GameObject MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return go;
    }

    static Text AddLabel(GameObject parent, string content, int size, Color col, Vector2 pos, Vector2 sizeDelta)
    {
        var go = MakeRect("Label", parent.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, sizeDelta);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.color = col;
        t.alignment = TextAnchor.MiddleCenter;
        return t;
    }

    static void AddActionButton(GameObject parent, string goName, string label, Color bgColor, Vector2 pos, Vector2 size, System.Action onClick)
    {
        var go = MakeRect(goName, parent.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
        AddLabel(go, label, 30, Color.white, Vector2.zero, size).gameObject.name = "Txt";
    }
}
