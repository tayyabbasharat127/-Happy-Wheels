using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns level-specific obstacles in code — no prefabs required.
/// Registered via EnsureManager in GameStateManager.Awake().
/// Level 1 = no obstacles (tutorial). Levels 2-4 scale up in count and danger.
/// </summary>
public class LevelObstacleManager : MonoBehaviour
{
    public static LevelObstacleManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (GameStateManager.Instance == null || CarController.Instance == null) return;

        int   level  = GameStateManager.Instance.CurrentLevel;
        float startX = CarController.Instance.carRigidbody.position.x;

        switch (level)
        {
            case 2: StartCoroutine(SpawnLevel2(startX)); break;
            case 3: StartCoroutine(SpawnLevel3(startX)); break;
            case 4: StartCoroutine(SpawnLevel4(startX)); break;
            // Level 1: tutorial — no obstacles
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Level layouts
    // ══════════════════════════════════════════════════════════════════════════

    // Level 2 — 4 obstacles: intro to hazards
    IEnumerator SpawnLevel2(float sx)
    {
        yield return null; // wait one frame so physics is fully initialised

        Rock(sx + 42f,  2.2f, 2.0f);          // small rock, early
        Boulder(sx + 82f,  18f, 1.4f, -1.5f); // medium boulder, drops from height
        Rock(sx + 128f, 2.5f, 2.5f);          // medium rock mid-level
        Barrier(sx + 165f, 2f,  4.5f, 0.9f);  // slow moving red barrier near end
    }

    // Level 3 — 7 obstacles: harder, tighter spacing
    IEnumerator SpawnLevel3(float sx)
    {
        yield return null;

        Rock(sx + 28f,  2.0f, 2.2f);
        Boulder(sx + 55f,  20f, 1.6f, -2f);
        Rock(sx + 82f,  2.5f, 3.0f);           // tall rock — need speed to clear
        Boulder(sx + 108f, 18f, 1.8f, -1.8f);
        Barrier(sx + 138f, 2f,  5.2f, 1.3f);   // faster barrier
        Rock(sx + 162f, 2.5f, 2.8f);
        Boulder(sx + 185f, 20f, 2.0f, -2.5f);  // big boulder near finish
    }

    // Level 4 — 9 obstacles: brutal. Spike kill-zones + boulder combos
    IEnumerator SpawnLevel4(float sx)
    {
        yield return null;

        Boulder(sx + 22f,  22f, 2.0f, -2f);
        Rock(sx + 42f,    2.5f, 2.5f);
        Spikes(sx + 65f,  0f,   2.5f, 1.2f);   // first spike zone — instant death
        Boulder(sx + 85f,  22f, 2.2f, -3f);    // heavy, fast-dropping
        Rock(sx + 105f,   2.5f, 3.5f);          // very tall blocker
        Barrier(sx + 128f, 2f,  6.0f, 1.8f);   // fast, tall barrier
        Boulder(sx + 152f, 22f, 2.0f, -2.5f);
        Spikes(sx + 170f,  0f,  2.5f, 1.2f);   // second spike zone
        Rock(sx + 185f,   2.5f, 3.0f);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Obstacle spawners
    // ══════════════════════════════════════════════════════════════════════════

    // Static rock — brown box, blocks path
    // x      = world X centre
    // height = visual height (units) — also collider half-height above spawn Y
    // width  = visual width (units)
    void Rock(float x, float height, float width)
    {
        var go = new GameObject("Rock");
        go.transform.position = new Vector3(x, height * 0.5f + 0.2f, 0f);
        go.transform.localScale = new Vector3(width, height, 1f);

        // Physics — no Rigidbody2D → static collider
        var col = go.AddComponent<BoxCollider2D>();
        col.sharedMaterial = HardMat();

        // Main rock body
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = BoxSprite();
        sr.color  = new Color(0.35f, 0.26f, 0.18f);
        sr.sortingOrder = 3;

        // Surface crack detail
        var crack = Child(go.transform, "Crack", new Vector3(0.08f, 0.12f, -0.1f),
                          new Vector3(0.65f, 0.55f, 1f));
        crack.AddComponent<SpriteRenderer>().color = new Color(0.22f, 0.15f, 0.09f, 0.70f);
        crack.GetComponent<SpriteRenderer>().sprite = BoxSprite();
        crack.GetComponent<SpriteRenderer>().sortingOrder = 4;

        // Dark top-edge highlight
        var top = Child(go.transform, "Top", new Vector3(0f, 0.46f, -0.05f),
                        new Vector3(1f, 0.06f, 1f));
        top.AddComponent<SpriteRenderer>().color = new Color(0.50f, 0.40f, 0.28f, 0.80f);
        top.GetComponent<SpriteRenderer>().sprite = BoxSprite();
        top.GetComponent<SpriteRenderer>().sortingOrder = 4;
    }

    // Falling boulder — drops from height, rolls and crushes
    // x      = world X centre
    // spawnY = initial height (falls due to gravity)
    // radius = collision/visual radius in game units
    // hVel   = initial horizontal velocity (negative = leftward, toward car)
    void Boulder(float x, float spawnY, float radius, float hVel)
    {
        var go = new GameObject("Boulder");
        go.transform.position   = new Vector3(x, spawnY, 0f);
        go.transform.localScale = Vector3.one * radius * 2f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass           = 35f;
        rb.linearDamping  = 0.4f;
        rb.angularDamping = 0.25f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation  = RigidbodyInterpolation2D.Interpolate;
        // Initial nudge toward player
        rb.linearVelocity = new Vector2(hVel, 0f);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius         = 0.5f; // × scale = radius units actual
        col.sharedMaterial = BoulderMat();

        // Visual: dark grey circle
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CircleSprite();
        sr.color  = new Color(0.32f, 0.30f, 0.26f);
        sr.sortingOrder = 3;

        // Highlight spot — makes it look 3D
        var spot = Child(go.transform, "Spot", new Vector3(-0.22f, 0.22f, -0.1f),
                         Vector3.one * 0.30f);
        var ssl  = spot.AddComponent<SpriteRenderer>();
        ssl.sprite = CircleSprite();
        ssl.color  = new Color(0.58f, 0.56f, 0.52f, 0.60f);
        ssl.sortingOrder = 4;
    }

    // Kinematic moving barrier — oscillates up/down, dangerous red
    // x         = world X
    // yBase     = lowest Y position
    // height    = barrier height in game units
    // speed     = oscillation speed (radians/second)
    void Barrier(float x, float yBase, float height, float speed)
    {
        var go = new GameObject("Barrier");
        go.transform.position   = new Vector3(x, yBase + height * 0.5f, 0f);
        go.transform.localScale = new Vector3(0.65f, height, 1f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType    = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<BoxCollider2D>();
        col.sharedMaterial = HardMat();

        // Main barrier body — danger red
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = BoxSprite();
        sr.color  = new Color(0.80f, 0.10f, 0.05f, 0.95f);
        sr.sortingOrder = 3;

        // Warning stripes (3 yellow bands)
        for (int i = 0; i < 4; i++)
        {
            float yOff = -0.40f + i * 0.26f;
            var stripe = Child(go.transform, "S" + i,
                new Vector3(0f, yOff, -0.05f), new Vector3(1.05f, 0.10f, 1f));
            stripe.AddComponent<SpriteRenderer>().color = new Color(0.95f, 0.80f, 0.05f, 0.90f);
            stripe.GetComponent<SpriteRenderer>().sprite = BoxSprite();
            stripe.GetComponent<SpriteRenderer>().sortingOrder = 4;
        }

        // Top spike (visual indicator of danger)
        var tip = Child(go.transform, "Tip",
            new Vector3(0f, 0.52f, -0.05f), new Vector3(0.5f, 0.08f, 1f));
        tip.AddComponent<SpriteRenderer>().color = new Color(1f, 0.3f, 0.1f);
        tip.GetComponent<SpriteRenderer>().sprite = BoxSprite();
        tip.GetComponent<SpriteRenderer>().sortingOrder = 4;

        StartCoroutine(MoveBarrier(rb, yBase, height, speed));
    }

    IEnumerator MoveBarrier(Rigidbody2D rb, float yBase, float height, float speed)
    {
        float phase = Random.Range(0f, Mathf.PI * 2f);
        float travel = 3.5f;

        while (rb != null)
        {
            phase += Time.fixedDeltaTime * speed;
            float y = yBase + height * 0.5f + Mathf.Sin(phase) * travel * 0.5f;
            rb.MovePosition(new Vector2(rb.position.x, y));
            yield return new WaitForFixedUpdate();
        }
    }

    // Spike kill-zone — instant death on contact, only on Level 4
    // x     = world X centre
    // yBase = Y base (sits on ground)
    // width = width of trigger
    // h     = spike height
    void Spikes(float x, float yBase, float width, float h)
    {
        var go = new GameObject("SpikeZone");
        go.transform.position = new Vector3(x, yBase + h * 0.5f, 0f);

        // Trigger (no Rigidbody2D → static trigger)
        var col = go.AddComponent<BoxCollider2D>();
        col.size      = new Vector2(width, h);
        col.isTrigger = true;
        go.AddComponent<SpikeKillZone>();

        // Visual — red base strip
        var base_ = Child(go.transform, "Base",
            Vector3.zero, new Vector3(width, 0.25f, 1f));
        base_.AddComponent<SpriteRenderer>().color = new Color(0.75f, 0.05f, 0.05f);
        base_.GetComponent<SpriteRenderer>().sprite = BoxSprite();
        base_.GetComponent<SpriteRenderer>().sortingOrder = 3;

        // Spike triangles (3 thin rotated boxes)
        int count = Mathf.Max(2, Mathf.RoundToInt(width * 1.5f));
        for (int i = 0; i < count; i++)
        {
            float xOff = -width * 0.45f + i * (width * 0.9f / Mathf.Max(count - 1, 1));
            var spike = Child(go.transform, "Sp" + i,
                new Vector3(xOff, h * 0.3f, -0.05f),
                new Vector3(0.18f, h * 0.6f, 1f));
            spike.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-5f, 5f));
            spike.AddComponent<SpriteRenderer>().color = new Color(0.90f, 0.08f, 0.05f);
            spike.GetComponent<SpriteRenderer>().sprite = BoxSprite();
            spike.GetComponent<SpriteRenderer>().sortingOrder = 4;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════════════════════

    static GameObject Child(Transform parent, string name, Vector3 localPos, Vector3 localScale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;
        return go;
    }

    static PhysicsMaterial2D HardMat()
    {
        var m = new PhysicsMaterial2D("Hard");
        m.friction   = 0.5f;
        m.bounciness = 0.0f;
        return m;
    }

    static PhysicsMaterial2D BoulderMat()
    {
        var m = new PhysicsMaterial2D("Boulder");
        m.friction   = 0.35f;
        m.bounciness = 0.15f;
        return m;
    }

    // 1×1 white sprite (reused from other managers pattern)
    static Sprite BoxSprite()
    {
        var tex  = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var fill = new Color[16];
        for (int i = 0; i < 16; i++) fill[i] = Color.white;
        tex.SetPixels(fill);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    // 32×32 circle sprite for boulders
    static Sprite CircleSprite()
    {
        const int S  = 32;
        const float R = S * 0.5f - 0.5f;
        var tex      = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var fill     = new Color[S * S];
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float dx = x - S * 0.5f + 0.5f;
            float dy = y - S * 0.5f + 0.5f;
            fill[y * S + x] = dx * dx + dy * dy <= R * R ? Color.white : Color.clear;
        }
        tex.SetPixels(fill);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), (float)S);
    }
}
