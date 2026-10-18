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
        var p = GameObject.FindGameObjectWithTag("Player");
        Debug.Log($"[DevInput] scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} player={(p != null ? p.transform.position.ToString() : "none")}");
    }

    public void Hold(string spec)
    {
        var p = spec.Split(',');
        axisX = float.Parse(p[0], CultureInfo.InvariantCulture);
        axisY = p.Length > 2 ? float.Parse(p[1], CultureInfo.InvariantCulture) : 0f;
        until = Time.time + float.Parse(p[p.Length - 1], CultureInfo.InvariantCulture);
    }
}
