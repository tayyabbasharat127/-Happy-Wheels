using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Makes existing SpriteShape ground rougher as levels increase.
/// Keeps colliders tagged as Ground so car grounding, checkpoints, and scoring stay unchanged.
/// </summary>
public class TerrainDifficultyManager : MonoBehaviour
{
    private const float BottomPointY = -4f;
    private const float SpawnSafeDistance = 16f;
    private const float BlendDistance = 18f;

    void Start()
    {
        if (GameStateManager.Instance == null) return;

        int level = GameStateManager.Instance.CurrentLevel;
        if (level <= 1) return;

        float startX = 0f;
        if (CarController.Instance != null && CarController.Instance.carRigidbody != null)
            startX = CarController.Instance.carRigidbody.position.x;

        ApplyTerrainProfile(level, startX);
        CheckpointManager.Instance?.RefreshCheckpointAtCar();
    }

    void ApplyTerrainProfile(int level, float startX)
    {
        SpriteShapeController[] shapes = FindObjectsOfType<SpriteShapeController>();

        float hillAmplitude = 0.35f + (level - 1) * 0.35f;
        float bumpAmplitude = 0.10f + (level - 1) * 0.16f;
        float frequency = 0.13f + level * 0.025f;
        float bumpFrequency = 0.58f + level * 0.08f;

        for (int i = 0; i < shapes.Length; i++)
        {
            SpriteShapeController shape = shapes[i];
            if (shape == null || !shape.CompareTag("Ground")) continue;

            RoughenShape(shape, level, startX, hillAmplitude, bumpAmplitude, frequency, bumpFrequency);
            shape.RefreshSpriteShape();
            shape.BakeCollider();
        }
    }

    void RoughenShape(
        SpriteShapeController shape,
        int level,
        float startX,
        float hillAmplitude,
        float bumpAmplitude,
        float frequency,
        float bumpFrequency)
    {
        Spline spline = shape.spline;
        int count = spline.GetPointCount();

        for (int i = 0; i < count; i++)
        {
            Vector3 point = spline.GetPosition(i);
            if (point.y < BottomPointY) continue;

            float worldX = shape.transform.TransformPoint(point).x;
            float spawnBlend = Mathf.Clamp01((worldX - startX - SpawnSafeDistance) / BlendDistance);
            if (spawnBlend <= 0f) continue;

            float hill = Mathf.Sin(worldX * frequency + level * 1.7f) * hillAmplitude;
            float bump = Mathf.Sin(worldX * bumpFrequency + level * 2.9f) * bumpAmplitude;
            float crest = Mathf.Sin(worldX * (frequency * 0.47f) + level) * hillAmplitude * 0.55f;
            float offset = (hill + bump + crest) * spawnBlend;

            point.y = Mathf.Clamp(point.y + offset, -2.8f, 7.7f);
            spline.SetPosition(i, point);

            float tangentScale = 1f + (level - 1) * 0.12f;
            spline.SetLeftTangent(i, spline.GetLeftTangent(i) * tangentScale);
            spline.SetRightTangent(i, spline.GetRightTangent(i) * tangentScale);
        }
    }
}
