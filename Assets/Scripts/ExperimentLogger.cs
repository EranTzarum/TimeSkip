using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// NOTE: Data model classes (ClickEvent, OutcomeEvent, KeyEvent, SceneEvent, MiniGameEvent, MemoryClickEvent,
// MemoryLevelResult, MinigameSection, Forest* events, River* events, DwarfTrial* events, SceneSection, SessionData)
// were moved to Assets/Scripts/ExperimentLogger/ExperimentDataModels.cs to reduce the size of this file.

public partial class ExperimentLogger : MonoBehaviour
{
    public static ExperimentLogger Instance { get; private set; }

    [Header("Session Info")]
    public string playerName = "";

    [Header("Supabase Settings")]
    [SerializeField] private string supabaseUrl = "https://npgswaexuhfhinxdxrmx.supabase.co";
    [SerializeField] private string supabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im5wZ3N3YWV4dWhmaGlueGR4cm14Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjUzMDc4MjksImV4cCI6MjA4MDg4MzgyOX0.x_scg6fi-dbJ2pKckNXkTUe7j3z2mnsqU1Fr2Emzn24";
    [SerializeField] private string supabaseTable = "session_logs";

    [Header("ML Model API Settings")]
    [Tooltip("HTTP endpoint for the ADHD model /predict API. Example: https://<space-subdomain>.hf.space/predict")]
    [SerializeField] private string modelApiUrl = "https://ithamarspitz-adhd-noise-classifier.hf.space/predict";
    [Tooltip("Optional Bearer token for calling via the HF Router. Leave empty for direct subdomain calls.")]
    [SerializeField] private string modelApiBearerToken = "";
    [Tooltip("If true, will send the computed feature vector to the model API after attempting Supabase save.")]
    [SerializeField] private bool enableModelApiCall = false; // off: HF Space is down and its 20 s timeout delayed the ending; report scores offline

    [Header("Prediction Display")]
    [SerializeField] private bool showPredictionOverlay = false; // never show a likelihood to the child
    [SerializeField] private float predictionShowSeconds = 5f;
    [SerializeField] private int predictionFontSize = 48;
    [SerializeField] private Color predictionTextColor = default;
    [SerializeField] private Color predictionBackdropColor = default;

    private SessionData session;
    private float sessionStartTime;
    private float currentSceneStartTime;
    private Vector3 lastMousePos;
    // Dwarf/minigame tracking
    private string currentMinigameName;
    private float currentMinigameStartTime;
    private string currentMinigameRunId; // unique per minigame run
    // Per-run final outcome guard
    private bool hasFinishedRun = false; // true after a final outcome is logged for the active run
    private readonly System.Collections.Generic.HashSet<string> finishedRunIds = new System.Collections.Generic.HashSet<string>();

    // Scene visit tracking
    private string currentSceneVisitId; // GUID for the active visit
    private string currentSceneNameGrouped; // grouped scene name of the active visit
    private bool hasOpenSceneVisit;

    // Area tracking inside a Unity scene (e.g., forest/river within the same scene)
    private string currentArea; // "forest" | "river" | grouped scene name by default
    private float currentAreaStartTime;
    // Guards for de-duplicating certain river logs
    private int _lastRiverLandFrame = -1;
    private string _lastRiverLandJumpId;

    // limit key scan cost by caching enum values
    private static KeyCode[] allKeyCodes;
    
    // prevent duplicate saves and allow waiting for a single flush
    private bool saveStarted = false;
    // Track key press start times to compute held durations
    private readonly Dictionary<KeyCode, float> _keyPressStartTimes = new Dictionary<KeyCode, float>();
    // Short-term debounce to prevent accidental duplicate non-minigame outcomes
    private readonly Dictionary<string, float> _nonMinigameOutcomeDebounce = new Dictionary<string, float>();
    // 15s rate limiter for non-minigame final outcomes (per area|outcome|reason)
    private const float NonMinigameOutcomeMinInterval = 15f;
    private readonly Dictionary<string, float> _nonMinigameOutcomeLastEmit = new Dictionary<string, float>();

