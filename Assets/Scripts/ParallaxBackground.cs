using UnityEngine;

// Creates three solid-colour background layers that scroll at different
// parallax speeds relative to the camera.  Works without any art assets.
// Attach to any GameObject in the gameplay scene.
//
// Optional: assign Sprite references in the Inspector to replace the solid
// colour quads with actual art once assets are available.
[DefaultExecutionOrder(-10)]
public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public struct Layer
    {
        public Sprite sprite;   // optional — solid colour used if null
        public Color  color;
        [Range(0f, 1f)] public float parallaxFactor; // 0 = static, 1 = moves with camera
        public float yPosition;
        public float scaleY;
    }

    public Layer[] layers = new Layer[]
    {
        // Sky — barely moves
        new Layer { color = new Color(0.53f, 0.81f, 0.92f, 1f), parallaxFactor = 0.05f, yPosition = 10f, scaleY = 30f },
        // Distant hills — moves slowly
        new Layer { color = new Color(0.35f, 0.60f, 0.35f, 0.9f), parallaxFactor = 0.25f, yPosition = 0f,  scaleY = 14f },
        // Near hills — moves at medium speed
        new Layer { color = new Color(0.22f, 0.48f, 0.22f, 0.8f), parallaxFactor = 0.55f, yPosition = -5f, scaleY = 10f },
    };

    private Transform[] layerTransforms;
    private Vector3     lastCamPos;
    private Camera      cam;

    void Start()
    {
        cam         = Camera.main;
        lastCamPos  = cam != null ? cam.transform.position : Vector3.zero;
        layerTransforms = new Transform[layers.Length];

        for (int i = 0; i < layers.Length; i++)
        {
            var go = new GameObject("ParallaxLayer_" + i);
            go.transform.SetParent(transform);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -100 + i;

            if (layers[i].sprite != null)
            {
                sr.sprite = layers[i].sprite;
            }
            else
            {
                // Create a 1×1 white sprite and tint it
                sr.sprite = CreateWhiteSquareSprite();
                sr.color  = layers[i].color;
            }

            // Make it wide enough to always fill the screen
            float width = cam != null ? cam.orthographicSize * cam.aspect * 6f : 60f;
            go.transform.localScale = new Vector3(width, layers[i].scaleY, 1f);

            layerTransforms[i] = go.transform;
            UpdateLayerPosition(i, cam != null ? cam.transform.position : Vector3.zero);
        }

        // Let LevelThemeManager update sky colour, then sync layer 0
        ApplyThemeColors();
    }

    void LateUpdate()
    {
        if (cam == null) return;
        Vector3 delta = cam.transform.position - lastCamPos;
        for (int i = 0; i < layerTransforms.Length; i++)
            UpdateLayerPosition(i, cam.transform.position);
        lastCamPos = cam.transform.position;
    }

    private void UpdateLayerPosition(int i, Vector3 camPos)
    {
        if (layerTransforms[i] == null) return;
        float x = camPos.x * layers[i].parallaxFactor;
        float y = layers[i].yPosition + camPos.y * layers[i].parallaxFactor * 0.3f;
        layerTransforms[i].position = new Vector3(x, y, 10f + i);
    }

    // Sync colours with level theme after LevelThemeManager sets the sky
    private void ApplyThemeColors()
    {
        int level = GameStateManager.Instance != null ? GameStateManager.Instance.CurrentLevel : 1;

        Color[][] levelPalettes =
        {
            // Level 1 — Meadows
            new[]{ new Color(0.53f,0.81f,0.92f), new Color(0.30f,0.58f,0.28f), new Color(0.18f,0.42f,0.16f) },
            // Level 2 — Rocky Pass
            new[]{ new Color(0.60f,0.35f,0.10f), new Color(0.45f,0.35f,0.20f), new Color(0.28f,0.22f,0.12f) },
            // Level 3 — Desert
            new[]{ new Color(0.78f,0.55f,0.20f), new Color(0.65f,0.48f,0.20f), new Color(0.50f,0.35f,0.15f) },
            // Level 4 — Volcano
            new[]{ new Color(0.06f,0.02f,0.04f), new Color(0.18f,0.04f,0.02f), new Color(0.28f,0.06f,0.02f) },
        };

        int idx = Mathf.Clamp(level - 1, 0, levelPalettes.Length - 1);
        for (int i = 0; i < Mathf.Min(layerTransforms.Length, levelPalettes[idx].Length); i++)
        {
            var sr = layerTransforms[i]?.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = levelPalettes[idx][i];
        }
    }

    private static Sprite CreateWhiteSquareSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
