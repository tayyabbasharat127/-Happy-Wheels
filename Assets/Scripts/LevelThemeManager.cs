using UnityEngine;

// Add to any GameObject in the gameplay scene.
// Reads CurrentLevel and sets camera background colour + ambient light to
// match each level's theme. No external sprites required.
public class LevelThemeManager : MonoBehaviour
{
    private static readonly Color[] SkyColors =
    {
        new Color(0.53f, 0.81f, 0.92f),  // Level 1 — sky blue  (Meadows)
        new Color(0.60f, 0.35f, 0.10f),  // Level 2 — amber     (Rocky Pass)
        new Color(0.78f, 0.44f, 0.20f),  // Level 3 — deep orange (Desert)
        new Color(0.06f, 0.02f, 0.04f),  // Level 4 — near black (Volcano)
    };

    private static readonly Color[] AmbientColors =
    {
        new Color(1.00f, 1.00f, 0.90f),  // warm white
        new Color(1.00f, 0.72f, 0.40f),  // amber
        new Color(1.00f, 0.65f, 0.30f),  // orange
        new Color(0.60f, 0.15f, 0.05f),  // lava red
    };

    void Start()
    {
        int level = GameStateManager.Instance != null ? GameStateManager.Instance.CurrentLevel : 1;
        int idx   = Mathf.Clamp(level - 1, 0, SkyColors.Length - 1);

        Camera cam = Camera.main;
        if (cam != null) cam.backgroundColor = SkyColors[idx];

        RenderSettings.ambientLight = AmbientColors[idx];
    }
}