    // Prediction overlay runtime refs
    private Canvas _predCanvas;
    private Text _predText;
    private Image _predBackdrop;
    private Coroutine _predHideRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        // Ensure Supabase is configured with provided values if placeholders were present
        if (string.IsNullOrWhiteSpace(supabaseUrl) || supabaseUrl.Contains("YOUR-PROJECT-REF"))
        {
            supabaseUrl = "https://npgswaexuhfhinxdxrmx.supabase.co";
        }
        if (string.IsNullOrWhiteSpace(supabaseAnonKey) || supabaseAnonKey.Contains("YOUR-ANON"))
        {
            supabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im5wZ3N3YWV4dWhmaGlueGR4cm14Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjUzMDc4MjksImV4cCI6MjA4MDg4MzgyOX0.x_scg6fi-dbJ2pKckNXkTUe7j3z2mnsqU1Fr2Emzn24";
        }
        if (string.IsNullOrWhiteSpace(supabaseTable))
        {
            supabaseTable = "session_logs";
        }
        // Load Hugging Face token from Scripts/hf_token[.txt] if present
        TryLoadHfTokenFromFile();
        // Default colors if not assigned in Inspector
        if (predictionTextColor == default) predictionTextColor = Color.white;
        if (predictionBackdropColor == default) predictionBackdropColor = new Color(0, 0, 0, 0.35f);
        // Model API: keep defaults unless explicitly changed; no-op if URL is empty or disabled.
        StartNewSession();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureLoggerExists()
    {
        if (Instance == null)
        {
            var go = new GameObject("ExperimentLogger");
            go.AddComponent<ExperimentLogger>();
        }
    }

    // Pseudonymous code shown to the clinician; no names are collected or uploaded.
    public string SessionCode { get; private set; }

    static string NewSessionCode()
    {
        const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // no 0/O/1/I/L
        var chars = new char[6];
        for (int i = 0; i < chars.Length; i++) chars[i] = alphabet[UnityEngine.Random.Range(0, alphabet.Length)];
        return new string(chars);
    }

    public void StartNewSession()
    {
        sessionStartTime = Time.time;
        SessionCode = NewSessionCode();
        playerName = SessionCode; // player_name column now holds the pseudonym
        session = new SessionData
        {
            sessionId = Guid.NewGuid().ToString(),
            playerName = playerName,
            startTime = DateTime.UtcNow.ToString("o")
        };

        if (allKeyCodes == null)
        {
            allKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        // Initialize current scene start time and start the first visit for the active scene
        var initialScene = GetGroupedSceneName(SceneManager.GetActiveScene().name);
        EnsureSceneSection(initialScene);
        StartSceneVisit(initialScene, null);

        // Initialize area to the grouped scene name by default
        currentArea = initialScene;
        currentAreaStartTime = Time.time;
    }

    // Load HF token from Assets/Scripts/hf_token or hf_token.txt (preferred),
    // falling back to HF_TOKEN environment variable. Does not log the token value.
    private void TryLoadHfTokenFromFile()
    {
        try
        {
            string scriptsDir = Path.Combine(Application.dataPath, "Scripts");
            string[] candidates = new string[]
            {
                Path.Combine(scriptsDir, "hf_token"),
                Path.Combine(scriptsDir, "hf_token.txt")
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    string token = File.ReadAllText(path, Encoding.UTF8).Trim();
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        modelApiBearerToken = token;
                        Debug.Log("[ExperimentLogger] Loaded Hugging Face token from Assets/Scripts/hf_token file.");
                        return;
                    }
                }
            }

            // Fallback: environment variable
            string envToken = Environment.GetEnvironmentVariable("HF_TOKEN");
            if (!string.IsNullOrWhiteSpace(envToken))
            {
                modelApiBearerToken = envToken.Trim();
                Debug.Log("[ExperimentLogger] Loaded Hugging Face token from HF_TOKEN environment variable.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ExperimentLogger] Failed loading HF token: {ex.Message}");
        }
    }

    public void SetPlayerName(string name, bool applyToCurrentSession = true)
    {
        playerName = name ?? string.Empty;
        if (applyToCurrentSession && session != null)
        {
            session.playerName = playerName;
        }
    }

