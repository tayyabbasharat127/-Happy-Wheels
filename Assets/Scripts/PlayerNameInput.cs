using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerNameInput : MonoBehaviour
{
    public const string KeyPlayerName = "HW_PlayerName";
    public static bool IsOpen { get; private set; }

    private static bool promptedThisSession;
    private Font font;
    private InputField input;
    private GameObject root;
    private bool modal;

    void Awake()
    {
        if (!promptedThisSession)
            IsOpen = true;
    }

    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();

        string savedName = PlayerPrefs.GetString(KeyPlayerName, "").Trim();
        modal = !promptedThisSession;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null || modal)
            canvas = CreateCanvas(modal ? "NamePromptCanvas" : "NameInputCanvas", modal ? 2000 : 50);

        if (modal)
            BuildModal(canvas, savedName);
        else
            Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (modal)
            IsOpen = false;
    }

    public static string GetPlayerName()
    {
        string name = PlayerPrefs.GetString(KeyPlayerName, "").Trim();
        return string.IsNullOrWhiteSpace(name) ? "Player" : name;
    }

    private void BuildModal(Canvas canvas, string savedName)
    {
        IsOpen = true;
        Time.timeScale = 0f;

        root = MakeRect("NamePrompt", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.06f, 0.92f);

        AddText(root.transform, "ENTER YOUR NAME", 52, new Color(1f, 0.84f, 0.18f),
            new Vector2(0f, 140f), new Vector2(700f, 80f));
        AddText(root.transform, "Your name appears on the left HUD.", 24, Color.white,
            new Vector2(0f, 82f), new Vector2(760f, 40f));

        input = BuildInput(root.transform, new Vector2(0f, 10f), new Vector2(520f, 64f), savedName);
        AddButton(root.transform, "START", new Color(0.12f, 0.6f, 0.2f), new Vector2(0f, -82f), SaveAndClose);
        input.ActivateInputField();
    }

    private InputField BuildInput(Transform parent, Vector2 pos, Vector2 size, string value)
    {
        var fieldGo = MakeRect("NameField", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        fieldGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.95f);

        var field = fieldGo.AddComponent<InputField>();
        field.characterLimit = 12;
        field.text = value;

        var phGo = MakeRect("Placeholder", fieldGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        StretchPadding(phGo, 14f);
        var placeholder = phGo.AddComponent<Text>();
        placeholder.text = "Enter name...";
        placeholder.font = font;
        placeholder.fontSize = 24;
        placeholder.fontStyle = FontStyle.Italic;
        placeholder.color = new Color(0.42f, 0.42f, 0.42f);
        placeholder.alignment = TextAnchor.MiddleLeft;

        var textGo = MakeRect("Text", fieldGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        StretchPadding(textGo, 14f);
        var text = textGo.AddComponent<Text>();
        text.font = font;
        text.fontSize = 26;
        text.color = new Color(0.04f, 0.04f, 0.08f);
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;

        field.placeholder = placeholder;
        field.textComponent = text;
        return field;
    }

    private void SaveAndClose()
    {
        string value = input != null ? input.text.Trim() : "";
        if (string.IsNullOrWhiteSpace(value))
            return;

        PlayerPrefs.SetString(KeyPlayerName, value);
        PlayerPrefs.Save();

        promptedThisSession = true;
        IsOpen = false;
        Time.timeScale = 1f;
        Destroy(root);
        Destroy(gameObject);
    }

    private Canvas CreateCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    private GameObject MakeRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    private Text AddText(Transform parent, string content, int size, Color col, Vector2 pos, Vector2 sizeDelta)
    {
        var go = MakeRect("Txt", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, sizeDelta);
        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.color = col;
        text.alignment = TextAnchor.MiddleCenter;
        return text;
    }

    private void AddButton(Transform parent, string label, Color color, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = MakeRect("Btn_" + label, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(260f, 64f));
        var image = go.AddComponent<Image>();
        image.color = color;
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        AddText(go.transform, label, 28, Color.white, Vector2.zero, new Vector2(260f, 64f));
    }

    private void StretchPadding(GameObject go, float pad)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
    }
}
