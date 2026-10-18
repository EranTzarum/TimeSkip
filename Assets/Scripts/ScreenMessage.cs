using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// One runtime UI style for the jam polish: navy panel, gold accent line, bold white text, gold key names.
// Everything is built in code, so no scene edits are needed. UI only.
public static class ScreenMessage
{
    public static readonly Color Navy = new Color(0.07f, 0.09f, 0.19f, 0.88f);
    public static readonly Color Gold = new Color(0.95f, 0.76f, 0.31f);
    public const string GoldHex = "#F2C14E";

    static Font font;
    static Font UiFont => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

    // A key name as it appears in instructions, e.g. Key("ENTER")
    public static string Key(string k) => $"<color={GoldHex}>[{k}]</color>";

    public static Canvas Canvas(string name, int order)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    // Navy panel with a gold line along its bottom (top: true puts it on top)
    public static Image Panel(Transform parent, Vector2 min, Vector2 max, bool lineOnTop = false)
    {
        var rt = Rect("Panel", parent, min, max);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = Navy;
        img.raycastTarget = false;
        var line = Rect("Accent", rt, new Vector2(0f, lineOnTop ? 1f : 0f), new Vector2(1f, lineOnTop ? 1f : 0f));
        line.sizeDelta = new Vector2(0f, 6f);
        var lineImg = line.gameObject.AddComponent<Image>();
        lineImg.color = Gold;
        lineImg.raycastTarget = false;
        return img;
    }

    public static Text Label(Transform parent, string text, int size, TextAnchor align = TextAnchor.MiddleCenter, Vector2? min = null, Vector2? max = null)
    {
        var rt = Rect("Text", parent, min ?? Vector2.zero, max ?? Vector2.one);
        rt.offsetMin = new Vector2(30f, 8f);
        rt.offsetMax = new Vector2(-30f, -8f);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = UiFont;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = align;
        t.color = Color.white;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // Primary = gold with dark text; secondary = translucent white with a light border
    public static Button Button(Transform parent, string label, bool primary, UnityAction onClick, Vector2 min, Vector2 max)
    {
        var rt = Rect("Button " + label, parent, min, max);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = primary ? Gold : new Color(1f, 1f, 1f, 0.14f);
        if (!primary) rt.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.55f);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(onClick);
        var t = Label(rt, label, 34);
        t.color = primary ? new Color(0.17f, 0.13f, 0.02f) : Color.white;
        t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        return b;
    }

    // Simple banner (kitchen hint): panel across the top with one line of text
    public static GameObject Show(string message, float anchorY = 0.9f, int fontSize = 44)
    {
        var canvas = Canvas("ScreenMessage", 50);
        float h = 0.07f;
        var panel = Panel(canvas.transform, new Vector2(0.1f, anchorY - h), new Vector2(0.9f, anchorY + h));
        Label(panel.transform, message, fontSize);
        return canvas.gameObject;
    }

    // Centered card: gold title, body lines, optional small footer
    public static GameObject Card(string title, string body, string footer = null, int order = 50)
    {
        var canvas = Canvas("ScreenCard", order);
        var panel = Panel(canvas.transform, new Vector2(0.22f, 0.3f), new Vector2(0.78f, 0.78f), lineOnTop: true);
        var t = Label(panel.transform, title, 60, TextAnchor.MiddleCenter, new Vector2(0f, 0.68f), new Vector2(1f, 1f));
        t.color = Gold;
        Label(panel.transform, body, 40, TextAnchor.MiddleCenter, new Vector2(0f, footer != null ? 0.22f : 0f), new Vector2(1f, 0.7f));
        if (footer != null)
        {
            var f = Label(panel.transform, footer, 30, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0.22f));
            f.fontStyle = FontStyle.Normal;
            f.color = new Color(0.8f, 0.83f, 0.92f);
        }
        return canvas.gameObject;
    }
}