    // ===== Prediction overlay UI =====
    private void EnsurePredictionUi()
    {
        if (_predCanvas != null) return;

        // Root canvas
        var goCanvas = new GameObject("PredictionCanvas");
        DontDestroyOnLoad(goCanvas);
        _predCanvas = goCanvas.AddComponent<Canvas>();
        _predCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _predCanvas.sortingOrder = 5000; // on top
        goCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        goCanvas.AddComponent<GraphicRaycaster>();

        // Backdrop panel stretches full screen
        var goPanel = new GameObject("Backdrop");
        goPanel.transform.SetParent(goCanvas.transform, false);
        _predBackdrop = goPanel.AddComponent<Image>();
        _predBackdrop.color = predictionBackdropColor;
        var rtPanel = goPanel.GetComponent<RectTransform>();
        rtPanel.anchorMin = Vector2.zero;
        rtPanel.anchorMax = Vector2.one;
        rtPanel.offsetMin = Vector2.zero;
        rtPanel.offsetMax = Vector2.zero;

        // Centered text
        var goText = new GameObject("PredictionText");
        goText.transform.SetParent(goPanel.transform, false);
        _predText = goText.AddComponent<Text>();
        _predText.alignment = TextAnchor.MiddleCenter;
        _predText.fontSize = predictionFontSize;
        _predText.color = predictionTextColor;
        _predText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _predText.verticalOverflow = VerticalWrapMode.Truncate;
        // Ensure the text always fits within the panel
        _predText.resizeTextForBestFit = true;
        _predText.resizeTextMinSize = Mathf.Max(12, Mathf.RoundToInt(predictionFontSize * 0.4f));
        _predText.resizeTextMaxSize = Mathf.Max(predictionFontSize, 18);
        _predText.supportRichText = false;
        // Built-in default font
        _predText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var rtText = goText.GetComponent<RectTransform>();
        rtText.anchorMin = new Vector2(0.1f, 0.35f);
        rtText.anchorMax = new Vector2(0.9f, 0.65f);
        rtText.offsetMin = Vector2.zero;
        rtText.offsetMax = Vector2.zero;

        goCanvas.SetActive(false);
    }

    private void HidePredictionUi()
    {
        if (_predCanvas != null)
        {
            _predCanvas.gameObject.SetActive(false);
        }
    }

