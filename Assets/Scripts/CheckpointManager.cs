using UnityEngine;

// Tracks the last activated checkpoint so LifeManager knows where to respawn.
// No scene objects needed — purely score-driven thresholds.
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    public Vector2 LastCheckpointPosition { get; private set; }
    public float LastCheckpointAngle { get; private set; }

    // Score thresholds (in metres, matching GameStateManager scoring)
    private static readonly float[] Thresholds = { 250f, 500f, 750f };
    private const float RayStartHeight = 22f;
    private const float RayDistance = 70f;
    private const float SpawnBodyHeight = 1.75f;
    private const float SpawnClearance = 0.45f;
    private const float SpawnBoxWidth = 4.8f;
    private const float SpawnBoxHeight = 3.0f;

    private readonly SafeCheckpoint[] checkpoints = new SafeCheckpoint[Thresholds.Length + 1];
    private readonly Collider2D[] overlapHits = new Collider2D[16];
    private int checkpointCount;
    private int nextThresholdIndex = 0;

    struct SafeCheckpoint
    {
        public Vector2 bodyPosition;
        public float bodyAngle;
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Save the car's starting world position as the default respawn
        if (CarController.Instance != null && CarController.Instance.carRigidbody != null)
        {
            Vector2 carPos = CarController.Instance.carRigidbody.position;
            SaveCheckpointNear(carPos.x, carPos, checkpointCount == 0);
        }

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
        {
            Vector2 carPos = CarController.Instance.carRigidbody.position;
            SaveCheckpointNear(carPos.x, carPos, true);
        }

        AudioManager.Instance?.PlayCheckpoint();
        nextThresholdIndex++;
    }

    public void RefreshCheckpointAtCar()
    {
        if (CarController.Instance == null || CarController.Instance.carRigidbody == null) return;

        Vector2 carPos = CarController.Instance.carRigidbody.position;
        SaveCheckpointNear(carPos.x, carPos, checkpointCount == 0);
    }

    public bool TryGetSafeRespawn(out Vector2 position, out float angle)
    {
        for (int i = checkpointCount - 1; i >= 0; i--)
        {
            SafeCheckpoint checkpoint = checkpoints[i];
            if (IsSpawnAreaClear(checkpoint.bodyPosition, checkpoint.bodyAngle, null))
            {
                position = checkpoint.bodyPosition;
                angle = checkpoint.bodyAngle;
                LastCheckpointPosition = position;
                LastCheckpointAngle = angle;
                return true;
            }
        }

        position = LastCheckpointPosition;
        angle = LastCheckpointAngle;
        return checkpointCount > 0;
    }

    void SaveCheckpointNear(float worldX, Vector2 fallbackPosition, bool append)
    {
        SafeCheckpoint checkpoint;
        if (!TryBuildCheckpoint(worldX, out checkpoint))
        {
            if (checkpointCount > 0)
                return;

            LastCheckpointPosition = fallbackPosition;
            LastCheckpointAngle = 0f;
            checkpoint.bodyPosition = fallbackPosition + Vector2.up * SpawnBodyHeight;
            checkpoint.bodyAngle = 0f;
        }

        LastCheckpointPosition = checkpoint.bodyPosition;
        LastCheckpointAngle = checkpoint.bodyAngle;

        int index = append ? Mathf.Min(checkpointCount, checkpoints.Length - 1) : Mathf.Max(checkpointCount - 1, 0);
        checkpoints[index] = checkpoint;
        if (append || checkpointCount == 0)
            checkpointCount = Mathf.Min(checkpointCount + 1, checkpoints.Length);
    }

    bool TryBuildCheckpoint(float worldX, out SafeCheckpoint checkpoint)
    {
        float[] offsets = { 0f, -4f, 4f, -8f, 8f, -14f, 14f };
        for (int i = 0; i < offsets.Length; i++)
        {
            if (TryBuildCheckpointAtX(worldX + offsets[i], out checkpoint))
                return true;
        }

        checkpoint = new SafeCheckpoint();
        return false;
    }

    bool TryBuildCheckpointAtX(float worldX, out SafeCheckpoint checkpoint)
    {
        checkpoint = new SafeCheckpoint();

        RaycastHit2D hit;
        if (!TryFindGroundBelow(worldX, out hit))
            return false;

        Vector2 normal = hit.normal.sqrMagnitude > 0.001f ? hit.normal.normalized : Vector2.up;
        Vector2 tangent = new Vector2(normal.y, -normal.x);
        if (tangent.x < 0f) tangent = -tangent;

        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
        Vector2 bodyPosition = hit.point + normal * SpawnBodyHeight + Vector2.up * SpawnClearance;
        if (!IsSpawnAreaClear(bodyPosition, angle, hit.collider))
            return false;

        checkpoint.bodyPosition = bodyPosition;
        checkpoint.bodyAngle = angle;
        return true;
    }

    bool TryFindGroundBelow(float worldX, out RaycastHit2D groundHit)
    {
        Vector2 rayStart = new Vector2(worldX, RayStartHeight);
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, Vector2.down, RayDistance);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider != null && hit.collider.CompareTag("Ground"))
            {
                groundHit = hit;
                return true;
            }
        }

        groundHit = new RaycastHit2D();
        return false;
    }

    bool IsSpawnAreaClear(Vector2 bodyPosition, float angle, Collider2D groundCollider)
    {
        int count = Physics2D.OverlapBoxNonAlloc(bodyPosition, new Vector2(SpawnBoxWidth, SpawnBoxHeight), angle, overlapHits);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = overlapHits[i];
            if (hit == null || hit == groundCollider) continue;

            Rigidbody2D attached = hit.attachedRigidbody;
            CarController car = CarController.Instance;
            if (car != null && car.carRigidbody != null && attached != null
                && (attached == car.carRigidbody
                    || attached == car.backTire
                    || attached == car.frontTire
                    || attached.transform.IsChildOf(car.carRigidbody.transform)))
                continue;

            if (hit.CompareTag("Ground")) continue;

            return false;
        }

        return true;
    }
}
