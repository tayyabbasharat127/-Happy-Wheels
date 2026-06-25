using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Car silhouette that drives across the bottom of the menu
    private RectTransform silhouetteCar;
    private float         silhouetteSpeed = 220f; // pixels per second (canvas space)

    void Start()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var canvasGo = new GameObject("MainMenuCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        BuildUI(canvas);

        if (FindAnyObjectByType<PlayerNameInput>() == null)
            new GameObject("PlayerNameInput").AddComponent<PlayerNameInput>();
    }

    void BuildUI(Canvas c)
    {
        // ── Background ─────────────────────────────────────────────────────────
        var bg = Rect("Bg", c.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.AddComponent<Image>().color = new Color(0.05f, 0.03f, 0.12f);

        // Gradient sky bands (simple coloured strips)
        AddStrip(c, "SkyTop",    new Color(0.08f, 0.06f, 0.20f), new Vector2(0.5f, 0.85f), new Vector2(1920f, 320f));
        AddStrip(c, "SkyMid",    new Color(0.12f, 0.08f, 0.28f), new Vector2(0.5f, 0.55f), new Vector2(1920f, 320f));
        AddStrip(c, "GroundBar", new Color(0.08f, 0.14f, 0.06f), new Vector2(0.5f, 0.08f), new Vector2(1920f, 180f));

        // ── Animated car silhouette at bottom ─────────────────────────────────
        var carGo = Rect("CarSilhouette", c.transform,
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(-100f, 120f), new Vector2(160f, 60f));
        var carImg  = carGo.AddComponent<Image>();
        carImg.color = new Color(0.06f, 0.06f, 0.10f, 0.9f);
        silhouetteCar = carGo.GetComponent<RectTransform>();
        StartCoroutine(AnimateCar());

        // ── Title ─────────────────────────────────────────────────────────────
        var titleGo  = Rect("Title", c.transform,
            new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f),
            Vector2.zero, new Vector2(1400f, 140f));
        var titleTxt = titleGo.AddComponent<Text>();
        titleTxt.text      = "HILL CLIMB HAVOC";
        titleTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize  = 82;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.color     = new Color(1f, 0.85f, 0.08f);
        titleTxt.alignment = TextAnchor.MiddleCenter;

        // ── Tagline ───────────────────────────────────────────────────────────
        var subGo  = Rect("Sub", c.transform,
            new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f),
            Vector2.zero, new Vector2(700f, 52f));
        var subTxt = subGo.AddComponent<Text>();
        subTxt.text      = "Don't flip. Don't run dry. Don't die.";
        subTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subTxt.fontSize  = 28;
        subTxt.fontStyle = FontStyle.Italic;
        subTxt.color     = new Color(0.72f, 0.72f, 0.88f);
        subTxt.alignment = TextAnchor.MiddleCenter;

        // ── PLAY button (with breathing animation) ────────────────────────────
        var playGo  = Rect("PlayBtn", c.transform,
            new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.44f),
            Vector2.zero, new Vector2(320f, 86f));
        var playImg = playGo.AddComponent<Image>();
        playImg.color = new Color(0.08f, 0.70f, 0.16f);

        var playBtn = playGo.AddComponent<Button>();
        playBtn.targetGraphic = playImg;
        var pc = playBtn.colors;
        pc.highlightedColor = new Color(0.15f, 0.92f, 0.28f);
        pc.pressedColor     = new Color(0.04f, 0.48f, 0.10f);
        playBtn.colors = pc;
        playBtn.onClick.AddListener(() =>
        {
            GameStateManager.ResetRunProgress();
            LifeManager.ResetLives();
            SceneManager.LoadScene(1);
        });

        var playTxtGo = Rect("Txt", playGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var playTxt   = playTxtGo.AddComponent<Text>();
        playTxt.text      = "PLAY";
        playTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        playTxt.fontSize  = 50;
        playTxt.fontStyle = FontStyle.Bold;
        playTxt.color     = Color.white;
        playTxt.alignment = TextAnchor.MiddleCenter;

        // ── Controls hint ─────────────────────────────────────────────────────
        var hintGo = Rect("Hint", c.transform,
            new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f),
            Vector2.zero, new Vector2(600f, 96f));
        hintGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);

        var hintTxtGo = Rect("Txt", hintGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var hintTxt   = hintTxtGo.AddComponent<Text>();
        hintTxt.text      = "→  Accelerate     ←  Brake\nSPACE  Nitro Boost     P  Pause\nCollect fuel cans to keep going!";
        hintTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintTxt.fontSize  = 20;
        hintTxt.color     = new Color(0.70f, 0.70f, 0.84f);
        hintTxt.alignment = TextAnchor.MiddleCenter;

        // ── Staggered intro animations ────────────────────────────────────────
        titleGo.transform.localScale = Vector3.zero;
        Tween.Scale(this, titleGo.transform, Vector3.one, 0.55f, Tween.Ease.OutBack, delay: 0.1f);

        subGo.transform.localScale = Vector3.zero;
        Tween.Scale(this, subGo.transform, Vector3.one, 0.4f, Tween.Ease.OutBack, delay: 0.38f);

        playGo.transform.localScale = Vector3.zero;
        Tween.Scale(this, playGo.transform, Vector3.one, 0.5f, Tween.Ease.OutBack, delay: 0.62f);

        hintGo.transform.localScale = Vector3.zero;
        Tween.Scale(this, hintGo.transform, Vector3.one, 0.4f, Tween.Ease.OutBack, delay: 0.85f);

        // Breathing play button — starts after intro finishes
        StartCoroutine(BreathingButton(playGo.transform));
    }

    // Loops the car silhouette from off-screen left to off-screen right
    private IEnumerator AnimateCar()
    {
        const float refWidth = 1920f;
        if (silhouetteCar == null) yield break;

        while (true)
        {
            silhouetteCar.anchoredPosition = new Vector2(-refWidth * 0.55f, 120f);
            float x = -refWidth * 0.55f;
            float limit = refWidth * 0.55f + 200f;

            while (x < limit)
            {
                x += silhouetteSpeed * Time.unscaledDeltaTime;
                if (silhouetteCar != null)
                    silhouetteCar.anchoredPosition = new Vector2(x, 120f);
                yield return null;
            }
            // brief pause before looping
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }

    // Gentle scale pulse on the Play button to draw attention
    private IEnumerator BreathingButton(Transform t)
    {
        yield return new WaitForSecondsRealtime(1.1f);
        while (t != null)
        {
            float elapsed = 0f;
            const float halfPeriod = 0.75f;
            while (elapsed < halfPeriod)
            {
                elapsed += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1.00f, 1.04f, elapsed / halfPeriod);
                if (t != null) t.localScale = Vector3.one * s;
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < halfPeriod)
            {
                elapsed += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1.04f, 1.00f, elapsed / halfPeriod);
                if (t != null) t.localScale = Vector3.one * s;
                yield return null;
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void AddStrip(Canvas c, string name, Color col, Vector2 anchorCenter, Vector2 size)
    {
        var go = Rect(name, c.transform, new Vector2(0f, anchorCenter.y), new Vector2(1f, anchorCenter.y),
            Vector2.zero, new Vector2(0f, size.y));
        go.AddComponent<Image>().color = col;
    }

    static GameObject Rect(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt           = go.AddComponent<RectTransform>();
        rt.anchorMin     = anchorMin;
        rt.anchorMax     = anchorMax;
        rt.pivot         = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta     = sizeDelta;
        return go;
    }
}
