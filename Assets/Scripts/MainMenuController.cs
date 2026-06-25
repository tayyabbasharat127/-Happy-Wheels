using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // ── Car animation ──────────────────────────────────────────────────────────
    private RectTransform carRoot;
    private const float CarSpeed   = 270f;
    private const float CarStartX  = -360f;
    private const float CarFinishX = 2300f;

    // ── Stars ──────────────────────────────────────────────────────────────────
    private readonly List<Image> stars = new List<Image>(70);

    // ── Title references for intro ─────────────────────────────────────────────
    private Transform titleGroup;
    private Transform playBtnT;
    private Transform hintPanelT;
    private Transform badgeT;

    // ══════════════════════════════════════════════════════════════════════════
    void Start()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var canvasGo    = new GameObject("MainMenuCanvas");
        var canvas      = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        BuildSky(canvas);
        BuildCelestial(canvas);
        BuildStars(canvas);
        BuildMountains(canvas);
        BuildGround(canvas);
        BuildCar(canvas);
        BuildTitle(canvas);
        BuildPlayButton(canvas);
        BuildControlsHint(canvas);
        BuildBadge(canvas);
        PlayIntroAnimations();

        if (FindAnyObjectByType<PlayerNameInput>() == null)
            new GameObject("PlayerNameInput").AddComponent<PlayerNameInput>();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Background sky gradient
    // ══════════════════════════════════════════════════════════════════════════
    void BuildSky(Canvas c)
    {
        // Base dark fill
        Stretch("BgBase", c.transform, new Color(0.01f, 0.01f, 0.06f));

        // Sunset gradient bands — top to bottom
        Band(c.transform, "Sky1", 0.78f, 1.00f, new Color(0.03f, 0.01f, 0.12f));
        Band(c.transform, "Sky2", 0.57f, 0.80f, new Color(0.10f, 0.03f, 0.22f));
        Band(c.transform, "Sky3", 0.38f, 0.60f, new Color(0.22f, 0.05f, 0.25f));
        Band(c.transform, "Sky4", 0.24f, 0.42f, new Color(0.42f, 0.09f, 0.18f));
        Band(c.transform, "Sky5", 0.15f, 0.28f, new Color(0.62f, 0.18f, 0.05f));
        Band(c.transform, "Sky6", 0.10f, 0.19f, new Color(0.82f, 0.32f, 0.03f));
        // Bright horizon flare
        Band(c.transform, "Flare", 0.145f, 0.175f, new Color(1.00f, 0.60f, 0.10f, 0.55f));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Sun with multi-ring glow
    // ══════════════════════════════════════════════════════════════════════════
    void BuildCelestial(Canvas c)
    {
        // Glow rings (largest behind)
        Circle(c.transform, "Glow3", 0.76f, 0.70f, 290f, new Color(1.00f, 0.65f, 0.15f, 0.06f));
        Circle(c.transform, "Glow2", 0.76f, 0.70f, 210f, new Color(1.00f, 0.75f, 0.25f, 0.12f));
        Circle(c.transform, "Glow1", 0.76f, 0.70f, 150f, new Color(1.00f, 0.85f, 0.40f, 0.22f));
        // Sun disc
        Circle(c.transform, "Sun",   0.76f, 0.70f, 108f, new Color(1.00f, 0.95f, 0.65f, 1.00f));
        // Soft inner spot to add depth
        Circle(c.transform, "SunC",  0.76f, 0.70f,  76f, new Color(1.00f, 0.98f, 0.85f, 0.50f));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Twinkling stars
    // ══════════════════════════════════════════════════════════════════════════
    void BuildStars(Canvas c)
    {
        // Only above horizon (~18% up)
        var root = Rect("StarRoot", c.transform,
            new Vector2(0f, 0.18f), Vector2.one, Vector2.zero, Vector2.zero);

        for (int i = 0; i < 68; i++)
        {
            float ax = Random.Range(0.01f, 0.99f);
            float ay = Random.Range(0.04f, 0.96f);
            float sz = Random.Range(2f, 5.5f);
            var go  = Rect("S" + i, root.transform,
                new Vector2(ax, ay), new Vector2(ax, ay), Vector2.zero, new Vector2(sz, sz));
            var img = go.AddComponent<Image>();
            float b  = Random.Range(0.55f, 1f);
            img.color = new Color(b, b * 0.90f + 0.10f, 1f, Random.Range(0.35f, 1f));
            stars.Add(img);
        }
        StartCoroutine(TwinkleLoop());
    }

    IEnumerator TwinkleLoop()
    {
        while (true)
        {
            foreach (var img in stars)
                if (img != null && Random.value < 0.022f)
                    StartCoroutine(TwinkleOne(img));
            yield return new WaitForSecondsRealtime(0.09f);
        }
    }

    IEnumerator TwinkleOne(Image img)
    {
        if (img == null) yield break;
        Color orig = img.color;
        float dur  = Random.Range(0.35f, 0.90f);
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            if (img == null) yield break;
            float a = Mathf.Lerp(orig.a, orig.a * 0.12f, Mathf.Sin(t / dur * Mathf.PI));
            img.color = new Color(orig.r, orig.g, orig.b, a);
            yield return null;
        }
        if (img) img.color = orig;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Mountain silhouettes — two depth layers
    // ══════════════════════════════════════════════════════════════════════════
    void BuildMountains(Canvas c)
    {
        // Far layer — dark purple peaks
        Mountain(c.transform, 0.08f, 0.255f, new Color(0.07f, 0.02f, 0.14f));
        Mountain(c.transform, 0.22f, 0.220f, new Color(0.05f, 0.01f, 0.11f));
        Mountain(c.transform, 0.38f, 0.270f, new Color(0.07f, 0.02f, 0.14f));
        Mountain(c.transform, 0.55f, 0.235f, new Color(0.05f, 0.01f, 0.11f));
        Mountain(c.transform, 0.70f, 0.260f, new Color(0.07f, 0.02f, 0.14f));
        Mountain(c.transform, 0.86f, 0.228f, new Color(0.05f, 0.01f, 0.11f));
        Mountain(c.transform, 1.00f, 0.250f, new Color(0.07f, 0.02f, 0.14f));

        // Near layer — dark green hills
        Mountain(c.transform, 0.02f, 0.170f, new Color(0.03f, 0.09f, 0.02f));
        Mountain(c.transform, 0.18f, 0.155f, new Color(0.04f, 0.11f, 0.03f));
        Mountain(c.transform, 0.34f, 0.175f, new Color(0.03f, 0.09f, 0.02f));
        Mountain(c.transform, 0.50f, 0.160f, new Color(0.04f, 0.11f, 0.03f));
        Mountain(c.transform, 0.66f, 0.172f, new Color(0.03f, 0.09f, 0.02f));
        Mountain(c.transform, 0.82f, 0.158f, new Color(0.04f, 0.11f, 0.03f));
        Mountain(c.transform, 0.97f, 0.165f, new Color(0.03f, 0.09f, 0.02f));
    }

    // cx = 0-1 horizontal centre, peakY = peak anchor Y, colour
    void Mountain(Transform parent, float cx, float peakY, Color col)
    {
        const float hw = 0.145f;
        var go = Rect("Mt", parent,
            new Vector2(Mathf.Max(0f, cx - hw), 0f),
            new Vector2(Mathf.Min(1f, cx + hw), peakY),
            Vector2.zero, Vector2.zero);
        go.AddComponent<Image>().color = col;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Ground, road and dashes
    // ══════════════════════════════════════════════════════════════════════════
    void BuildGround(Canvas c)
    {
        // Ground body
        Band(c.transform, "Ground",  0.00f, 0.155f, new Color(0.03f, 0.09f, 0.02f));
        // Bright grass edge
        Band(c.transform, "GEdge",   0.148f, 0.165f, new Color(0.07f, 0.22f, 0.04f));
        // Road stripe
        Band(c.transform, "Road",    0.118f, 0.151f, new Color(0.07f, 0.09f, 0.07f));
        // Subtle road shoulder lines
        Band(c.transform, "ShldTop", 0.149f, 0.151f, new Color(0.55f, 0.50f, 0.05f, 0.45f));
        Band(c.transform, "ShldBot", 0.118f, 0.120f, new Color(0.55f, 0.50f, 0.05f, 0.45f));

        // Dashed center line
        for (int i = 0; i < 16; i++)
        {
            float x0 = i / 16f + 0.004f;
            float x1 = x0 + 0.038f;
            Band2(c.transform, "D" + i, x0, x1, 0.128f, 0.140f, new Color(0.80f, 0.68f, 0.10f, 0.70f));
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Animated car silhouette
    // ══════════════════════════════════════════════════════════════════════════
    void BuildCar(Canvas c)
    {
        // Root anchored at bottom-left; y=188 places tires on the grass edge
        // (road top at ~163px, grass edge at ~178px, tire radius 23px → center at 200px)
        var rootGo = Rect("CarRoot", c.transform,
            Vector2.zero, Vector2.zero,
            new Vector2(CarStartX, 188f), Vector2.zero);
        carRoot = rootGo.GetComponent<RectTransform>();

        Transform p = rootGo.transform;

        // Car body (main box)
        CarPart(p, "Body",  new Vector2( 90f,  38f), new Vector2(180f, 56f), new Color(0.08f, 0.10f, 0.20f));
        // Roof
        CarPart(p, "Roof",  new Vector2( 68f,  72f), new Vector2(112f, 44f), new Color(0.10f, 0.12f, 0.24f));
        // Windshield glow
        CarPart(p, "Wind",  new Vector2(110f,  74f), new Vector2( 50f, 28f), new Color(0.28f, 0.58f, 1.00f, 0.25f));
        // Headlight
        CarPart(p, "Head",  new Vector2(180f,  38f), new Vector2( 18f, 14f), new Color(1.00f, 0.94f, 0.55f, 1.00f));
        // Headlight cone beam (very faint)
        CarPart(p, "Beam",  new Vector2(310f,  26f), new Vector2(260f,  8f), new Color(1.00f, 0.92f, 0.45f, 0.05f));
        // Tail-light
        CarPart(p, "Tail",  new Vector2(  2f,  38f), new Vector2(  9f, 14f), new Color(0.90f, 0.05f, 0.05f, 0.90f));
        // Rear exhaust pipe
        CarPart(p, "Pipe",  new Vector2(  4f,  20f), new Vector2( 22f,  8f), new Color(0.25f, 0.25f, 0.28f));

        // Tires — y=10 puts tire center at canvas y≈198, bottom≈175 ≈ grass edge
        SpawnTire(p, "TireB", new Vector2( 47f, 10f));
        SpawnTire(p, "TireF", new Vector2(158f, 10f));

        // Exhaust puffs
        StartCoroutine(ExhaustPuffs(p));
        StartCoroutine(AnimateCar());
    }

    void CarPart(Transform p, string n, Vector2 pos, Vector2 sz, Color col)
    {
        var go = Rect(n, p, Vector2.zero, Vector2.zero, pos, sz);
        go.AddComponent<Image>().color = col;
    }

    void SpawnTire(Transform parent, string name, Vector2 pos)
    {
        var tire = Rect(name, parent, Vector2.zero, Vector2.zero, pos, new Vector2(46f, 46f));
        tire.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.07f);
        var rim = Rect("Rim", tire.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(18f, 18f));
        rim.AddComponent<Image>().color = new Color(0.40f, 0.42f, 0.48f);
        // Spoke cross H
        var sh = Rect("SH", tire.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(16f, 3f));
        sh.AddComponent<Image>().color = new Color(0.28f, 0.30f, 0.34f);
        var sv = Rect("SV", tire.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(3f, 16f));
        sv.AddComponent<Image>().color = new Color(0.28f, 0.30f, 0.34f);

        StartCoroutine(SpinTire(tire.GetComponent<RectTransform>()));
    }

    IEnumerator SpinTire(RectTransform rt)
    {
        float angle = 0f;
        while (rt != null)
        {
            angle -= 380f * Time.unscaledDeltaTime;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
    }

    IEnumerator ExhaustPuffs(Transform parent)
    {
        // Spawn small grey puffs behind the car at intervals
        var pool = new List<RectTransform>();
        for (int i = 0; i < 6; i++)
        {
            var p = Rect("Puff" + i, parent,
                Vector2.zero, Vector2.zero,
                new Vector2(-30f, 15f), new Vector2(12f, 10f));
            var img = p.AddComponent<Image>();
            img.color = new Color(0.55f, 0.55f, 0.58f, 0f);
            pool.Add(p.GetComponent<RectTransform>());
        }

        int idx = 0;
        while (true)
        {
            yield return new WaitForSecondsRealtime(0.22f);
            var rt = pool[idx % pool.Count];
            if (rt != null)
                StartCoroutine(AnimatePuff(rt.GetComponent<Image>(), rt));
            idx++;
        }
    }

    IEnumerator AnimatePuff(Image img, RectTransform rt)
    {
        rt.anchoredPosition = new Vector2(Random.Range(-55f, -20f), Random.Range(8f, 22f));
        rt.sizeDelta = new Vector2(Random.Range(10f, 18f), Random.Range(8f, 14f));
        float dur = Random.Range(0.4f, 0.7f);
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            if (img == null) yield break;
            float a = Mathf.Lerp(0.55f, 0f, t / dur);
            img.color = new Color(0.55f, 0.55f, 0.58f, a);
            rt.anchoredPosition += Vector2.left * 55f * Time.unscaledDeltaTime;
            yield return null;
        }
        if (img) img.color = new Color(0.55f, 0.55f, 0.58f, 0f);
    }

    IEnumerator AnimateCar()
    {
        while (true)
        {
            if (carRoot == null) yield break;
            carRoot.anchoredPosition = new Vector2(CarStartX, carRoot.anchoredPosition.y);
            while (carRoot != null && carRoot.anchoredPosition.x < CarFinishX)
            {
                carRoot.anchoredPosition += Vector2.right * CarSpeed * Time.unscaledDeltaTime;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.3f);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Title with fake bloom glow
    // ══════════════════════════════════════════════════════════════════════════
    void BuildTitle(Canvas c)
    {
        titleGroup = Rect("TitleGroup", c.transform,
            new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f),
            Vector2.zero, new Vector2(1600f, 180f)).transform;

        // Glow layer 3 — widest, most transparent warm orange
        var g3 = MakeLabel(titleGroup, "G3", "HILL CLIMB HAVOC", 94,
            new Color(0.90f, 0.35f, 0.00f, 0.09f), FontStyle.Bold, Vector2.zero, new Vector2(1640f, 185f));
        g3.transform.localScale = Vector3.one * 1.08f;

        // Glow layer 2 — medium, amber
        var g2 = MakeLabel(titleGroup, "G2", "HILL CLIMB HAVOC", 94,
            new Color(1.00f, 0.60f, 0.00f, 0.18f), FontStyle.Bold, Vector2.zero, new Vector2(1620f, 182f));
        g2.transform.localScale = Vector3.one * 1.04f;

        // Glow layer 1 — tight, gold
        MakeLabel(titleGroup, "G1", "HILL CLIMB HAVOC", 94,
            new Color(1.00f, 0.80f, 0.00f, 0.30f), FontStyle.Bold, Vector2.zero, new Vector2(1605f, 180f));

        // Main crisp title
        MakeLabel(titleGroup, "Title", "HILL CLIMB HAVOC", 94,
            new Color(1.00f, 0.88f, 0.06f), FontStyle.Bold, Vector2.zero, new Vector2(1600f, 180f));

        // Left & right gold accent lines
        AccentLine(titleGroup, "LL",  new Vector2(-780f, 0f));
        AccentLine(titleGroup, "LR",  new Vector2( 780f, 0f));

        // Tagline
        MakeLabel(c.transform, "Tag",
            "- Don't flip.   Don't run dry.   Don't die. -",
            27, new Color(0.82f, 0.78f, 1.00f), FontStyle.Italic,
            new Vector2(0f, 0f), new Vector2(1100f, 52f),
            anchor: new Vector2(0.5f, 0.570f));

        // Start title pulse coroutine after intro
        StartCoroutine(TitlePulse());
    }

    void AccentLine(Transform parent, string name, Vector2 pos)
    {
        var go = Rect(name, parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(60f, 3f));
        go.AddComponent<Image>().color = new Color(1f, 0.80f, 0.10f, 0.65f);
    }

    IEnumerator TitlePulse()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        while (titleGroup != null)
        {
            for (float t = 0f; t < 1.8f; t += Time.unscaledDeltaTime)
            {
                if (titleGroup == null) yield break;
                float s = 1f + 0.012f * Mathf.Sin(t / 1.8f * Mathf.PI * 2f);
                titleGroup.localScale = Vector3.one * s;
                yield return null;
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Play button with pulsing outer ring
    // ══════════════════════════════════════════════════════════════════════════
    void BuildPlayButton(Canvas c)
    {
        // Pulsing amber glow ring (behind button)
        var ring = Rect("Ring", c.transform,
            new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.44f),
            Vector2.zero, new Vector2(360f, 100f));
        ring.AddComponent<Image>().color = new Color(1f, 0.72f, 0f, 0f);
        StartCoroutine(PulseRing(ring.GetComponent<Image>()));

        // Drop shadow
        var sh = Rect("BtnShadow", c.transform,
            new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.44f),
            new Vector2(6f, -7f), new Vector2(352f, 96f));
        sh.AddComponent<Image>().color = new Color(0f, 0.07f, 0f, 0.70f);

        // Button body
        var btnGo  = Rect("PlayBtn", c.transform,
            new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.44f),
            Vector2.zero, new Vector2(352f, 96f));
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.09f, 0.70f, 0.17f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var bc = btn.colors;
        bc.normalColor      = new Color(0.09f, 0.70f, 0.17f);
        bc.highlightedColor = new Color(0.16f, 0.92f, 0.26f);
        bc.pressedColor     = new Color(0.04f, 0.48f, 0.10f);
        bc.fadeDuration     = 0.10f;
        btn.colors = bc;
        var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
        btn.onClick.AddListener(() =>
        {
            GameStateManager.ResetRunProgress();
            LifeManager.ResetLives();
            SceneManager.LoadScene(1);
        });

        // Top sheen strip
        var sheen = Rect("Sheen", btnGo.transform,
            new Vector2(0f, 0.55f), Vector2.one, Vector2.zero, Vector2.zero);
        sheen.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.09f);

        // Left accent bar
        var bar = Rect("Acc", btnGo.transform,
            new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(4f, 0f), new Vector2(6f, 0f));
        bar.AddComponent<Image>().color = new Color(0.2f, 1.0f, 0.3f, 0.55f);

        // Label
        var txtGo = Rect("Lbl", btnGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var txt   = txtGo.AddComponent<Text>();
        txt.text      = "PLAY";
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = 56;
        txt.fontStyle = FontStyle.Bold;
        txt.color     = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;

        playBtnT = btnGo.transform;
        StartCoroutine(BreathButton(playBtnT));
    }

    IEnumerator PulseRing(Image img)
    {
        yield return new WaitForSecondsRealtime(0.9f);
        var rt = img.rectTransform;
        while (img != null)
        {
            Vector2 s0 = new Vector2(352f, 96f);
            Vector2 s1 = new Vector2(430f, 140f);

            for (float t = 0f; t < 0.55f; t += Time.unscaledDeltaTime)
            {
                if (img == null) yield break;
                float p = t / 0.55f;
                img.color = new Color(1f, 0.72f, 0.05f, Mathf.Lerp(0f, 0.32f, p));
                rt.sizeDelta = Vector2.Lerp(s0, s1, p);
                yield return null;
            }
            for (float t = 0f; t < 0.65f; t += Time.unscaledDeltaTime)
            {
                if (img == null) yield break;
                float p = t / 0.65f;
                img.color = new Color(1f, 0.72f, 0.05f, Mathf.Lerp(0.32f, 0f, p));
                rt.sizeDelta = Vector2.Lerp(s1, new Vector2(390f, 110f), p);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1.10f);
        }
    }

    IEnumerator BreathButton(Transform t)
    {
        yield return new WaitForSecondsRealtime(1.1f);
        while (t != null)
        {
            for (float e = 0f; e < 0.75f; e += Time.unscaledDeltaTime)
            { if (t) t.localScale = Vector3.one * Mathf.Lerp(1.00f, 1.045f, e / 0.75f); yield return null; }
            for (float e = 0f; e < 0.75f; e += Time.unscaledDeltaTime)
            { if (t) t.localScale = Vector3.one * Mathf.Lerp(1.045f, 1.00f, e / 0.75f); yield return null; }
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Controls hint panel
    // ══════════════════════════════════════════════════════════════════════════
    void BuildControlsHint(Canvas c)
    {
        var panel = Rect("CtrlPanel", c.transform,
            new Vector2(0.5f, 0.268f), new Vector2(0.5f, 0.268f),
            Vector2.zero, new Vector2(700f, 80f));
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.38f);

        // Gold left/right accent lines
        Accent(panel.transform, "AL", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(3f, 0f),  new Vector2(4f, 0f));
        Accent(panel.transform, "AR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-3f, 0f), new Vector2(4f, 0f));

        var row1 = Rect("Row1", panel.transform,
            new Vector2(0f, 0.52f), new Vector2(1f, 1.00f), Vector2.zero, Vector2.zero);
        var t1 = row1.AddComponent<Text>();
        t1.text      = "Right: Accelerate   |   Left: Brake   |   Space: Nitro   |   P: Pause";
        t1.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t1.fontSize  = 19;
        t1.color     = new Color(0.80f, 0.80f, 1.00f);
        t1.alignment = TextAnchor.MiddleCenter;

        var row2 = Rect("Row2", panel.transform,
            new Vector2(0f, 0.00f), new Vector2(1f, 0.48f), Vector2.zero, Vector2.zero);
        var t2 = row2.AddComponent<Text>();
        t2.text      = "Collect fuel cans to keep going!";
        t2.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t2.fontSize  = 17;
        t2.fontStyle = FontStyle.Italic;
        t2.color     = new Color(0.60f, 0.75f, 0.60f);
        t2.alignment = TextAnchor.MiddleCenter;

        hintPanelT = panel.transform;
    }

    void Accent(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 off, Vector2 sz)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt      = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = off;
        rt.offsetMax = off + sz;
        go.AddComponent<Image>().color = new Color(1f, 0.78f, 0.10f, 0.75f);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DSU Game Jam badge — top-left corner
    // ══════════════════════════════════════════════════════════════════════════
    void BuildBadge(Canvas c)
    {
        var badge = Rect("Badge", c.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(118f, -40f), new Vector2(300f, 58f));
        badge.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.60f);

        // Gold left bar
        var bar = Rect("Bar", badge.transform,
            new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(3f, 0f), new Vector2(5f, 0f));
        bar.AddComponent<Image>().color = new Color(1f, 0.78f, 0.08f, 0.90f);

        var label = Rect("Txt", badge.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero)
                       .AddComponent<Text>();
        label.text      = "DSU GAME JAM  SPRING 2026";
        label.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize  = 17;
        label.fontStyle = FontStyle.Bold;
        label.color     = new Color(1f, 0.82f, 0.22f);
        label.alignment = TextAnchor.MiddleCenter;

        badgeT = badge.transform;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Intro staggered entrance animations
    // ══════════════════════════════════════════════════════════════════════════
    void PlayIntroAnimations()
    {
        if (titleGroup != null)
        {
            titleGroup.localScale = Vector3.zero;
            Tween.Scale(this, titleGroup, Vector3.one, 0.60f, Tween.Ease.OutBack, delay: 0.10f);
        }
        if (playBtnT != null)
        {
            playBtnT.localScale = Vector3.zero;
            Tween.Scale(this, playBtnT, Vector3.one, 0.52f, Tween.Ease.OutBack, delay: 0.50f);
        }
        if (hintPanelT != null)
        {
            hintPanelT.localScale = Vector3.zero;
            Tween.Scale(this, hintPanelT, Vector3.one, 0.40f, Tween.Ease.OutBack, delay: 0.80f);
        }
        if (badgeT != null)
        {
            badgeT.localScale = Vector3.zero;
            Tween.Scale(this, badgeT, Vector3.one, 0.35f, Tween.Ease.OutBack, delay: 0.95f);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Shared layout helpers
    // ══════════════════════════════════════════════════════════════════════════

    // Creates a RectTransform child (anchor + size)
    static GameObject Rect(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        var go        = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt        = go.AddComponent<RectTransform>();
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.pivot      = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta  = size;
        return go;
    }

    // Full-screen colour fill
    static void Stretch(string name, Transform parent, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = col;
    }

    // Horizontal band (full-width, anchor Y range)
    static void Band(Transform parent, string name, float yMin, float yMax, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, yMin);
        rt.anchorMax = new Vector2(1f, yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = col;
    }

    // Band2: horizontal band using both X and Y anchor ranges
    static void Band2(Transform parent, string name, float xMin, float xMax, float yMin, float yMax, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = col;
    }

    // Circle: centred Image at normalized (cx,cy) with pixel size
    static void Circle(Transform parent, string name, float cx, float cy, float size, Color col)
    {
        var go = Rect(name, parent, new Vector2(cx, cy), new Vector2(cx, cy), Vector2.zero, new Vector2(size, size));
        go.AddComponent<Image>().color = col;
    }

    // Text label at anchor centre (optional override) with pixel size
    static Text MakeLabel(Transform parent, string name, string text, int fontSize,
        Color col, FontStyle style, Vector2 pos, Vector2 size, Vector2? anchor = null)
    {
        var a  = anchor ?? new Vector2(0.5f, 0.5f);
        var go = Rect(name, parent, a, a, pos, size);
        var t  = go.AddComponent<Text>();
        t.text      = text;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = fontSize;
        t.fontStyle = style;
        t.color     = col;
        t.alignment = TextAnchor.MiddleCenter;
        return t;
    }
}
