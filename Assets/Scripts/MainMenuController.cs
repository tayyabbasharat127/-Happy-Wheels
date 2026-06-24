using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    void Start()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        var canvasGo = new GameObject("MainMenuCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight   = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        BuildUI(canvas);

        if (FindAnyObjectByType<PlayerNameInput>() == null)
            new GameObject("PlayerNameInput").AddComponent<PlayerNameInput>();
    }

    void BuildUI(Canvas c)
    {
        // ── Background ────────────────────────────────────────────────────────
        var bg = Rect("Bg", c.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.AddComponent<Image>().color = new Color(0.07f, 0.04f, 0.14f);

        // ── Title ─────────────────────────────────────────────────────────────
        var titleGo = Rect("Title", c.transform,
            new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f),
            Vector2.zero, new Vector2(900f, 130f));
        var titleTxt = titleGo.AddComponent<Text>();
        titleTxt.text       = "HAPPY WHEELS";
        titleTxt.font       = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize   = 88;
        titleTxt.fontStyle  = FontStyle.Bold;
        titleTxt.color      = new Color(1f, 0.85f, 0.08f);
        titleTxt.alignment  = TextAnchor.MiddleCenter;

        // ── Sub-title ─────────────────────────────────────────────────────────
        var subGo = Rect("Sub", c.transform,
            new Vector2(0.5f, 0.60f), new Vector2(0.5f, 0.60f),
            Vector2.zero, new Vector2(700f, 52f));
        var subTxt = subGo.AddComponent<Text>();
        subTxt.text      = "Reach each distance goal — don't flip!";
        subTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subTxt.fontSize  = 28;
        subTxt.fontStyle = FontStyle.Italic;
        subTxt.color     = new Color(0.75f, 0.75f, 0.88f);
        subTxt.alignment = TextAnchor.MiddleCenter;

        // ── PLAY button ───────────────────────────────────────────────────────
        var playGo  = Rect("PlayBtn", c.transform,
            new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.44f),
            Vector2.zero, new Vector2(300f, 80f));
        var playImg = playGo.AddComponent<Image>();
        playImg.color = new Color(0.1f, 0.72f, 0.18f);

        var playBtn = playGo.AddComponent<Button>();
        playBtn.targetGraphic = playImg;
        var pc = playBtn.colors;
        pc.highlightedColor = new Color(0.15f, 0.92f, 0.28f);
        pc.pressedColor     = new Color(0.05f, 0.48f, 0.1f);
        playBtn.colors = pc;
        playBtn.onClick.AddListener(() =>
        {
            GameStateManager.ResetRunProgress();
            SceneManager.LoadScene(1);
        });

        var playTxtGo = Rect("Txt", playGo.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var playTxt = playTxtGo.AddComponent<Text>();
        playTxt.text      = "PLAY";
        playTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        playTxt.fontSize  = 46;
        playTxt.fontStyle = FontStyle.Bold;
        playTxt.color     = Color.white;
        playTxt.alignment = TextAnchor.MiddleCenter;

        // ── Controls hint ─────────────────────────────────────────────────────
        var hintGo = Rect("Hint", c.transform,
            new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f),
            Vector2.zero, new Vector2(480f, 88f));
        var hintBg  = hintGo.AddComponent<Image>();
        hintBg.color = new Color(1f, 1f, 1f, 0.06f);

        var hintTxtGo = Rect("Txt", hintGo.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var hintTxt = hintTxtGo.AddComponent<Text>();
        hintTxt.text      = "→  Right Arrow  =  Accelerate\n\nCollect fuel cans to keep going!";
        hintTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintTxt.fontSize  = 22;
        hintTxt.color     = new Color(0.72f, 0.72f, 0.85f);
        hintTxt.alignment = TextAnchor.MiddleCenter;

        // ── Animations ───────────────────────────────────────────────────────
        titleGo.transform.localScale = Vector3.zero;
        Tween.Scale(this, titleGo.transform, Vector3.one, 0.55f,
            Tween.Ease.OutBack, delay: 0.1f);

        subGo.transform.localScale = Vector3.zero;
        Tween.Scale(this, subGo.transform, Vector3.one, 0.4f,
            Tween.Ease.OutBack, delay: 0.4f);

        playGo.transform.localScale = Vector3.zero;
        Tween.Scale(this, playGo.transform, Vector3.one, 0.5f,
            Tween.Ease.OutBack, delay: 0.65f);
    }

    static GameObject Rect(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt          = go.AddComponent<RectTransform>();
        rt.anchorMin    = anchorMin;
        rt.anchorMax    = anchorMax;
        rt.pivot        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta    = sizeDelta;
        return go;
    }
}