    private IEnumerator HidePredictionRoutine(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2) || Input.touchCount > 0)
                break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        HidePredictionUi();
        _predHideRoutine = null;
    }

    private void ShowPredictionOnScreen(string text)
    {
        if (!showPredictionOverlay) return;
        if (string.IsNullOrWhiteSpace(text)) return;

        EnsurePredictionUi();
        if (_predText != null)
        {
            _predText.text = text;
            _predText.fontSize = predictionFontSize;
            _predText.color = predictionTextColor;
            _predText.resizeTextForBestFit = true;
            _predText.resizeTextMinSize = Mathf.Max(12, Mathf.RoundToInt(predictionFontSize * 0.4f));
            _predText.resizeTextMaxSize = Mathf.Max(predictionFontSize, 18);
        }
        if (_predBackdrop != null)
        {
            _predBackdrop.color = predictionBackdropColor;
        }

        if (_predCanvas != null)
        {
            _predCanvas.gameObject.SetActive(true);
        }

        if (_predHideRoutine != null) StopCoroutine(_predHideRoutine);
        _predHideRoutine = StartCoroutine(HidePredictionRoutine(Mathf.Max(0.5f, predictionShowSeconds)));
    }

    // Global click logging is disabled per requirement. Only MemoryGame clicks are tracked via dedicated API below.
    public void LogClick(string area, string target, Vector2 position, string inputType = "mouseLeft") { /* no-op */ }

    public void SetArea(string area)
    {
        // Normalize and apply only known areas; fallback to grouped scene name
        string grouped = GetGroupedSceneName(SceneManager.GetActiveScene().name);
        string norm = string.IsNullOrWhiteSpace(area) ? grouped : area.Trim().ToLowerInvariant();
        if (norm != "forest" && norm != "river")
        {
            norm = grouped;
        }
        if (!string.Equals(currentArea, norm, StringComparison.Ordinal))
        {
            currentArea = norm;
            currentAreaStartTime = Time.time;
        }
    }

    public string GetCurrentArea()
    {
        return string.IsNullOrEmpty(currentArea) ? GetGroupedSceneName(SceneManager.GetActiveScene().name) : currentArea;
    }

    public void LogOutcome(string area, string outcome, string reason = "", int livesLeft = 0, string minigame = null)
    {
        if (session == null) return;

        // Determine minigame context and minigame-relative time
        string mg = GetEffectiveMinigameName(minigame);
        // Compute relative time if any minigame is currently active (start time captured once when playable)
        float tMinigame = (!string.IsNullOrEmpty(currentMinigameName) && currentMinigameStartTime > 0f)
            ? (Time.time - currentMinigameStartTime)
            : 0f;
        string sceneName = GetGroupedSceneName(SceneManager.GetActiveScene().name);

        // Final-outcome guard: prevent duplicate final outcomes within the same run
        bool isFinal = false;
        if (!string.IsNullOrEmpty(outcome))
        {
            string o = outcome.ToLowerInvariant();
            isFinal = (o == "win" || o == "lose" || o == "fail" || o.StartsWith("minigame_final_"));
        }
        if (isFinal)
        {
            // If we are inside an active minigame run, use run id to gate duplicates
            if (!string.IsNullOrEmpty(currentMinigameRunId))
            {
                if (finishedRunIds.Contains(currentMinigameRunId))
                {
                    return; // already finished for this run
                }
            }
            else
            {
                // Non-minigame (e.g., forest/river) — allow multiple finals across runs.
                // Apply a 15s rate limit per (area|outcome|reason) and a tiny debounce to avoid bursts.
                try
                {
                    string areaKey = (GetCurrentArea() ?? string.Empty).ToLowerInvariant();
                    string outcomeKey = (outcome ?? string.Empty).ToLowerInvariant();
                    string reasonKey = (reason ?? string.Empty).ToLowerInvariant();
                    string throttleKey = areaKey + "|" + outcomeKey + "|" + reasonKey;

                    // 15s rate limit
                    if (_nonMinigameOutcomeLastEmit.TryGetValue(throttleKey, out var lastFinalT))
                    {
                        if (Time.time - lastFinalT < NonMinigameOutcomeMinInterval)
                        {
                            return; // suppress if within 15s window
                        }
                    }

                    // Tiny debounce (250ms) as safety for double-callbacks
                    string debounceKey = throttleKey; // reuse same key
                    if (_nonMinigameOutcomeDebounce.TryGetValue(debounceKey, out var lastT))
                    {
                        if (Time.time - lastT < 0.25f)
                        {
                            return; // suppress duplicate within 250ms window
                        }
                    }
                    _nonMinigameOutcomeDebounce[debounceKey] = Time.time;
                    _nonMinigameOutcomeLastEmit[throttleKey] = Time.time;
                }
                catch { }
            }
        }

        var e = new OutcomeEvent
        {
            time = DateTime.UtcNow.ToString("o"),
            timeSinceStart = R2(Time.time - sessionStartTime),
            timeSinceSceneStart = R2(Time.time - currentSceneStartTime),
            timeSinceMinigameStart = R2(tMinigame),
            // Use current area label instead of Unity scene when available
            scene = string.IsNullOrEmpty(currentArea) ? sceneName : currentArea,
            area = area,
            outcome = outcome,
            reason = reason
        };
        // Attach player position except in dwarf/memory grouped scenes
        try
        {
            if (ShouldAttachPlayerPos() && TryGetPlayerPos(out var p))
            {
                e.playerPos = p;
            }
        }
        catch { }
        // Store under the area section for forest/river; otherwise store under grouped scene
        string sectionSceneName = sceneName;
        if (!string.IsNullOrEmpty(area))
        {
            string a = area.Trim().ToLowerInvariant();
            if (a == "forest" || a == "river")
            {
                sectionSceneName = GetCurrentArea();
            }
        }
        var section = EnsureSceneSection(sectionSceneName);
        section.outcomes.Add(e);
        if (!string.IsNullOrEmpty(mg))
        {
            EnsureMinigameSection(section, mg).outcomes.Add(e);
        }

        // Mark run as finished only for active minigame runs (Dwarf/Memory)
        if (isFinal && !string.IsNullOrEmpty(currentMinigameRunId))
        {
            hasFinishedRun = true;
            finishedRunIds.Add(currentMinigameRunId);
        }
    }

    // Convenience helpers for common gameplay results
    public void LogForestWin()
    {
        LogOutcome("forest", "win", "collected_5_screws");
    }

    public void LogForestFailure(string reason = "touched_berry")
    {
        LogOutcome("forest", "fail", reason);
    }

    public void LogRiverFailure(string reason = "touched_fish")
    {
        // Route by current in-scene area: if we are actually in Forest (shared components), log as Forest
        string area = "River";
        try
        {
            string cur = GetCurrentArea();
            if (string.Equals(cur, "forest", StringComparison.OrdinalIgnoreCase))
            {
                area = "Forest";
            }
        }
        catch { }
        LogOutcome(area, "fail", reason);
    }

    public void LogDwarfResult(bool win, string reason = "")
    {
        // Preserve compatibility: include current minigame if any
        LogOutcome("dwarf", win ? "win" : "fail", reason, 0, currentMinigameName);
    }

    // Removed from core: Scene management, Input logging, Minigame-specific logging, and
    // persistence/export methods were moved into partial class files under Assets/Scripts/ExperimentLogger/.
}