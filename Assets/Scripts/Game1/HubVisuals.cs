using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Hub (Start scene) visual polish for the jam. Visual only: no colliders, physics or logs change.
// - Sky backdrop: day -> night (around the dwarf's house) -> day (near the river), from Resources/Sky/sky_day|sky_night.
//   Missing images = the old plain sky, so the build never depends on them.
// - Edge guide arrow pointing to the next entrance that is not cleared yet, then to the spaceship.
// - The old static red arrow sprites are hidden (they always pointed right).
// - On the bottom floor the player's drawing is lowered a little: the floor collider sits above the drawn ground.
public class HubVisuals : MonoBehaviour
{
    // Hub x positions (see docs/TAKEOVER): forest gate ~27, house ~49, river ~117, spaceship ~125
    const float NightStart = 36f, NightFull = 46f, NightEnd = 96f, DayAgain = 108f;
    const float SkyParallax = 0.03f;
    const float FloorY = -3.0f, FeetDrop = 0.35f; // ponytail: hand-tuned for the Start floor; adjust if the art moves

    Camera cam;
    Transform player;
    SpriteRenderer day, night;
    readonly System.Collections.Generic.List<SpriteRenderer> sceneBackdrops = new();
    static readonly Color NightTint = new Color(0.38f, 0.42f, 0.68f, 1f);
    SpriteRenderer playerRenderer, playerVisual;
    RectTransform arrow;
    Text arrowText;
    (string gate, string label, Transform t)[] targets;
    Transform ship;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded += (scene, _) => { if (scene.name == "Start") new GameObject("HubVisuals").AddComponent<HubVisuals>(); };
        if (SceneManager.GetActiveScene().name == "Start") new GameObject("HubVisuals").AddComponent<HubVisuals>();
    }

    void Start()
    {
        cam = Camera.main;
        var pm = FindAnyObjectByType<PlayerMovementGame>();
        player = pm != null ? pm.transform : null;

        HideOldArrows();
        BuildSky();
        BuildFeetVisual();
        BuildGuide();
    }

    static void HideOldArrows()
    {
        foreach (var r in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            if (r.sprite != null && r.sprite.texture != null && r.sprite.texture.name.StartsWith("arrow-red"))
                r.enabled = false;
    }

    // ---------------- sky ----------------
    void BuildSky()
    {
        if (cam == null) return;
        day = SkyLayer("SkyDay", Resources.Load<Texture2D>("Sky/sky_day"), -32000);
        night = SkyLayer("SkyNight", Resources.Load<Texture2D>("Sky/sky_night"), -31999);
        if (night != null) night.color = new Color(1f, 1f, 1f, 0f);
        // BG_3 is an opaque pale-sky layer that would hide the forest backdrop
        if (day != null)
            foreach (var r in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (r.sprite != null && r.sprite.texture != null && r.sprite.texture.name.StartsWith("BG_3"))
                    r.enabled = false;
        // The painted layers around the house (BG_1, BG_2) are opaque, so they get the night as a tint
        foreach (var r in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            if (r.enabled && r.sprite != null && r.sprite.texture != null
                && (r.sprite.texture.name.StartsWith("BG_1") || r.sprite.texture.name.StartsWith("BG_2")))
                sceneBackdrops.Add(r);
    }

    SpriteRenderer SkyLayer(string name, Texture2D tex, int order)
    {
        if (tex == null) return null;
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        var r = new GameObject(name).AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.sortingOrder = order;
        // Cover the view with a little margin for the parallax drift
        float viewH = cam.orthographicSize * 2f, viewW = viewH * cam.aspect;
        float scale = Mathf.Max(viewH * 1.08f / (tex.height / 100f), (viewW + 8f) / (tex.width / 100f));
        r.transform.localScale = Vector3.one * scale;
        return r;
    }

    float NightAmount(float x)
    {
        if (x < NightStart || x > DayAgain) return 0f;
        if (x < NightFull) return Mathf.InverseLerp(NightStart, NightFull, x);
        if (x > NightEnd) return 1f - Mathf.InverseLerp(NightEnd, DayAgain, x);
        return 1f;
    }

    // ---------------- feet ----------------
    void BuildFeetVisual()
    {
        if (player == null) return;
        playerRenderer = player.GetComponent<SpriteRenderer>();
        if (playerRenderer == null) return;
        // A copy that follows the real renderer; the real one is hidden but keeps its collider and animation
        var go = new GameObject("PlayerVisual");
        go.transform.SetParent(player, false);
        playerVisual = go.AddComponent<SpriteRenderer>();
        playerVisual.sortingLayerID = playerRenderer.sortingLayerID;
        playerVisual.sortingOrder = playerRenderer.sortingOrder;
        playerVisual.sharedMaterial = playerRenderer.sharedMaterial;
        playerRenderer.enabled = false;
    }

    // ---------------- guide ----------------
    void BuildGuide()
    {
        targets = new (string, string, Transform)[]
        {
            ("terrain", "Forest", FindAnyObjectByType<TerrainTrigger2D>()?.transform),
            ("house", "Dwarf's house", FindAnyObjectByType<HouseTrigger2D>()?.transform),
            ("rock", "River", FindAnyObjectByType<RockTrigger2D>()?.transform),
        };
        ship = FindAnyObjectByType<SpaceShip>()?.transform;

        var canvas = ScreenMessage.Canvas("GuideCanvas", 8);
        arrow = ScreenMessage.Rect("Guide", canvas.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        arrow.sizeDelta = new Vector2(420f, 110f);
        arrow.gameObject.AddComponent<Image>().color = ScreenMessage.Navy;
        arrowText = ScreenMessage.Label(arrow, "", 46);
    }

    (string label, float x)? NextTarget()
    {
        foreach (var t in targets)
            if (t.t != null && !InstructionManager.IsGateCleared(t.gate)) return (t.label, t.t.position.x);
        if (ship != null) return ("Spaceship", ship.position.x);
        return null;
    }

    void LateUpdate()
    {
        if (cam == null) return;
        float px = player != null ? player.position.x : cam.transform.position.x;

        // Sky follows the camera with a slight drift; night fades in and out with the player's progress
        Vector3 c = cam.transform.position;
        var skyPos = new Vector3(c.x - c.x * SkyParallax, c.y, 50f);
        if (day != null) day.transform.position = skyPos;
        if (night != null)
        {
            night.transform.position = skyPos;
            night.color = new Color(1f, 1f, 1f, NightAmount(px));
        }
        var tint = Color.Lerp(Color.white, NightTint, NightAmount(px));
        foreach (var r in sceneBackdrops) if (r != null) r.color = tint;

        // Player drawing: same sprite and facing as the hidden real renderer
        if (playerVisual != null && playerRenderer != null)
        {
            playerVisual.sprite = playerRenderer.sprite;
            playerVisual.flipX = playerRenderer.flipX;
            playerVisual.color = playerRenderer.color;
            float drop = player.position.y < FloorY ? FeetDrop : 0f;
            float k = player.lossyScale.y != 0f ? 1f / player.lossyScale.y : 1f;
            playerVisual.transform.localPosition = new Vector3(0f, -drop * k, 0f);
        }

        // Guide: shown only while the next place is off screen, on the side it is on
        if (arrow == null) return;
        var next = NextTarget();
        float halfW = cam.orthographicSize * cam.aspect;
        bool show = next.HasValue && Mathf.Abs(next.Value.x - c.x) > halfW * 0.9f;
        if (arrow.gameObject.activeSelf != show) arrow.gameObject.SetActive(show);
        if (!show) return;
        bool right = next.Value.x > c.x;
        float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 5f);
        arrow.localScale = Vector3.one * pulse;
        arrow.anchorMin = arrow.anchorMax = new Vector2(right ? 1f : 0f, 0.5f);
        arrow.pivot = new Vector2(right ? 1f : 0f, 0.5f);
        arrow.anchoredPosition = new Vector2(right ? -20f : 20f, 0f);
        string gold = ScreenMessage.GoldHex;
        arrowText.text = right ? $"{next.Value.label}  <color={gold}>>>></color>" : $"<color={gold}><<<</color>  {next.Value.label}";
    }
}
