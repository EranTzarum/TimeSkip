using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// INFO and STATISTICS screens for the main menu, built at runtime (no extra scenes to merge).
public class MenuOverlay : MonoBehaviour
{
    static MenuOverlay open;

    // Set by the ending card: the main menu opens straight on STATISTICS
    public static bool OpenStatisticsOnLoad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, _) =>
        {
            if (scene.name != "main" || !OpenStatisticsOnLoad) return;
            OpenStatisticsOnLoad = false;
            ShowStatistics();
        };
    }

    public static void ShowInfo() => Show("About Time Skip", InfoText());
    public static void ShowStatistics() => Show("Statistics (clinicians & researchers)", StatisticsText());

    static void Show(string title, string body)
    {
        if (open != null) Destroy(open.gameObject);
        var go = new GameObject("MenuOverlay");
        open = go.AddComponent<MenuOverlay>();
        open.Build(title, body);
    }

    void Update()
    {
        // ENTER also closes, so the ending flow is ENTER (statistics), ENTER (main menu)
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            Destroy(gameObject);
    }

    void Build(string title, string body)
    {
        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        var dim = Rect("Dim", transform, Vector2.zero, Vector2.one);
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        var panel = Rect("Panel", transform, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.94f));
        panel.gameObject.AddComponent<Image>().color = new Color(0.09f, 0.17f, 0.10f, 0.97f);

        var titleText = Label(Rect("Title", panel, new Vector2(0.03f, 0.88f), new Vector2(0.97f, 0.98f)), title, 54, TextAnchor.MiddleLeft);
        titleText.fontStyle = FontStyle.Bold;

        // Scrollable body
        var view = Rect("Viewport", panel, new Vector2(0.03f, 0.13f), new Vector2(0.97f, 0.87f));
        view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
        view.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        var content = Rect("Content", view, new Vector2(0f, 1f), new Vector2(1f, 1f));
        content.pivot = new Vector2(0.5f, 1f);
        var bodyText = Label(content, body, 30, TextAnchor.UpperLeft);
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        content.offsetMin = new Vector2(24, 0);
        content.offsetMax = new Vector2(-24, 0);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 40f;

        var close = Rect("Close", panel, new Vector2(0.40f, 0.02f), new Vector2(0.60f, 0.10f));
        close.gameObject.AddComponent<Image>().color = new Color(0.95f, 0.78f, 0.25f);
        close.gameObject.AddComponent<Button>().onClick.AddListener(() => Destroy(gameObject));
        // A GameObject holds one Graphic, so the label goes on a child of the Image
        var closeLabel = Label(Rect("Label", close, Vector2.zero, Vector2.one), "CLOSE (Enter)", 34, TextAnchor.MiddleCenter);
        closeLabel.color = Color.black;
    }

    static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    static Text Label(RectTransform rt, string text, int size, TextAnchor anchor)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        // Built-in font has no en dash or bullet glyphs
        t.text = text.Replace("–", "-").Replace("•", "-");
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Color.white;
        t.supportRichText = true;
        return t;
    }

    // ---------------- INFO ----------------
    static string InfoText() =>
@"<b>What is Time Skip?</b>
A short story game for children aged 10–12. The child helps fix a broken time machine: collects screws in the magic forest, remembers what was in the dwarf's house, and crosses the river. While the child plays, the game quietly records <i>how</i> they play. There is no questionnaire and no score shown to the child.

<b>How to play</b>
• Arrow keys move, Space jumps (one extra jump in the air).
• At each glowing spot press <b>ENTER</b> to start that mini-game. To skip a mini-game, jump 3 times there.
• Touch the spaceship at the end to finish the session.

<b>What the game observes</b>
• <b>Forest</b> (response inhibition / impulsivity): collect screws, avoid look-alike poisonous berries (a Go/No-Go task). Time to reach each screw and how much it varies (attention consistency). Key-press rate and rhythm (motor activity).
• <b>Dwarf's house</b> (visual working memory): a room picture for 5 seconds, then ""which of these did you see?"". Correct answers, mistakes and response time.
• <b>River</b> (action restraint, planning): jump between rocks and do not jump when a fish is there. Collisions, leaving the screen, key-press dynamics.
• Skipping a mini-game is itself a behaviour (avoidance).
Each session becomes 23 behavioural measures. A research model turns them into a signal that only a clinician sees.

<b>Clinical basis</b>
Barkley's model (response inhibition as a core difficulty in ADHD), Baddeley's working-memory model, reaction-time variability as a robust ADHD marker (Kofler et al., 2013, 319 studies), and stealth assessment (Shute). The game flow was designed with occupational therapy students.

<b>Privacy</b>
No names: each session gets a random code. The full session stays on this device; only summary numbers are uploaded. No camera, no microphone.

<b>Important</b>
Research and screening prototype. <b>Not a medical device and not a diagnosis.</b> Results must be interpreted by a qualified clinician.

