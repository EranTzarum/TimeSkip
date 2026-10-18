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

    public void ShowWellDone() => ShowEndPanel("Well Done!");

    public void ShowTryAgain() => ShowEndPanel("Try Again!");

    public void ShowTimeUp() => ShowEndPanel("Time's up! Good job!");

    // Called from several end paths; listeners are reset so they never stack.
    void ShowEndPanel(string message)
    {
        if (messageText.gameObject.activeSelf) return; // first end reason wins
        // The scene's MessageText is 14pt in a corner and parents two of the buttons, so it can't be moved.
        // Keep it (empty) for the buttons and show the message as a big centered label instead.
        messageText.gameObject.SetActive(true);
        messageText.text = "";
        var label = new GameObject("EndMessage", typeof(RectTransform)).AddComponent<Text>();
        label.transform.SetParent(messageText.canvas.transform, false);
        var rt = label.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.75f);
        rt.sizeDelta = new Vector2(900f, 140f);
        label.font = messageText.font;
        label.text = message;
        label.fontSize = 64;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);
        Wire(restartButton, RestartGame);
        Wire(mainMenuButton, GoToMainMenu);
        Wire(startButton, GoToStart);
    }

    static void Wire(Button button, UnityEngine.Events.UnityAction action)
    {
        button.gameObject.SetActive(true);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    void RestartGame()
    {
        restartButton.interactable = false;
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
