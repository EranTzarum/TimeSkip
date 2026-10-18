using UnityEngine;
using UnityEngine.UI;

// Runtime on-screen banner (no scene setup needed). UI only.
public static class ScreenMessage
{
    public static GameObject Show(string message, float anchorY = 0.9f, int fontSize = 44)
    {
        var canvasGo = new GameObject("ScreenMessage");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(canvasGo.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = message;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        textGo.AddComponent<Outline>().effectColor = Color.black;
        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, anchorY);
        rt.sizeDelta = new Vector2(1600, 200);
        return canvasGo;
    }
}
