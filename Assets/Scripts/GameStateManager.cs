using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public Transform carTransform;

    public const float LevelDistance = 1000f;
    public const int MaxLevel = 4;

    private static int sessionLevel = 1;
    private static float sessionTotalScore;

    public float Score { get; private set; }
    public float LastCompletedLevelScore { get; private set; }
    public float TotalScore => sessionTotalScore + Score;
    public float CompletedTotalScore => sessionTotalScore;
    public int CurrentLevel { get; private set; }
    public bool IsLevelPaused { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsWin { get; private set; }

    public event Action<float> OnScoreUpdated;
    public event Action<int> OnLevelComplete;
    public event Action OnGameOver;
    public event Action OnWin;

    private float upsideDownTimer;
    private float flipCooldown;
    private bool countedThisFlip;
    private bool levelCompleted;

    private const float UpsideDownAngleMin = 100f;
    private const float UpsideDownAngleMax = 260f;
    private const float FlipConfirmSeconds = 0.8f;
    private const float FlipCooldownSeconds = 4f;

    private float startX;

    public static void ResetRunProgress()
    {
        sessionLevel = 1;
        sessionTotalScore = 0f;
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        CurrentLevel = Mathf.Clamp(sessionLevel, 1, MaxLevel);
    }

    void Start()
    {
        Time.timeScale = 1f;

        if (carTransform == null)
        {
            var cc = FindAnyObjectByType<CarController>();
            if (cc != null)
                carTransform = (cc.carRigidbody != null) ? cc.carRigidbody.transform : cc.transform;
        }

        if (carTransform != null)
            startX = carTransform.position.x;

        if (AudioManager.Instance != null)
            AudioManager.Instance.StartEngine();
    }

    void Update()
    {
        if (IsGameOver || IsWin || IsLevelPaused || levelCompleted || carTransform == null) return;
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

        float angle = carTransform.eulerAngles.z;
        bool upsideDown = angle > UpsideDownAngleMin && angle < UpsideDownAngleMax;

        if (upsideDown)
        {
            upsideDownTimer += Time.deltaTime;
            if (upsideDownTimer >= FlipConfirmSeconds && !countedThisFlip)
            {
                countedThisFlip = true;
                TriggerGameOver();
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

    public void CompleteCurrentLevel()
    {
        if (levelCompleted) return;
        levelCompleted = true;
        LastCompletedLevelScore = Score;
        sessionTotalScore += Score;

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetEngineThrottle(0f);

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
        sessionLevel = Mathf.Clamp(CurrentLevel + 1, 1, MaxLevel);
        IsLevelPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(1);
    }

    public void TriggerGameOver()
    {
        if (IsGameOver || IsWin) return;
        IsLevelPaused = false;
        Time.timeScale = 1f;
        IsGameOver = true;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopEngine();
            AudioManager.Instance.PlayCrash();
        }
        OnGameOver?.Invoke();
    }

    public void TriggerWin()
    {
        if (IsWin || IsGameOver) return;
        IsLevelPaused = false;
        Time.timeScale = 1f;
        IsWin = true;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopEngine();
            AudioManager.Instance.PlayWin();
        }
        OnWin?.Invoke();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(1);
    }

    public void RestartFromLevelOne()
    {
        ResetRunProgress();
        Time.timeScale = 1f;
        SceneManager.LoadScene(1);
    }

    public void GoToMainMenu()
    {
        ResetRunProgress();
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
