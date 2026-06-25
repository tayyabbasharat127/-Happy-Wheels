using System;
using System.Collections;
using UnityEngine;

public class LifeManager : MonoBehaviour
{
    public static LifeManager Instance { get; private set; }

    public const int StartingLives = 3;

    private static int lives = StartingLives;

    public int Lives => lives;

    public event Action<int> OnLifeChanged;   // fires with new life count
    public event Action      OnRespawnStart;  // UIManager shows overlay
    public event Action      OnRespawnEnd;    // UIManager hides overlay

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public static void ResetLives() => lives = StartingLives;

    public void LoseLife()
    {
        if (lives <= 0) return;
        lives--;
        OnLifeChanged?.Invoke(lives);
        AudioManager.Instance?.PlayLifeLost();

        if (lives <= 0)
        {
            GameStateManager.Instance?.TriggerGameOver();
        }
        else
        {
            StartCoroutine(RespawnSequence());
        }
    }

    private IEnumerator RespawnSequence()
    {
        GameStateManager.Instance.IsRespawning = true;
        AudioManager.Instance?.SetLowFuelWarning(false);
        AudioManager.Instance?.SetEngineThrottle(0f);

        OnRespawnStart?.Invoke();

        // Brief freeze so player reads the "RESPAWNING" overlay
        yield return new WaitForSecondsRealtime(1.6f);

        // Teleport car to last checkpoint (or start) — runs in real time
        Vector2 respawnPos = CheckpointManager.Instance != null
            ? CheckpointManager.Instance.LastCheckpointPosition
            : Vector2.zero;

        CarController car = CarController.Instance;
        if (car != null) car.RespawnAt(respawnPos);

        // Wait one physics frame for teleport to settle before unfreezing
        yield return new WaitForSecondsRealtime(0.1f);

        GameStateManager.Instance.IsRespawning = false;
        GameStateManager.Instance.ResetFlipState();
        AudioManager.Instance?.StartEngine();

        OnRespawnEnd?.Invoke();
    }
}