<b>Team</b>
Eran Tzarum and Itamar Shpitzer (development, ML). Michal Leff-Cohen, Hila Buchbut, Shira Shalit (occupational therapy). Supervisor: Prof. Erel Segal-Halevi, Ariel University.
Sheba ARC Healthcare Game Jam 2026.";

    // ---------------- STATISTICS ----------------
    static string StatisticsText()
    {
        var sb = new StringBuilder();
        var sessions = LoadSessions();

        // Clinician view: the model's result for the last round, big, before the details
        if (sessions.Count > 0)
        {
            var last = sessions[0];
            sb.AppendLine($"<b>Last round ({last.When:dd/MM HH:mm}, code {last.Code})</b>");
            if (last.ModelP.HasValue)
            {
                double p = last.ModelP.Value;
                bool flagged = p >= 0.5; // the model's own predict() threshold
                string color = flagged ? "#FF8A65" : "#81C784";
                sb.AppendLine($"<size=44><b>ADHD model signal: <color={color}>{p * 100:0}%</color></b></size>");
                sb.AppendLine(flagged ? "Model prediction: <b>ADHD pattern</b> (signal 50% or more)" : "Model prediction: <b>no ADHD pattern</b> (signal below 50%)");
            }
            else
                sb.AppendLine("<size=32><b>ADHD model signal: not available</b></size>\nThe model did not answer for this round (offline or the service was asleep).");
            sb.AppendLine();
        }

        sb.AppendLine("<b>Sessions on this device</b>");
        if (sessions.Count == 0)
        {
            sb.AppendLine("No sessions yet. Play through to the spaceship to record one.");
        }
        else
        {
            sb.AppendLine($"{sessions.Count} session(s) recorded.\n");
            foreach (var s in sessions.Take(8))
            {
                sb.Append($"<b>{s.When:dd/MM HH:mm}</b>  code {s.Code}  |  ");
                sb.Append($"forest: {Get(s, "screws_collected")}/{Get(s, "screws_spawned")} screws, {Get(s, "forest_fish_collisions")} hits  |  ");
                sb.Append($"memory: {Get(s, "dwarf_correct")} correct, {Get(s, "dwarf_incorrect")} wrong  |  ");
                sb.Append($"river: {(Get(s, "river_win") > 0 ? "crossed" : "not crossed")}, {Get(s, "river_fish_collisions")} hits");
                sb.AppendLine(s.ModelP.HasValue ? $"  |  model signal: {s.ModelP.Value:0.00}" : "  |  model signal: n/a");
            }
            sb.AppendLine("\n<b>Averages across these sessions</b>");
            foreach (var (key, label) in new[]
            {
                ("target_rt_median", "Median time to reach a screw (s)"),
                ("target_rt_cv", "Variability of that time (CV)"),
                ("forest_keys", "Key presses in the forest"),
                ("forest_key_gap_cv", "Key rhythm variability, forest (CV)"),
                ("dwarf_wm_rt_median", "Memory response time (s)"),
                ("dwarf_correct", "Memory correct answers"),
                ("river_keys", "Key presses in the river"),
                ("river_out_of_bounds", "River: left the screen"),
            })
            {
                double avg = sessions.Average(s => Get(s, key));
                sb.AppendLine($"• {label}: {avg.ToString("0.##", CultureInfo.InvariantCulture)}");
            }
            var withP = sessions.Where(s => s.ModelP.HasValue).ToList();
            if (withP.Count > 0)
                sb.AppendLine($"• Model signal, average of {withP.Count}: {withP.Average(s => s.ModelP.Value):0.00}");
        }

        sb.AppendLine("\n<b>The model</b>");
        sb.AppendLine("Logistic regression on the 23 behavioural measures, hosted as a web service. It returns a probability-like signal, never shown to the child.");
        sb.AppendLine("Results reported in the project book (pilot, held-out test of 20 sessions):");
        sb.AppendLine("• Accuracy 85% (17 of 20)");
        sb.AppendLine("• Sensitivity 100% (10 of 10)   • Specificity 70% (7 of 10)");
        sb.AppendLine("• Pilot cohort: 104 children, grades 3–6, one school, with parental consent.");
        sb.AppendLine("<b>Limitations:</b> very small test set, single site, groups tested on different days, decision threshold not yet calibrated. These numbers show feasibility only.");
        sb.AppendLine("\n<b>Not a medical device and not a diagnosis.</b> Interpret with clinical judgement.");
        return sb.ToString();
    }

    class Session
    {
        public DateTime When;
        public string Code = "-";
        public Dictionary<string, double> M = new Dictionary<string, double>();
        public double? ModelP;
    }

    static double Get(Session s, string key) => s.M.TryGetValue(key, out var v) ? v : 0;

    static List<Session> LoadSessions()
    {
        var list = new List<Session>();
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "session_logs");
            if (!Directory.Exists(dir)) return list;
            foreach (var path in Directory.GetFiles(dir, "*.json").Where(p => !p.EndsWith(".prediction.json")))
            {
                var s = new Session { When = File.GetLastWriteTime(path) };
                string text = File.ReadAllText(path);
                string header = text.Split('\n')[0];
                foreach (Match m in Regex.Matches(header, @"(\w+)=([-\d.]+)"))
                    s.M[m.Groups[1].Value] = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                var code = Regex.Match(text, "\"playerName\"\\s*:\\s*\"([^\"]*)\"");
                if (code.Success && code.Groups[1].Value.Length > 0) s.Code = code.Groups[1].Value;
                string predPath = Path.ChangeExtension(path, ".prediction.json");
                if (File.Exists(predPath))
                {
                    var p = Regex.Match(File.ReadAllText(predPath), @"""probabilities""\s*:\s*\[\s*\[\s*[-\d.eE+]+\s*,\s*([-\d.eE+]+)");
                    if (p.Success) s.ModelP = double.Parse(p.Groups[1].Value, CultureInfo.InvariantCulture);
                }
                list.Add(s);
            }
        }
        catch (Exception ex) { Debug.LogWarning($"[MenuOverlay] Could not read sessions: {ex.Message}"); }
        return list.OrderByDescending(s => s.When).ToList();
    }
}
