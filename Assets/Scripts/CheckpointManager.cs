using UnityEngine;

// Tracks the last activated checkpoint so LifeManager knows where to respawn.
// No scene objects needed — purely score-driven thresholds.
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    public Vector2 LastCheckpointPosition { get; private set; }

    // Score thresholds (in metres, matching GameStateManager scoring)
    private static readonly float[] Thresholds = { 250f, 500f, 750f };
    private int nextThresholdIndex = 0;
    private bool hasCheckpoint = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Save the car's starting world position as the default respawn
        if (CarController.Instance != null && CarController.Instance.carRigidbody != null)
            LastCheckpointPosition = CarController.Instance.carRigidbody.position;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnScoreUpdated += OnScoreUpdated;
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnScoreUpdated -= OnScoreUpdated;
    }

    void OnScoreUpdated(float score)
    {
        if (nextThresholdIndex >= Thresholds.Length) return;
        if (score < Thresholds[nextThresholdIndex]) return;

        // Activate checkpoint
        if (CarController.Instance != null && CarController.Instance.carRigidbody != null)
            LastCheckpointPosition = CarController.Instance.carRigidbody.position;

        AudioManager.Instance?.PlayCheckpoint();
        nextThresholdIndex++;
    }
}
