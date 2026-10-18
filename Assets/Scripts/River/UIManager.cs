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
        messageText.gameObject.SetActive(true);
        messageText.text = message;
        // Scenes place this as 14pt text in the bottom-left corner, hidden behind props. Center it above the buttons.
        var rt = messageText.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 140f);
        rt.sizeDelta = new Vector2(700f, 120f);
        messageText.fontSize = 56;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.horizontalOverflow = HorizontalWrapMode.Overflow;
        if (messageText.GetComponent<Outline>() == null) messageText.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);
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
