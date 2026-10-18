using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // Add this line
public class UIManager : MonoBehaviour
{
    public Text countdownText;
    public Text gameTimerText;
    public Text messageText;
    public Button restartButton;
    public Button mainMenuButton;
    public Button startButton;
    public bool isForest = false;
        // Add references for screws
    public List<GameObject> darkScrews = new List<GameObject>();
    public List<GameObject> normalScrews = new List<GameObject>();

    public int screwsCollected = 0;
    public int totalScrews = 5;

    void Start()
    {
        // Initially hide message and restart button
        messageText.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);
        mainMenuButton.gameObject.SetActive(false);
        startButton.gameObject.SetActive(false);
        // Debug check for screw lists
        Debug.Log($"Dark screws count: {darkScrews.Count}, Normal screws count: {normalScrews.Count}");
        
        // Initialize screws UI regardless of isForest flag (remove the condition)
        if(isForest) InitializeScrewsUI();
    }

    void InitializeScrewsUI()
    {
        // Validate lists
        if (darkScrews.Count == 0 || normalScrews.Count == 0)
        {
            Debug.LogError("Screw lists not properly assigned in Unity Inspector!");
            return;
        }

        for(int i = 0; i < totalScrews; i++)
        {
            if(i < darkScrews.Count && i < normalScrews.Count)
            {
                darkScrews[i].SetActive(true);
                normalScrews[i].SetActive(false);
                }
        }
    }

    public void CollectScrew()
    {
        if(screwsCollected < totalScrews)
        {
            darkScrews[screwsCollected].gameObject.SetActive(false);
            normalScrews[screwsCollected].gameObject.SetActive(true);
            screwsCollected++;
        }
    }

    public void ShowStartCountdown(float time)
    {
        countdownText.gameObject.SetActive(true);
        countdownText.text = Mathf.Ceil(time).ToString();
    }

    public void UpdateCountdown(float timeLeft)
    {
        if (timeLeft > 0)
        {
            countdownText.text = Mathf.Ceil(timeLeft).ToString();
        }
    }

    public void HideCountdown()
    {
        countdownText.gameObject.SetActive(false);
    }

    public void UpdateGameTimer(float timeLeft)
    {
        gameTimerText.text = "Time: " + Mathf.Ceil(timeLeft).ToString() + "s";
    }

    public void ShowWellDone() => ShowEndPanel("Well done!",
        isForest ? "You caught all 5 screws" : "You crossed the river");

    public void ShowTryAgain() => ShowEndPanel("Nice try!",
        isForest ? $"You caught {screwsCollected} of {totalScrews} screws" : "The river was too strong this time");

    public void ShowTimeUp() => ShowEndPanel("Time's up!",
        isForest ? $"You caught {screwsCollected} of {totalScrews} screws" : "Good effort");

    // Called from several end paths; the first end reason wins.
    // Shared jam style: a centered card with three clear buttons. The scene's small corner buttons stay hidden.
    bool endShown;
    void ShowEndPanel(string title, string subtitle)
    {
        if (endShown) return;
        endShown = true;
        var canvas = ScreenMessage.Canvas("EndCard", 30);
        var panel = ScreenMessage.Panel(canvas.transform, new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.82f), lineOnTop: true);
        var t = ScreenMessage.Label(panel.transform, title, 64, TextAnchor.MiddleCenter, new Vector2(0f, 0.8f), new Vector2(1f, 1f));
        t.color = ScreenMessage.Gold;
        ScreenMessage.Label(panel.transform, subtitle, 38, TextAnchor.MiddleCenter, new Vector2(0f, 0.64f), new Vector2(1f, 0.8f));
        ScreenMessage.Button(panel.transform, "Play again", true, RestartGame, new Vector2(0.15f, 0.43f), new Vector2(0.85f, 0.57f));
        ScreenMessage.Button(panel.transform, "Back to the map", false, GoToStart, new Vector2(0.15f, 0.25f), new Vector2(0.85f, 0.39f));
        ScreenMessage.Button(panel.transform, "Main menu", false, GoToMainMenu, new Vector2(0.15f, 0.07f), new Vector2(0.85f, 0.21f));
    }

    bool restarting;
    void RestartGame()
    {
        if (restarting) return;
        restarting = true;
        if (GameController.Instance != null) GameController.Instance.RestartGame();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GoToMainMenu()
    {
        if (GameController.Instance != null) GameController.Instance.GoToMainMenu();
        else SceneManager.LoadScene("main");
    }

    void GoToStart()
    {
        if (GameController.Instance != null) GameController.Instance.GoToStart();
        else SceneManager.LoadScene("Start");
    }
}
