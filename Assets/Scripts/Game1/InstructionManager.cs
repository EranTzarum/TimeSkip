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

            case InstructionState.WaitingForEnter:
                // House: load "MemoryGame" scene
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    SceneManager.LoadScene("MemoryGame");
                }
                break;

            case InstructionState.WaitingForRock:
                // Rock: load "river" scene
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    SceneManager.LoadScene("river");
                }
                break;

            case InstructionState.WaitingForTerrain:
                // Terrain: load "forest" scene
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    SceneManager.LoadScene("forest");
                }
                break;

            case InstructionState.Completed:
                // Do nothing; instructions are done
                break;
        }
    }

    // -------------- House Triggers --------------
    public void TriggerHouseInstruction()
    {
        if (currentState != InstructionState.WaitingForEnter)
        {
            currentState = InstructionState.WaitingForEnter;
            instructionText.color = Color.black;
            instructionText.text = "Click enter to enter the house.";
            // Voice prompt for Enter instruction (house)
            PlayInstructionVoice();
        }
    }

    public void ClearHouseInstruction()
    {
        if (currentState == InstructionState.WaitingForEnter)
        {
            instructionText.color = Color.white;
            instructionText.text = "";
            currentState = InstructionState.Completed;
        }
    }

    // -------------- Rock Triggers --------------
    public void TriggerRockInstruction()
    {
        if (currentState != InstructionState.WaitingForRock)
        {
            currentState = InstructionState.WaitingForRock;
            instructionText.text = "Click enter to go to the river.";
            // Voice prompt for Enter instruction (river)
            PlayInstructionVoice();
        }
    }

    public void ClearRockInstruction()
    {
        if (currentState == InstructionState.WaitingForRock)
        {
            instructionText.text = "";
            currentState = InstructionState.Completed;
        }
    }

    // -------------- Terrain Triggers --------------
    public void TriggerTerrainInstruction()
    {
        if (currentState != InstructionState.WaitingForTerrain)
        {
            currentState = InstructionState.WaitingForTerrain;
            instructionText.text = "Click enter to go to the forest.";
            // Voice prompt for Enter instruction (forest)
            PlayInstructionVoice();
        }
    }

    public void ClearTerrainInstruction()
    {
        if (currentState == InstructionState.WaitingForTerrain)
        {
            instructionText.text = "";
            currentState = InstructionState.Completed;
        }
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
