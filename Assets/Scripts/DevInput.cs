using System.Globalization;
using UnityEngine;

// Test hook for automated playtests of the WebGL build: browser key taps are too short to count as "held",
// so page JS holds a direction with unityInstance.SendMessage("DevInput", "Hold", "1,2.5") (x direction, seconds).
// Optional third value is y: "0,1,1.5" holds up for 1.5 s (top-down rooms).
public class DevInput : MonoBehaviour
{
    static float axisX, axisY, until;

    static bool jumpQueued;

    public static float Horizontal(float real) => Time.time < until ? axisX : real;

    public static float Vertical(float real) => Time.time < until ? axisY : real;

    // True once per queued jump (page JS: SendMessage("DevInput", "Jump"))
    public static bool ConsumeJump()
    {
        bool j = jumpQueued;
        jumpQueued = false;
        return j;
    }

    public void Jump() => jumpQueued = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        var go = new GameObject("DevInput");
        go.AddComponent<DevInput>();
        DontDestroyOnLoad(go);
    }

    public void Where()
    {
        // Forest/river players are untagged, so fall back to the movement components
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) p = FindAnyObjectByType<PlayerMovementRiver>()?.gameObject;
        if (p == null) p = FindAnyObjectByType<PlayerMovementHome>()?.gameObject;
        var cam = Camera.main;
        var r = p != null ? p.GetComponentInChildren<SpriteRenderer>() : null;
        Debug.Log($"[DevInput] scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} player={(p != null ? p.transform.position.ToString() : "none")} cam={(cam != null ? cam.transform.position.ToString() : "none")} visible={(r != null && r.isVisible)} order={(r != null ? r.sortingLayerName + "/" + r.sortingOrder : "-")} near={Near()}");
    }

    public void Hold(string spec)
    {
        var p = spec.Split(',');
        axisX = float.Parse(p[0], CultureInfo.InvariantCulture);
        axisY = p.Length > 2 ? float.Parse(p[1], CultureInfo.InvariantCulture) : 0f;
        until = Time.time + float.Parse(p[p.Length - 1], CultureInfo.InvariantCulture);
    }

    // Nearby gameplay objects for scripted play: tag:x:y;...  (tags that are not defined in the project are skipped)
    static string Near()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var tag in new[] { "Mushroom", "Screw", "Fish", "RiverBankEnd" })
        {
            GameObject[] found;
            try { found = GameObject.FindGameObjectsWithTag(tag); } catch { continue; }
            foreach (var g in found)
                sb.Append(tag[0]).Append(':').Append(g.transform.position.x.ToString("F1", CultureInfo.InvariantCulture))
                  .Append(':').Append(g.transform.position.y.ToString("F1", CultureInfo.InvariantCulture)).Append(';');
        }
        // River rocks are untagged platforms; they carry RockId
        foreach (var rock in FindObjectsByType<RockId>(FindObjectsSortMode.None))
            sb.Append("K:").Append(rock.transform.position.x.ToString("F1", CultureInfo.InvariantCulture))
              .Append(':').Append(rock.transform.position.y.ToString("F1", CultureInfo.InvariantCulture)).Append(';');
        return sb.ToString();
    }
}
