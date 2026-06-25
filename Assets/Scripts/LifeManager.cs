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

    private bool respawnRunning;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public static void ResetLives() => lives = StartingLives;

    public void LoseLife()
    {
        if (respawnRunning || GameStateManager.Instance == null || GameStateManager.Instance.IsRespawning) return;
        if (lives <= 0) return;

        respawnRunning = true;
        GameStateManager.Instance.IsRespawning = true;

        CarController.Instance?.BeginRespawnFreeze();

        lives--;
        OnLifeChanged?.Invoke(lives);
        AudioManager.Instance?.PlayLifeLost();

        if (lives <= 0)
        {
            respawnRunning = false;
            GameStateManager.Instance.IsRespawning = false;
            GameStateManager.Instance?.TriggerGameOver();
        }
        else
        {
            StartCoroutine(RespawnSequence());
        }
    }

    private IEnumerator RespawnSequence()
    {
        AudioManager.Instance?.SetLowFuelWarning(false);
        AudioManager.Instance?.SetEngineThrottle(0f);

        OnRespawnStart?.Invoke();

        // Brief freeze so player reads the "RESPAWNING" overlay
        yield return new WaitForSecondsRealtime(1.6f);

        Vector2 respawnPos = Vector2.zero;
        float respawnAngle = 0f;
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.TryGetSafeRespawn(out respawnPos, out respawnAngle);

        CarController car = CarController.Instance;
        if (car != null)
        {
            car.RespawnAt(respawnPos, respawnAngle, true);
            ResetCameraToCar(car);
        }

        // Let the new pose settle while the rigidbodies are frozen.
        yield return new WaitForSecondsRealtime(0.25f);

        if (car != null)
            car.EndRespawnFreeze();

        yield return new WaitForSecondsRealtime(0.15f);

        GameStateManager.Instance.IsRespawning = false;
        GameStateManager.Instance.ResetFlipState();
        AudioManager.Instance?.StartEngine();

        OnRespawnEnd?.Invoke();
        respawnRunning = false;
    }

    void ResetCameraToCar(CarController car)
    {
        if (car == null || car.carRigidbody == null) return;

        Follow[] cameras = FindObjectsOfType<Follow>();
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
                cameras[i].ResetTarget(car.carRigidbody.transform, true);
        }
    }
}
