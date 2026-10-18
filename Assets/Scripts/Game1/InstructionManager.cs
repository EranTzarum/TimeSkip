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
                ShowStartCard("The magic forest", $"Catch 5 falling screws\nUse {ScreenMessage.Key("LEFT")} {ScreenMessage.Key("RIGHT")} to move, {ScreenMessage.Key("SPACE")} to jump\nStay on the screen - the forest keeps moving");
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
                ShowStartCard("The river", $"Jump from rock to rock to cross\nUse {ScreenMessage.Key("LEFT")} {ScreenMessage.Key("RIGHT")} to move, {ScreenMessage.Key("SPACE")} to jump\nWatch out for the piranhas");
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
                instructionText.text = $"Press {ScreenMessage.Key("SPACE")} to jump";
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
            ShowProgress();
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
                    instructionText.text = $"Use {ScreenMessage.Key("LEFT")} and {ScreenMessage.Key("RIGHT")} to walk";
                }
                break;

            case InstructionState.waitingForAnyKey:
                // Wait for any key press
                if (Input.anyKeyDown)
                {
                    currentState = InstructionState.Completed;
                    instructionText.text = "";
                    if (startCard != null) Destroy(startCard);
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
    private static readonly HashSet<string> visitedGates = new HashSet<string>(); // entered with ENTER (not skipped)
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
        string action = gateInfo[gate].action;
        instructionText.text = clearedGates.Contains(gate)
            ? $"Press {ScreenMessage.Key("ENTER")} to {action}"
            : $"Press {ScreenMessage.Key("ENTER")} to {action}\n<size=32><color=#D8DCEF>Not now? Jump 3 times with {ScreenMessage.Key("SPACE")}</color></size>";
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
            visitedGates.Add(activeGate);
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
                instructionText.text = "Skipped - you can keep going";
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

    // Instruction banner: screen-space panel at the top in the shared jam style, so the text never covers the player.
    // (Was a magenta world-space text in the middle of the screen.)
    Image bannerPanel;
    GameObject startCard;

    void SetupCanvasAndText()
    {
        var canvas = ScreenMessage.Canvas("InstructionCanvas", 10);
        bannerPanel = ScreenMessage.Panel(canvas.transform, new Vector2(0.14f, 0.81f), new Vector2(0.86f, 0.97f));
        instructionText = ScreenMessage.Label(bannerPanel.transform, "", 46);
    }

    // Forest/river intro: a centered card instead of the banner; any key still starts the game
    void ShowStartCard(string title, string body)
    {
        instructionText.text = "";
        startCard = ScreenMessage.Card(title, body, $"Press {ScreenMessage.Key("SPACE")} to start", 20);
    }

    // Hub progress, bottom-left: adventures the child entered with ENTER. UI only.
    void ShowProgress()
    {
        // Built-in font has no circle glyphs, so done = green, not yet = grey
        string Item(string gate, string name) => visitedGates.Contains(gate)
            ? $"<color=#7CFC7C>{name} - done</color>" : $"<color=#B8BCC8>{name}</color>";
        var canvas = ScreenMessage.Canvas("ProgressCanvas", 9);
        var panel = ScreenMessage.Panel(canvas.transform, new Vector2(0.01f, 0.015f), new Vector2(0.56f, 0.085f));
        ScreenMessage.Label(panel.transform,
            $"Time machine:   {Item("terrain", "Forest")}    {Item("house", "Dwarf's house")}    {Item("rock", "River")}", 30, TextAnchor.MiddleLeft);
    }

    // Hide the panel whenever there is nothing to say
    void LateUpdate()
    {
        if (bannerPanel == null || instructionText == null) return;
        bool show = !string.IsNullOrEmpty(instructionText.text);
        if (bannerPanel.gameObject.activeSelf != show) bannerPanel.gameObject.SetActive(show);
    }
}
