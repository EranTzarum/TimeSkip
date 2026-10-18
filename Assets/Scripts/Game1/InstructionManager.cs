using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class InstructionManager : MonoBehaviour
{
    // Track whether we've shown instructions for the "start," "forest," or "river" scenes
    private static bool startInstructionsShown = false;
    private static bool forestInstructionsShown = false;
    private static bool riverInstructionsShown = false;

    public bool isForest = false;
    public bool isRiver = false;
    public bool isDwarf = false;

    private AudioSource audioSource;
    [SerializeField] private AudioClip soundtrack;
    
    [Header("Instruction Voice Prompt")]
    [SerializeField] private AudioClip instructionVoiceClip; // single clip reused for dwarf/river/forest prompts
    [SerializeField] [Range(0f, 1f)] private float instructionVoiceVolume = 1f;
    
    [Header("Start Prompt Voice (Any-Key/Jump)")]
    [Tooltip("Audio clip to play when showing the initial start instructions (forest/river any-key, start jump)")]
    [SerializeField] private AudioClip startPromptVoiceClip; // plays when initial instruction texts are shown

    private bool PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
            return true;
        }
        return false;
    }

    private void PlayInstructionVoice()
    {
        if (instructionVoiceClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(instructionVoiceClip, Mathf.Clamp01(instructionVoiceVolume));
        }
    }

    private void PlayStartPromptVoice()
    {
        // Prefer the explicit startPromptVoiceClip; fallback to instructionVoiceClip if not assigned
        var clip = startPromptVoiceClip != null ? startPromptVoiceClip : instructionVoiceClip;
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, Mathf.Clamp01(instructionVoiceVolume));
        }
    }


    private enum InstructionState
    {
        waitingForAnyKey,
        WaitingForJump,
        WaitingForArrow,
        WaitingForEnter,  // For house
        WaitingForRock,   // For rock
        WaitingForTerrain, // For terrain
        Completed
    }

    private InstructionState currentState = InstructionState.WaitingForJump;
    private Text instructionText;

    public bool IsCompleted => currentState == InstructionState.Completed;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        // Add AudioSource component if it doesn't exist
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        PlaySound(soundtrack);
        // Make sure we have an EventSystem for UI
        EnsureEventSystem();

        // Set up the Canvas and Text UI
        SetupCanvasAndText();

        // Decide which instructions to show based on flags
        if (isForest)
        {
            // Show forest instructions only if not shown before
            if (!forestInstructionsShown)
            {
                instructionText.text = "Collect the screws to fix the time machine! Avoid the berries, they are poisonous! Press any key to start.";
                // Mark we have shown forest instructions
                forestInstructionsShown = true;
                currentState = InstructionState.waitingForAnyKey;
                // Voice prompt for the forest any-key instruction
                PlayStartPromptVoice();
            }
            else
            {
                // Already shown, skip
                instructionText.text = "";
                currentState = InstructionState.Completed;
            }
        }
        else if (isRiver)
        {
            // Show river instructions only if not shown before
            if (!riverInstructionsShown)
            {
                instructionText.text = "Welcome to the river! Be careful of the piranhas! Press any key to start.";
                // Mark we have shown river instructions
                riverInstructionsShown = true;
                currentState = InstructionState.waitingForAnyKey;
                // Voice prompt for the river any-key instruction
                PlayStartPromptVoice();
            }
            else
            {
                // Already shown, skip
                instructionText.text = "";
                currentState = InstructionState.Completed;
            }
        }
        else if (isDwarf)
        {
            PlayStartPromptVoice();
            // Was left at WaitingForJump, so Space in the memory game showed the arrow-key tutorial
            instructionText.text = "";
            currentState = InstructionState.Completed;
        }
        else
        {
            // Presumably the "Start" scene or some other scene
            if (!startInstructionsShown)
            {
                // Show the original "start" instructions once
                instructionText.text = "Use space key to jump.";
                startInstructionsShown = true;
                currentState = InstructionState.WaitingForJump;
                // Voice prompt for the start jump instruction
                PlayStartPromptVoice();
            }
            else
            {
                // Already shown, skip
                instructionText.text = "";
                currentState = InstructionState.Completed;
            }
            BuildGateWalls();
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case InstructionState.WaitingForJump:
                // Wait for space key to be pressed
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    currentState = InstructionState.WaitingForArrow;
                    instructionText.text = "Use right arrow to go right or left arrow to go left.";
                }
                break;

            case InstructionState.waitingForAnyKey:
                // Wait for any key press
                if (Input.anyKeyDown)
                {
                    currentState = InstructionState.Completed;
                    instructionText.text = "";
                }
                break;

            case InstructionState.WaitingForArrow:
                // Wait for the left or right arrow press
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    currentState = InstructionState.Completed;
                    instructionText.text = "";
                }
                break;

            case InstructionState.Completed:
                // Do nothing; instructions are done
                break;
        }
        UpdateGate();
    }

    // -------------- Hub gates (Start scene) --------------
    // Each minigame entrance blocks the path until the child enters it (Enter) or skips it (3 jumps).
    // The prompt shows once on arrival and the voice repeats every few seconds, instead of
    // re-firing every time the player's collider brushes the trigger edge.
    private static readonly HashSet<string> clearedGates = new HashSet<string>();
    private readonly Dictionary<string, GameObject> gateWalls = new Dictionary<string, GameObject>();
    private readonly List<float> skipJumpTimes = new List<float>();
    private string activeGate, activeGateScene;
    private float nextVoiceAt;
    private const float VoiceRepeatSeconds = 8f, SkipWindowSeconds = 3f;
    private const int SkipJumps = 3;

    private static readonly Dictionary<string, (string scene, string action)> gateInfo = new Dictionary<string, (string, string)>
    {
        { "house", ("MemoryGame", "enter the dwarf's house") },
        { "rock", ("river", "go to the river") },
        { "terrain", ("forest", "go to the forest") },
    };
    // Original trigger + tall GateZone overlap, so count overlaps instead of toggling on every enter/exit
    private readonly Dictionary<string, int> gateOverlaps = new Dictionary<string, int>();

    public void TriggerHouseInstruction() => EnterGate("house");
    public void ClearHouseInstruction() => ExitGate("house");
    public void TriggerRockInstruction() => EnterGate("rock");
    public void ClearRockInstruction() => ExitGate("rock");
    public void TriggerTerrainInstruction() => EnterGate("terrain");
    public void ClearTerrainInstruction() => ExitGate("terrain");

    public void EnterGate(string gate)
    {
        gateOverlaps.TryGetValue(gate, out int n);
        gateOverlaps[gate] = n + 1;
        if (activeGate == gate) return;
        activeGate = gate;
        activeGateScene = gateInfo[gate].scene;
        skipJumpTimes.Clear();
        currentState = InstructionState.Completed; // tutorial steps no longer apply
        instructionText.color = Color.black;
        string action = gateInfo[gate].action;
        instructionText.text = clearedGates.Contains(gate)
            ? $"Press ENTER to {action}."
            : $"Press ENTER to {action}.\n(Or jump 3 times to skip it.)";
        if (Time.time >= nextVoiceAt)
        {
            PlayInstructionVoice();
            nextVoiceAt = Time.time + VoiceRepeatSeconds;
        }
    }

    public void ExitGate(string gate)
    {
        gateOverlaps.TryGetValue(gate, out int n);
        gateOverlaps[gate] = Mathf.Max(0, n - 1);
        if (gateOverlaps[gate] > 0 || activeGate != gate) return;
        activeGate = null;
        instructionText.color = Color.white;
        instructionText.text = "";
    }

    void UpdateGate()
    {
        if (activeGate == null) return;
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ClearGate(activeGate);
            SceneManager.LoadScene(activeGateScene);
            return;
        }
        if (Input.GetKeyDown(KeyCode.Space) && !clearedGates.Contains(activeGate))
        {
            skipJumpTimes.Add(Time.time);
            skipJumpTimes.RemoveAll(t => Time.time - t > SkipWindowSeconds);
            if (skipJumpTimes.Count >= SkipJumps)
            {
                ClearGate(activeGate);
                instructionText.text = "Skipped! You can keep going.";
                return;
            }
        }
        if (Time.time >= nextVoiceAt)
        {
            PlayInstructionVoice();
            nextVoiceAt = Time.time + VoiceRepeatSeconds;
        }
    }

    void ClearGate(string gate)
    {
        clearedGates.Add(gate);
        if (gateWalls.TryGetValue(gate, out var wall) && wall != null) Destroy(wall);
        gateWalls.Remove(gate);
    }

    // Invisible wall just past each entrance (on the side away from the player's start), removed when cleared.
    void BuildGateWalls()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        float startX = player != null ? player.transform.position.x : float.NegativeInfinity;
        void Build(Component trigger, string gate)
        {
            if (trigger == null || clearedGates.Contains(gate)) return;
            var col = trigger.GetComponent<Collider2D>();
            if (col == null) return;
            var b = col.bounds;
            float x = b.center.x >= startX ? b.max.x + 0.3f : b.min.x - 0.3f;
            const float height = 60f; // covers every platform height at this x
            // 3 units thick, extending away from the player: a frame stall (dt up to 0.33 s) tunneled through 0.4
            const float thick = 3f;
            float away = b.center.x >= startX ? 1f : -1f;
            var wall = new GameObject("GateWall_" + gate);
            wall.transform.position = new Vector3(x + away * thick / 2f, b.center.y, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(thick, height);
            gateWalls[gate] = wall;
            // Tall trigger zone from the entrance to the wall, so the prompt shows at any height
            var zone = new GameObject("GateZone_" + gate);
            float zoneMin = Mathf.Min(b.min.x, x), zoneMax = Mathf.Max(b.max.x, x);
            zone.transform.position = new Vector3((zoneMin + zoneMax) / 2f, b.center.y, 0f);
            var zc = zone.AddComponent<BoxCollider2D>();
            zc.isTrigger = true;
            zc.size = new Vector2(zoneMax - zoneMin, height);
            var gz = zone.AddComponent<GateZone>();
            gz.instructionManager = this;
            gz.gate = gate;
            Debug.Log($"[Gate] {gate} trigger {b.center} size {b.size} wall x={x}");
        }
        Build(FindAnyObjectByType<HouseTrigger2D>(), "house");
        Build(FindAnyObjectByType<RockTrigger2D>(), "rock");
        Build(FindAnyObjectByType<TerrainTrigger2D>(), "terrain");
    }

    // Make sure there is an EventSystem in the scene
    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
        }
    }

    // Helper method to set up a Canvas + Text in World Space
    void SetupCanvasAndText()
    {
        // Create a Canvas (World Space)
        GameObject canvasGO = new GameObject("InstructionCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        // If there is a main camera, place the canvas in front of it
        if (Camera.main != null)
        {
            canvasGO.transform.SetParent(Camera.main.transform);
            canvasGO.transform.localPosition = new Vector3(0f, 0f, 2f);
            canvasGO.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // Fallback if no main camera is found
            canvasGO.transform.position = new Vector3(0f, 0f, -9f);
        }
        // Scale down the canvas
        canvasGO.transform.localScale = Vector3.one * 0.01f;

        // Adjust RectTransform size
        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(800, 600);

        // Optional: add CanvasScaler and GraphicRaycaster
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Create a Text object for instructions
        GameObject textGO = new GameObject("InstructionText");
        textGO.transform.SetParent(canvasGO.transform, false);
        instructionText = textGO.AddComponent<Text>();

        // Use LegacyRuntime font instead of Arial
        instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        // Make text bigger
        instructionText.fontSize = 56;
        // Center alignment and color
        instructionText.alignment = TextAnchor.MiddleCenter;
        instructionText.color = Color.magenta;
        // Wrap text properly
        instructionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        instructionText.verticalOverflow = VerticalWrapMode.Overflow;

        // Position the Text in the center of the canvas
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = new Vector2(600, 150);
    }
}
