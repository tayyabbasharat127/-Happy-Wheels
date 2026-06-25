using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public Transform carTransform;

    public const float LevelDistance = 1000f;
    public const int  MaxLevel       = 4;

    private static int   sessionLevel = 1;
    private static float sessionTotalScore;

    public float Score                  { get; private set; }
    public float LastCompletedLevelScore{ get; private set; }
    public float TotalScore             => sessionTotalScore + Score;
    public float CompletedTotalScore    => sessionTotalScore;
    public int   CurrentLevel           { get; private set; }
    public bool  IsLevelPaused          { get; private set; }
    public bool  IsPaused               { get; private set; }
    public bool  IsGameOver             { get; private set; }
    public bool  IsWin                  { get; private set; }
    public bool  IsRespawning           { get; set; }

    public event Action<float> OnScoreUpdated;
    public event Action<int>   OnLevelComplete;
    public event Action<bool>  OnPauseChanged;
    public event Action        OnGameOver;
    public event Action        OnWin;

    private float upsideDownTimer;
    private float flipCooldown;
    private bool  countedThisFlip;
    private bool  levelCompleted;

    private const float UpsideDownAngleMin  = 100f;
    private const float UpsideDownAngleMax  = 260f;
    private const float FlipConfirmSeconds  = 0.8f;
    private const float FlipCooldownSeconds = 4f;

    private float startX;

    public static void ResetRunProgress()
    {
        sessionLevel      = 1;
        sessionTotalScore = 0f;
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance     = this;
        CurrentLevel = Mathf.Clamp(sessionLevel, 1, MaxLevel);

        // Auto-create any managers not already in the scene.
        // Awake() runs before any Start(), so these will be fully initialized
        // by the time the rest of the scene's Start() methods execute.
        EnsureManager<LifeManager>();
        EnsureManager<CheckpointManager>();
        EnsureManager<LevelThemeManager>();
        EnsureManager<ParallaxBackground>();
        EnsureManager<LevelObstacleManager>();
    }

    static void EnsureManager<T>() where T : MonoBehaviour
    {
        if (FindAnyObjectByType<T>(FindObjectsInactive.Include) == null)
            new GameObject(typeof(T).Name).AddComponent<T>();
    }

    void Start()
    {
        Time.timeScale = 1f;

        if (carTransform == null)
        {
            var cc = FindAnyObjectByType<CarController>();
            if (cc != null)
                carTransform = cc.carRigidbody != null ? cc.carRigidbody.transform : cc.transform;
        }

        if (carTransform != null) startX = carTransform.position.x;
        AudioManager.Instance?.StartEngine();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            TogglePause();

        if (IsGameOver || IsWin || IsLevelPaused || IsPaused || levelCompleted || IsRespawning || carTransform == null) return;

        UpdateScore();
        DetectFlip();
        CheckLevelProgress();
    }

    void UpdateScore()
    {
        float dist = Mathf.Max(0f, carTransform.position.x - startX) * 5f;
        if (dist > Score)
        {
            Score = dist;
            OnScoreUpdated?.Invoke(Score);
        }
    }

    void DetectFlip()
    {
        if (flipCooldown > 0f) { flipCooldown -= Time.deltaTime; return; }

        float angle      = carTransform.eulerAngles.z;
        bool  upsideDown = angle > UpsideDownAngleMin && angle < UpsideDownAngleMax;

        if (upsideDown)
        {
            upsideDownTimer += Time.deltaTime;
            if (upsideDownTimer >= FlipConfirmSeconds && !countedThisFlip)
            {
                countedThisFlip = true;
                flipCooldown    = FlipCooldownSeconds;
                TriggerDeath();
            }
        }
        else
        {
            upsideDownTimer = 0f;
            countedThisFlip = false;
        }
    }

    void CheckLevelProgress()
    {
        if (Score < LevelDistance) return;
        CompleteCurrentLevel();
    }

    // Called by HeadScript, DetectFlip, fuel-out — goes through life system
    public void TriggerDeath()
    {
        if (IsGameOver || IsWin || IsRespawning) return;

        if (LifeManager.Instance != null)
            LifeManager.Instance.LoseLife();
        else
            TriggerGameOver();
    }

    public void CompleteCurrentLevel()
    {
        if (levelCompleted) return;
        levelCompleted          = true;
        LastCompletedLevelScore = Score;
        sessionTotalScore      += Score;

        AudioManager.Instance?.SetEngineThrottle(0f);
        AudioManager.Instance?.SetLowFuelWarning(false);

        if (CurrentLevel >= MaxLevel)
        {
            TriggerWin();
            return;
        }

        IsLevelPaused = true;
        Time.timeScale = 0f;
        OnLevelComplete?.Invoke(CurrentLevel);
    }

    public void ContinueToNextLevel()
    {
        if (!IsLevelPaused) return;
        sessionLevel  = Mathf.Clamp(CurrentLevel + 1, 1, MaxLevel);
        IsLevelPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(1);
    }

    public void TogglePause()
    {
        if (IsPaused) ResumeGame();
        else          PauseGame();
    }

    public void PauseGame()
    {
        if (PlayerNameInput.IsOpen || IsGameOver || IsWin || IsLevelPaused || levelCompleted || IsPaused || IsRespawning) return;
        IsPaused       = true;
        Time.timeScale = 0f;
        AudioManager.Instance?.SetEngineThrottle(0f);
        OnPauseChanged?.Invoke(true);
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;
        IsPaused       = false;
        Time.timeScale = 1f;
        OnPauseChanged?.Invoke(false);
    }

    public void TriggerGameOver()
    {
        if (IsGameOver || IsWin) return;
        IsPaused       = false;
        IsLevelPaused  = false;
        IsRespawning   = false;
        Time.timeScale = 1f;
        IsGameOver     = true;
        AudioManager.Instance?.SetLowFuelWarning(false);
        AudioManager.Instance?.StopEngine();
        AudioManager.Instance?.PlayCrash();
        OnGameOver?.Invoke();
    }

    public void TriggerWin()
    {
        if (IsWin || IsGameOver) return;
        IsPaused       = false;
        IsLevelPaused  = false;
        IsRespawning   = false;
        Time.timeScale = 1f;
        IsWin          = true;
        AudioManager.Instance?.SetLowFuelWarning(false);
        AudioManager.Instance?.StopEngine();
        AudioManager.Instance?.PlayWin();
        OnWin?.Invoke();
    }

    // Called by LifeManager after respawn completes — resets flip state so detection restarts clean
    public void ResetFlipState()
    {
        upsideDownTimer = 0f;
        countedThisFlip = false;
        flipCooldown    = 1f; // brief grace after respawn
    }

    // "Try Again" and "Play Again" — both go to level 1 with fresh lives
    public void Restart() => RestartFromLevelOne();

    public void RestartFromLevelOne()
    {
        ResetRunProgress();
        LifeManager.ResetLives();
        Time.timeScale = 1f;
        SceneManager.LoadScene(1);
    }

    public void GoToMainMenu()
    {
        ResetRunProgress();
        LifeManager.ResetLives();
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
