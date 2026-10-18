using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Forest and river players are small against busy backgrounds; a "YOU" label follows them. UI only.
public class PlayerMarker : MonoBehaviour
{
    Transform target;
    RectTransform label;
    Camera cam;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded += (scene, _) => TryCreate(scene.name);
        TryCreate(SceneManager.GetActiveScene().name);
    }

    static void TryCreate(string scene)
    {
        if (scene != "forest" && scene != "river") return;
        var player = FindAnyObjectByType<PlayerMovementRiver>();
        if (player == null) return;
        new GameObject("PlayerMarker").AddComponent<PlayerMarker>().target = player.transform;
    }

    void Start()
    {
        cam = Camera.main;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var go = new GameObject("Label");
        go.transform.SetParent(transform, false);
        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "YOU\n▼";
        text.fontSize = 34;
        text.fontStyle = FontStyle.Bold;
        text.lineSpacing = 0.8f;
        text.alignment = TextAnchor.LowerCenter;
        text.color = new Color(1f, 0.92f, 0.3f);
        text.raycastTarget = false;
        go.AddComponent<Outline>().effectColor = Color.black;
        label = text.rectTransform;
        label.anchorMin = label.anchorMax = Vector2.zero;
        label.pivot = new Vector2(0.5f, 0f);
        label.sizeDelta = new Vector2(200, 90);
    }

    void LateUpdate()
    {
        if (target == null || cam == null) { Destroy(gameObject); return; }
        var r = target.GetComponentInChildren<SpriteRenderer>();
        Vector3 top = r != null ? new Vector3(r.bounds.center.x, r.bounds.max.y, 0f) : target.position;
        Vector3 sp = cam.WorldToScreenPoint(top);
        var canvasRT = (RectTransform)transform;
        float k = canvasRT.rect.width / Screen.width;
        label.anchoredPosition = new Vector2(sp.x * k, sp.y * k + 6f);
    }
}
