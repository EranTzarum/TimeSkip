using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    // Singleton instance
    public static GameController Instance { get; private set; }

    [Header("Game Settings")]
    public float startDelay = 5f;
    public float gameDuration = 60f; // 1 minute
    public float minScrollSpeed = 0.5f;
    public float maxScrollSpeed = 2f;
    public float currentScrollSpeed;
    private float timer;
    private bool gameStarted = false;
    private bool gameEnded = false;

    [Header("References")]
    public UIManager uiManager;
    public BackgroundManager backgroundManager; // Reference to BackgroundManager

    // --- NEW: Reference to InstructionManager and a flag for starting the countdown ---
    private InstructionManager instructionManager;
    private bool countdownStarted = false;

    

    void Awake()
    {
        // Implement Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes if necessary
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }

    void Start()
    {
        timer = 0f;
        
        // Find UIManager in scene if reference is lost
        if (uiManager == null)
        {
            uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("UIManager not found in scene!");
                return;
            }
        }

        // --- NEW: Find the InstructionManager in the scene ---
        instructionManager = FindAnyObjectByType<InstructionManager>();
        if (instructionManager == null)
        {
            Debug.LogError("InstructionManager not found in scene!");
        }
        // Note: We no longer call uiManager.ShowStartCountdown() here.
    }

    void Update()
    {
        if (!gameStarted)
        {
            // Wait until the instructions are done before the countdown
            if (instructionManager == null || instructionManager.IsCompleted)
            {
                if (!countdownStarted)
                {
                    // Reset timer and show the countdown only after instructions are complete.
                    timer = 0f;
                    uiManager.ShowStartCountdown(startDelay);
                    countdownStarted = true;
                }

                // Countdown before starting the game
                timer += Time.deltaTime;
                uiManager.UpdateCountdown(startDelay - timer);
                if (timer >= startDelay)
                {
                    StartGame();
                }
            }
        }
        else if (!gameEnded)
        {
            // Game timer
            timer += Time.deltaTime;
            uiManager.UpdateGameTimer(gameDuration - timer);

            // Update speed based on elapsed time
            float normalizedTime = timer / gameDuration;
            currentScrollSpeed = Mathf.Lerp(minScrollSpeed, maxScrollSpeed, normalizedTime);
            
            // Uncomment these lines if you want to update the background and fish spawners with the new speed.
            // if (backgroundManager != null)
            // {
            //     backgroundManager.UpdateSpeed(currentScrollSpeed);
            // }

            // FishSpawner[] fishSpawners = FindObjectsOfType<FishSpawner>();
            // foreach (FishSpawner spawner in fishSpawners)
            // {
            //     spawner.UpdateSpeed(currentScrollSpeed);
            // }

            if (timer >= gameDuration)
            {
                // Timeout used to destroy the player with no UI (soft-lock). UI only: no new log event.
                uiManager.ShowTimeUp();
                EndGame();
            }
        }
    }

    void StartGame()
    {
        gameStarted = true;
        currentScrollSpeed = minScrollSpeed;
        uiManager.HideCountdown();

        // River is not a minigame: do not mark minigame start

        // Start background spawning
        if (backgroundManager != null)
        {
            backgroundManager.StartSpawning(currentScrollSpeed);
            Debug.Log("GameController: Started background spawning.");
        }
        else
        {
            Debug.LogError("BackgroundManager not assigned in GameController.");
        }

        // Start spawning fish
        FishSpawner[] fishSpawners = FindObjectsOfType<FishSpawner>();
        foreach (FishSpawner spawner in fishSpawners)
        {
            spawner.StartSpawning();
            Debug.Log("GameController: Started FishSpawner.");
        }
    }

    public void EndGame()
    {
        if (gameEnded) return; // Prevent multiple calls
        gameEnded = true;

        // River is not a minigame: do not mark minigame end

        // Stop spawning fish
        FishSpawner[] fishSpawners = FindObjectsOfType<FishSpawner>();
        foreach (FishSpawner spawner in fishSpawners)
        {
            spawner.StopSpawning();
            Debug.Log("GameController: Stopped FishSpawner.");
        }

        // Optionally, stop background scrolling if needed
        if (backgroundManager != null)
        {
            backgroundManager.StopSpawning(); // Implement if necessary
            Debug.Log("GameController: Stopped BackgroundManager spawning.");
        }
        // Find and destroy the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Destroy(player);
            Debug.Log("GameController: Player destroyed.");
        }
        else
        {
            Debug.LogWarning("GameController: Player not found during EndGame.");
        }

        // Optionally, display end game UI
    }

    public void RestartGame()
    {
        // Destroy the persistent GameController before loading new scene
        Destroy(gameObject);
        Instance = null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("GameController: Game restarted.");
    }

    public void QuitGame()
    {
        // Destroy the persistent GameController before quitting
        Destroy(gameObject);
        Instance = null;
        Application.Quit();
        Debug.Log("GameController: Game quit.");
    }

    public void GoToMainMenu()
    {
        // Destroy the persistent GameController before loading new scene
        Destroy(gameObject);
        Instance = null;
        SceneManager.LoadScene("main");
        Debug.Log("GameController: Main menu loaded.");
    }

    public void GoToStart()
    {
        // Destroy the persistent GameController before loading new scene
        Destroy(gameObject);
        Instance = null;
        SceneManager.LoadScene("Start");
        Debug.Log("GameController: Start loaded.");
    }
}
