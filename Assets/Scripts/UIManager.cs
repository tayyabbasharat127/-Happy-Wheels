using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // ── Score ──────────────────────────────────────────────────────────────────
    private Text       scoreText;

    // ── Player sidebar ─────────────────────────────────────────────────────────
    private Text       playerNameText;
    private Text       levelStatusText;

    // ── Lives ──────────────────────────────────────────────────────────────────
    private Text[]     heartTexts = new Text[3];

    // ── Fuel bar ───────────────────────────────────────────────────────────────
    private Image      fuelFill;
    private Text       fuelWarningText;
    private float      fuelPulseTimer;

    // ── Nitro charges ─────────────────────────────────────────────────────────
    private Text[]     nitroTexts = new Text[3];

    // ── Speedometer ────────────────────────────────────────────────────────────
    private Image      speedArc;
    private Text       speedValueText;   // numeric "XX km/h" inside the arc

    // ── Pause button ──────────────────────────────────────────────────────────
    private GameObject pauseButton;
    private Text       pauseButtonText;      // ">" shown when paused (resume indicator)
    private GameObject pauseIconBars;        // || bars shown when game is running

    // ── Panels ─────────────────────────────────────────────────────────────────
    private GameObject pausePanel;
    private GameObject levelPanel;
    private Text       levelTitle;
    private Text       levelSummary;
    private Text       levelButtonText;
    private Text[]     starTexts      = new Text[3];
    private GameObject gameOverPanel;
    private GameObject winPanel;
    private GameObject respawnOverlay;
    private Text       respawnText;
    private Text       respawnLivesText;

    // ── Score popup pool ───────────────────────────────────────────────────────
    private Canvas     canvas;

    // ── Internal state ─────────────────────────────────────────────────────────
    private float displayedScore;
    private int   lastMilestone;
    private int   shownLevelComplete;

    private static readonly Color PanelBg  = new Color(0f, 0f, 0f, 0.78f);
    private static readonly Color WinBg    = new Color(0f, 0.12f, 0.04f, 0.82f);
    private static readonly Color FuelGood = new Color(0.15f, 0.80f, 0.20f);
    private static readonly Color FuelMid  = new Color(1.00f, 0.75f, 0.00f);
    private static readonly Color FuelLow  = new Color(1.00f, 0.15f, 0.10f);

    // ══════════════════════════════════════════════════════════════════════════
    void Start()
    {
        canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) { enabled = false; return; }

        BuildScoreLabel(canvas);
        BuildPlayerSidebar(canvas);
        BuildLivesDisplay(canvas);
        BuildFuelBar(canvas);
        BuildNitroDisplay(canvas);
        BuildSpeedometer(canvas);
        BuildPauseButton(canvas);
        BuildPausePanel(canvas);
        BuildLevelPanel(canvas);
        BuildGameOverPanel(canvas);
        BuildWinPanel(canvas);
        BuildRespawnOverlay(canvas);

        if (FindAnyObjectByType<PlayerNameInput>() == null)
            new GameObject("PlayerNameInput").AddComponent<PlayerNameInput>();

        // Seed score display from previously completed levels so Level 2/3/4
        // don't animate up from 0 — they start from the accumulated total.
        if (GameStateManager.Instance != null)
        {
            displayedScore = GameStateManager.Instance.CompletedTotalScore;
            lastMilestone  = Mathf.RoundToInt(displayedScore) / 250;
        }

        // GameStateManager events
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLevelComplete += ShowLevelComplete;
            GameStateManager.Instance.OnPauseChanged  += ShowPauseChanged;
            GameStateManager.Instance.OnGameOver       += ShowGameOver;
            GameStateManager.Instance.OnWin            += ShowWin;
        }

        // LifeManager events
        if (LifeManager.Instance != null)
        {
            LifeManager.Instance.OnLifeChanged    += RefreshLives;
            LifeManager.Instance.OnRespawnStart   += ShowRespawnOverlay;
            LifeManager.Instance.OnRespawnEnd     += HideRespawnOverlay;
        }
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLevelComplete -= ShowLevelComplete;
            GameStateManager.Instance.OnPauseChanged  -= ShowPauseChanged;
            GameStateManager.Instance.OnGameOver       -= ShowGameOver;
            GameStateManager.Instance.OnWin            -= ShowWin;
        }
        if (LifeManager.Instance != null)
        {
            LifeManager.Instance.OnLifeChanged  -= RefreshLives;
            LifeManager.Instance.OnRespawnStart -= ShowRespawnOverlay;
            LifeManager.Instance.OnRespawnEnd   -= HideRespawnOverlay;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    void Update()
    {
        if (GameStateManager.Instance == null) return;

        UpdatePlayerSidebar();
        UpdatePauseButton();
        UpdateScoreDisplay();
        UpdateFuelBar();
        UpdateNitroDisplay();
        UpdateSpeedometer();

        // Re-show level panel if lost reference
        if (GameStateManager.Instance.IsLevelPaused && shownLevelComplete != GameStateManager.Instance.CurrentLevel)
            ShowLevelComplete(GameStateManager.Instance.CurrentLevel);
    }

    // ── Score display ──────────────────────────────────────────────────────────
    void UpdateScoreDisplay()
    {
        if (scoreText == null || GameStateManager.Instance.IsGameOver || GameStateManager.Instance.IsWin) return;

        // TotalScore = completed levels + current level progress
        float target = GameStateManager.Instance.TotalScore;
        displayedScore = Mathf.Lerp(displayedScore, target, Time.unscaledDeltaTime * 8f);
        int shown = Mathf.RoundToInt(displayedScore);
        scoreText.text = shown + " m";

        // Score punch + popup every 250m milestone
        int milestone = shown / 250;
        if (milestone > lastMilestone)
        {
            lastMilestone = milestone;
            Tween.PunchScale(this, scoreText.transform, Vector3.one * 0.22f, 0.3f);
            SpawnScorePopup(milestone * 250 + "m!");
        }
    }

    // ── Player sidebar ─────────────────────────────────────────────────────────
    void UpdatePlayerSidebar()
    {
        if (playerNameText != null)
            playerNameText.text = PlayerNameInput.GetPlayerName();
        if (levelStatusText != null)
            levelStatusText.text = "LEVEL " + GameStateManager.Instance.CurrentLevel;
    }

    // ── Fuel bar ───────────────────────────────────────────────────────────────
    void UpdateFuelBar()
    {
        if (fuelFill == null) return;
        CarController car = CarController.Instance;
        float f = car != null ? Mathf.Clamp01(car.fuel) : 1f;
        fuelFill.fillAmount = Mathf.Lerp(fuelFill.fillAmount, f, Time.unscaledDeltaTime * 10f);

        // Color shift
        Color target = f > 0.4f ? FuelGood : (f > 0.2f ? FuelMid : FuelLow);
        fuelFill.color = Color.Lerp(fuelFill.color, target, Time.unscaledDeltaTime * 6f);

        // Pulse when critical
        if (f < 0.2f)
        {
            fuelPulseTimer += Time.unscaledDeltaTime * 8f;
            float pulse = (Mathf.Sin(fuelPulseTimer) + 1f) * 0.5f;
            fuelFill.color = Color.Lerp(FuelLow * 0.6f, FuelLow, pulse);
            if (fuelWarningText != null) fuelWarningText.gameObject.SetActive(true);
        }
        else
        {
            fuelPulseTimer = 0f;
            if (fuelWarningText != null) fuelWarningText.gameObject.SetActive(false);
        }
    }

    // ── Nitro ─────────────────────────────────────────────────────────────────
    void UpdateNitroDisplay()
    {
        CarController car = CarController.Instance;
        int charges = car != null ? car.nitroCharges : CarController.MaxNitroCharges;
        bool nitroOn = car != null && car.IsNitroActive;
        for (int i = 0; i < nitroTexts.Length; i++)
        {
            if (nitroTexts[i] == null) continue;
            bool filled = i < charges;
            nitroTexts[i].fontStyle = filled ? FontStyle.Bold : FontStyle.Normal;
            nitroTexts[i].color     = filled
                ? (nitroOn ? new Color(0.4f, 0.8f, 1f) : new Color(0.2f, 0.6f, 1f))
                : new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }
    }

    // ── Speedometer arc ────────────────────────────────────────────────────────
    void UpdateSpeedometer()
    {
        if (speedArc == null) return;
        CarController car = CarController.Instance;
        float rawSpeed = car != null && car.carRigidbody != null
            ? car.carRigidbody.linearVelocity.magnitude : 0f;

        // Convert physics units/s to display km/h (feels realistic in this game's scale)
        float kmh = rawSpeed * 3.6f;
        speedArc.fillAmount = Mathf.Lerp(speedArc.fillAmount,
            Mathf.Clamp01(rawSpeed / 22f), Time.unscaledDeltaTime * 8f);

        // Arc color: green -> yellow -> red with speed
        float t = speedArc.fillAmount;
        speedArc.color = t < 0.5f
            ? Color.Lerp(FuelGood, FuelMid, t * 2f)
            : Color.Lerp(FuelMid,  FuelLow,  (t - 0.5f) * 2f);

        // Live numeric display inside arc
        if (speedValueText != null)
        {
            speedValueText.text  = Mathf.RoundToInt(kmh).ToString();
            speedValueText.color = speedArc.color;
        }
    }

    // ── Lives ─────────────────────────────────────────────────────────────────
    void RefreshLives(int remaining)
    {
        for (int i = 0; i < heartTexts.Length; i++)
        {
            if (heartTexts[i] == null) continue;
            bool alive = i < remaining;
            // Use ASCII-safe characters — LegacyRuntime.ttf on WebGL may not support all Unicode
            heartTexts[i].text      = alive ? "<3" : " X";
            heartTexts[i].fontSize  = alive ? 22   : 26;
            heartTexts[i].color     = alive ? new Color(1f, 0.25f, 0.35f) : new Color(0.35f, 0.35f, 0.35f, 0.5f);
            if (!alive) Tween.PunchScale(this, heartTexts[i].transform, Vector3.one * 0.3f, 0.3f);
        }
    }

    // ── Respawn overlay ────────────────────────────────────────────────────────
    void ShowRespawnOverlay()
    {
        if (respawnLivesText != null && LifeManager.Instance != null)
            respawnLivesText.text = "Lives remaining: " + LifeManager.Instance.Lives;
        if (respawnOverlay != null) respawnOverlay.SetActive(true);
        Tween.ShakePosition(this, Camera.main?.transform, 0.5f, 0.3f);
    }
    void HideRespawnOverlay()
    {
        if (respawnOverlay != null) respawnOverlay.SetActive(false);
    }

    // ── Score popup ────────────────────────────────────────────────────────────
    void SpawnScorePopup(string msg)
    {
        if (canvas == null) return;
        var go = MakeRect("Popup", canvas.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), new Vector2(300f, 50f));
        var txt = go.AddComponent<Text>();
        txt.text      = msg;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = 32;
        txt.fontStyle = FontStyle.Bold;
        txt.color     = new Color(1f, 0.9f, 0.1f);
        txt.alignment = TextAnchor.MiddleCenter;
        // Float upward and fade
        StartCoroutine(PopupRoutine(go, txt));
    }

    private System.Collections.IEnumerator PopupRoutine(GameObject go, Text txt)
    {
        var rt      = go.GetComponent<RectTransform>();
        Vector2 start = rt.anchoredPosition;
        float elapsed = 0f;
        const float dur = 1.0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = elapsed / dur;
            rt.anchoredPosition = start + Vector2.up * (80f * t);
            var c = txt.color; c.a = 1f - t; txt.color = c;
            yield return null;
        }
        Destroy(go);
    }

    // ── Panel show/hide ────────────────────────────────────────────────────────
    void ShowPauseChanged(bool paused)
    {
        if (pausePanel != null) pausePanel.SetActive(paused);
        UpdatePauseButton();
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

        // Star rating based on lives remaining
        int lives = LifeManager.Instance != null ? LifeManager.Instance.Lives : 1;
        int stars = lives >= 3 ? 3 : (lives >= 2 ? 2 : 1);
        for (int i = 0; i < starTexts.Length; i++)
        {
            if (starTexts[i] == null) continue;
            bool earned = i < stars;
            starTexts[i].text  = "*";
            starTexts[i].color = earned ? new Color(1f, 0.85f, 0.1f) : new Color(0.3f, 0.3f, 0.3f);
            float delay = 0.15f + i * 0.2f;
            starTexts[i].transform.localScale = Vector3.zero;
            Tween.Scale(this, starTexts[i].transform, Vector3.one, 0.35f, Tween.Ease.OutBack, delay);
        }

        levelPanel.SetActive(true);
        Tween.PunchScale(this, levelTitle.transform, Vector3.one * 0.16f, 0.35f);
    }

    void ContinueLevel()
    {
        if (levelPanel != null) levelPanel.SetActive(false);
        shownLevelComplete = 0;
        GameStateManager.Instance?.ContinueToNextLevel();
    }

    void ShowGameOver()
    {
        if (gameOverPanel == null) return;
        if (pausePanel != null) pausePanel.SetActive(false);
        gameOverPanel.SetActive(true);

        var summary = gameOverPanel.transform.Find("GOSummary")?.GetComponent<Text>();
        if (summary != null && GameStateManager.Instance != null)
            summary.text = "Level " + GameStateManager.Instance.CurrentLevel + " Score: "
                + Mathf.RoundToInt(GameStateManager.Instance.Score) + " m\nTotal: "
                + Mathf.RoundToInt(GameStateManager.Instance.TotalScore) + " m";

        Tween.Color(this, gameOverPanel.GetComponent<Image>(), PanelBg, 0.5f);
        Tween.ShakePosition(this, Camera.main?.transform, 0.6f, 0.35f);
    }

    void ShowWin()
    {
        if (winPanel == null) return;
        if (pausePanel != null) pausePanel.SetActive(false);
        winPanel.SetActive(true);

        var summary = winPanel.transform.Find("WinSummary")?.GetComponent<Text>();
        if (summary != null && GameStateManager.Instance != null)
            summary.text = "All 4 Levels Cleared!\nFinal Score: "
                + Mathf.RoundToInt(GameStateManager.Instance.CompletedTotalScore) + " m";

        Tween.Color(this, winPanel.GetComponent<Image>(), WinBg, 0.5f);
    }

    void UpdatePauseButton()
    {
        if (pauseButton == null || GameStateManager.Instance == null) return;
        bool canShow = !PlayerNameInput.IsOpen
            && !GameStateManager.Instance.IsGameOver
            && !GameStateManager.Instance.IsWin
            && !GameStateManager.Instance.IsLevelPaused
            && !GameStateManager.Instance.IsRespawning;
        pauseButton.SetActive(canShow);

        // Swap icon: || bars when running, ">" when paused
        bool paused = GameStateManager.Instance.IsPaused;
        if (pauseIconBars   != null) pauseIconBars.SetActive(!paused);
        if (pauseButtonText != null) pauseButtonText.gameObject.SetActive(paused);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Build methods
    // ══════════════════════════════════════════════════════════════════════════

    void BuildScoreLabel(Canvas c)
    {
        var root = MakeRect("ScoreRoot", c.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(240f, 62f));
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
        scoreText = AddLabel(root, "0 m", 36, Color.white, Vector2.zero, new Vector2(240f, 62f));
        root.transform.localScale = Vector3.zero;
        Tween.Scale(this, root.transform, Vector3.one, 0.45f, Tween.Ease.OutBack, delay: 0.2f);
    }

    void BuildPlayerSidebar(Canvas c)
    {
        var panel = MakeRect("PlayerSidebar", c.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(126f, -82f), new Vector2(230f, 92f));
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
        AddLabel(panel, "PLAYER", 18, new Color(1f, 0.85f, 0.1f), new Vector2(0f, 24f), new Vector2(210f, 26f));
        playerNameText  = AddLabel(panel, PlayerNameInput.GetPlayerName(), 26, Color.white, new Vector2(0f, -2f),  new Vector2(210f, 34f));
        levelStatusText = AddLabel(panel, "LEVEL 1", 18, new Color(0.65f, 0.95f, 1f), new Vector2(0f, -30f), new Vector2(210f, 26f));
    }

    void BuildLivesDisplay(Canvas c)
    {
        var panel = MakeRect("LivesPanel", c.transform,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-120f, -140f), new Vector2(130f, 44f));
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        int lives = LifeManager.Instance != null ? LifeManager.Instance.Lives : LifeManager.StartingLives;
        for (int i = 0; i < 3; i++)
        {
            var go = MakeRect("Heart" + i, panel.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(18f + i * 40f, 0f), new Vector2(36f, 36f));
            heartTexts[i]       = go.AddComponent<Text>();
            heartTexts[i].font  = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            heartTexts[i].fontSize  = 28;
            heartTexts[i].fontStyle = FontStyle.Bold;
            heartTexts[i].alignment = TextAnchor.MiddleCenter;
        }
        RefreshLives(lives);
    }

    void BuildFuelBar(Canvas c)
    {
        // Container
        var root = MakeRect("FuelRoot", c.transform,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(110f, 80f), new Vector2(180f, 60f));
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        AddLabel(root, "FUEL", 16, new Color(0.8f, 0.8f, 0.8f), new Vector2(0f, 18f), new Vector2(170f, 22f));

        // Background track
        var track = MakeRect("Track", root.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(150f, 18f));
        track.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);

        // Fill (Filled image, horizontal)
        var fillGo = MakeRect("Fill", track.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fuelFill         = fillGo.AddComponent<Image>();
        fuelFill.color   = FuelGood;
        fuelFill.type    = Image.Type.Filled;
        fuelFill.fillMethod    = Image.FillMethod.Horizontal;
        fuelFill.fillAmount    = 1f;
        fuelFill.sprite  = CreateWhiteSprite();

        fuelWarningText = AddLabel(root, "LOW FUEL!", 14, FuelLow, new Vector2(0f, -24f), new Vector2(170f, 20f));
        fuelWarningText.gameObject.SetActive(false);
    }

    void BuildNitroDisplay(Canvas c)
    {
        var root = MakeRect("NitroRoot", c.transform,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(110f, 150f), new Vector2(180f, 50f));
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
        AddLabel(root, "NITRO", 16, new Color(0.5f, 0.8f, 1f), new Vector2(0f, 14f), new Vector2(170f, 22f));

        for (int i = 0; i < 3; i++)
        {
            var go = MakeRect("N" + i, root.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-40f + i * 40f, -10f), new Vector2(30f, 30f));
            nitroTexts[i]       = go.AddComponent<Text>();
            nitroTexts[i].font  = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nitroTexts[i].text  = "N";
            nitroTexts[i].fontSize  = 22;
            nitroTexts[i].alignment = TextAnchor.MiddleCenter;
            nitroTexts[i].color     = new Color(0.2f, 0.6f, 1f);
        }
    }

    void BuildSpeedometer(Canvas c)
    {
        var root = MakeRect("SpeedRoot", c.transform,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-70f, 80f), new Vector2(110f, 110f));
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        AddLabel(root, "SPD", 14, new Color(0.7f, 0.7f, 0.7f), new Vector2(0f, 38f), new Vector2(100f, 20f));

        // Arc fill as speedometer gauge
        var arcGo  = MakeRect("Arc", root.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(78f, 78f));
        // Background ring
        var bgArc  = arcGo.AddComponent<Image>();
        bgArc.sprite     = CreateWhiteSprite();
        bgArc.color      = new Color(0.15f, 0.15f, 0.15f);
        bgArc.type       = Image.Type.Filled;
        bgArc.fillMethod = Image.FillMethod.Radial360;
        bgArc.fillAmount = 0.75f;
        bgArc.fillOrigin = 2; // Left origin → arc sweeps left to right

        // Foreground fill
        var fillGo = MakeRect("Fill", arcGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        speedArc         = fillGo.AddComponent<Image>();
        speedArc.sprite  = CreateWhiteSprite();
        speedArc.color   = FuelGood;
        speedArc.type    = Image.Type.Filled;
        speedArc.fillMethod = Image.FillMethod.Radial360;
        speedArc.fillAmount = 0f;
        speedArc.fillOrigin = 2;

        // Numeric speed in centre of arc (big number)
        speedValueText = AddLabel(root, "0", 28, FuelGood, new Vector2(0f, -2f), new Vector2(90f, 36f));
        speedValueText.fontStyle = FontStyle.Bold;
        // "km/h" unit below number
        AddLabel(root, "km/h", 11, new Color(0.60f, 0.60f, 0.65f), new Vector2(0f, -26f), new Vector2(90f, 18f));
    }

    void BuildPauseButton(Canvas c)
    {
        // Square button (56×56) — top right corner
        pauseButton = MakeRect("PauseBtn", c.transform,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-44f, -44f), new Vector2(56f, 56f));
        var bg = pauseButton.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.60f);

        var btn = pauseButton.AddComponent<Button>();
        btn.targetGraphic = bg;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.25f, 0.25f, 0.85f);
        colors.pressedColor     = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        btn.colors = colors;
        btn.onClick.AddListener(() => GameStateManager.Instance?.TogglePause());
        var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;

        // ── Pause icon: two white vertical bars (||) ──────────────────────────
        pauseIconBars = new GameObject("PauseIcon");
        pauseIconBars.transform.SetParent(pauseButton.transform, false);
        var rt = pauseIconBars.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        var barL = MakeRect("BarL", pauseIconBars.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-8f, 0f), new Vector2(9f, 26f));
        barL.AddComponent<Image>().color = Color.white;

        var barR = MakeRect("BarR", pauseIconBars.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(8f, 0f), new Vector2(9f, 26f));
        barR.AddComponent<Image>().color = Color.white;

        // ── Resume icon: ">" text (ASCII-safe, universally readable) ──────────
        pauseButtonText = AddLabel(pauseButton, ">", 34, Color.white, Vector2.zero, new Vector2(56f, 56f));
        pauseButtonText.gameObject.SetActive(false);  // hidden until paused

        pauseButton.SetActive(false);
    }

    void BuildPausePanel(Canvas c)
    {
        pausePanel = MakeRect("PausePanel", c.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        pausePanel.AddComponent<Image>().color = PanelBg;
        AddLabel(pausePanel, "PAUSED", 64, new Color(1f, 0.85f, 0.1f), new Vector2(0f, 92f),  new Vector2(760f, 90f));
        AddLabel(pausePanel, "Right/D Accelerate  |  Left/A Reverse  |  SPACE Nitro  |  P Pause", 22, Color.white, new Vector2(0f, 28f), new Vector2(800f, 38f));
        AddActionButton(pausePanel, "ResumeBtn", "RESUME",    new Color(0.1f, 0.65f, 0.18f), new Vector2(0f, -54f),  new Vector2(240f, 62f), () => GameStateManager.Instance?.ResumeGame());
        AddActionButton(pausePanel, "MenuBtn",   "MAIN MENU", new Color(0.22f, 0.22f, 0.55f), new Vector2(0f, -134f), new Vector2(240f, 62f), () => GameStateManager.Instance?.GoToMainMenu());
        pausePanel.SetActive(false);
    }

    void BuildLevelPanel(Canvas c)
    {
        levelPanel = MakeRect("LevelCompletePanel", c.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        levelPanel.AddComponent<Image>().color = new Color(0.03f, 0.05f, 0.12f, 0.88f);

        levelTitle   = AddLabel(levelPanel, "LEVEL 1 COMPLETE", 62, new Color(1f, 0.85f, 0.1f), new Vector2(0f, 140f), new Vector2(900f, 90f));
        levelSummary = AddLabel(levelPanel, "", 28, Color.white, new Vector2(0f, 56f), new Vector2(760f, 88f));

        // Stars row
        for (int i = 0; i < 3; i++)
        {
            float xOff = (i - 1) * 70f;
            var go = MakeRect("Star" + i, levelPanel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(xOff, -26f), new Vector2(60f, 60f));
            starTexts[i]          = go.AddComponent<Text>();
            starTexts[i].font     = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            starTexts[i].fontSize = 48;
            starTexts[i].alignment= TextAnchor.MiddleCenter;
            starTexts[i].text     = "★";
            starTexts[i].color    = new Color(0.3f, 0.3f, 0.3f);
        }

        AddActionButton(levelPanel, "NextLevelBtn", "PLAY LEVEL 2", new Color(0.1f, 0.65f, 0.18f), new Vector2(0f, -120f), new Vector2(280f, 66f), ContinueLevel);
        levelButtonText = levelPanel.transform.Find("NextLevelBtn/Txt")?.GetComponent<Text>();
        levelPanel.SetActive(false);
    }

    void BuildGameOverPanel(Canvas c)
    {
        gameOverPanel = MakeRect("GameOverPanel", c.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        gameOverPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        AddLabel(gameOverPanel, "GAME OVER", 64, new Color(1f, 0.22f, 0.22f), new Vector2(0f, 90f),  new Vector2(800f, 90f));
        AddLabel(gameOverPanel, "", 28, Color.white, new Vector2(0f, 20f), new Vector2(700f, 60f)).gameObject.name = "GOSummary";
        AddActionButton(gameOverPanel, "TryAgainBtn", "TRY AGAIN", new Color(0.18f, 0.55f, 0.18f), new Vector2(0f, -60f),  new Vector2(240f, 62f), () => GameStateManager.Instance?.Restart());
        AddActionButton(gameOverPanel, "MenuBtn",     "MAIN MENU", new Color(0.22f, 0.22f, 0.55f), new Vector2(0f, -140f), new Vector2(240f, 62f), () => GameStateManager.Instance?.GoToMainMenu());
        gameOverPanel.SetActive(false);
    }

    void BuildWinPanel(Canvas c)
    {
        winPanel = MakeRect("WinPanel", c.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        winPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        AddLabel(winPanel, "YOU WIN!", 64, new Color(0.2f, 1f, 0.3f), new Vector2(0f, 110f), new Vector2(900f, 90f));
        AddLabel(winPanel, "", 28, Color.white, new Vector2(0f, 36f), new Vector2(760f, 60f)).gameObject.name = "WinSummary";
        AddActionButton(winPanel, "PlayAgainBtn", "PLAY AGAIN", new Color(0.1f, 0.65f, 0.18f), new Vector2(0f, -50f),  new Vector2(260f, 66f), () => GameStateManager.Instance?.RestartFromLevelOne());
        AddActionButton(winPanel, "MenuBtn",      "MAIN MENU", new Color(0.22f, 0.22f, 0.55f), new Vector2(0f, -134f), new Vector2(260f, 66f), () => GameStateManager.Instance?.GoToMainMenu());
        winPanel.SetActive(false);
    }

    void BuildRespawnOverlay(Canvas c)
    {
        respawnOverlay = MakeRect("RespawnOverlay", c.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        respawnOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        respawnText      = AddLabel(respawnOverlay, "RESPAWNING...", 52, new Color(1f, 0.85f, 0.1f), Vector2.zero,      new Vector2(700f, 80f));
        respawnLivesText = AddLabel(respawnOverlay, "",              28, Color.white,                new Vector2(0f, -64f), new Vector2(500f, 44f));
        respawnOverlay.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static GameObject MakeRect(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin       = anchorMin;
        rt.anchorMax       = anchorMax;
        rt.pivot           = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition= anchoredPos;
        rt.sizeDelta       = sizeDelta;
        return go;
    }

    static Text AddLabel(GameObject parent, string content, int size, Color col, Vector2 pos, Vector2 sizeDelta)
    {
        var go = MakeRect("Label", parent.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, sizeDelta);
        var t = go.AddComponent<Text>();
        t.text      = content;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = size;
        t.fontStyle = FontStyle.Bold;
        t.color     = col;
        t.alignment = TextAnchor.MiddleCenter;
        return t;
    }

    static void AddActionButton(GameObject parent, string goName, string label, Color bgColor,
        Vector2 pos, Vector2 size, System.Action onClick)
    {
        var go  = MakeRect(goName, parent.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());
        var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
        AddLabel(go, label, 30, Color.white, Vector2.zero, size).gameObject.name = "Txt";
    }

    static Sprite CreateWhiteSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
