using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int lives = 3; // Number of lives
    public int currentLevel = 0; // Current level index

    public UIManagerMG uiManager;
    public QuestionManager questionManager;

    // Timestamp when the question (stimulus) becomes visible
    private float questionShownAt = 0f;

    private const string DefaultQuestion = "Which of these did you see in the room?";

    // Next level, or finish after the last one ("NextScene" did not exist).
    private void AdvanceLevel()
    {
        currentLevel++;
        if (currentLevel >= questionManager.GetTotalQuestions())
        {
            try { ExperimentLogger.Instance?.LogDwarfMinigameEnd("memory", "reached_level_4"); } catch (Exception) { } // original reason string kept for log compatibility
            SceneManager.LoadScene("kitchen");
            return;
        }
        StartCoroutine(PlayLevel());
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        try { ExperimentLogger.Instance?.LogDwarfMinigameStart("memory"); } catch (Exception) { }
        StartCoroutine(PlayLevel());
    }

    
    public IEnumerator PlayLevel()
    {
        Debug.Log($"🟡 Current level index: {currentLevel}");
        try { ExperimentLogger.Instance?.LogMemoryLevelStart(currentLevel); } catch (Exception) { }

        // Make sure the question panel is hidden before showing the scene image
        uiManager.HideQuestionPanel();

        Sprite snapshot = SceneSnapshot.Instance.GetSnapshotForLevel(currentLevel);
        string question = questionManager.GetQuestion(currentLevel);

        if (snapshot == null)
        {
            // Used to hang on a blank screen. Skip the level so the session continues.
            Debug.LogError($"Missing snapshot for level {currentLevel}, skipping level.");
            try { ExperimentLogger.Instance?.LogMemoryLevelResult(currentLevel, false, "skipped_missing_asset", lives); } catch (Exception) { }
            AdvanceLevel();
            yield break;
        }

        uiManager.ShowSceneSnapshot(snapshot); // Show scene image
        questionManager.PreloadQuestion(currentLevel); // Preload question while image is visible
        yield return new WaitForSeconds(5f); // Wait for 5 seconds

        uiManager.HideSceneSnapshot(); // Hide image
        if (uiManager.questionText != null)
            uiManager.questionText.text = string.IsNullOrWhiteSpace(question) || question.StartsWith("q") ? DefaultQuestion : question;
        uiManager.ShowQuestionPanel(); // Show question panel after image disappears

        // Mark stimulus onset and log dwarf_trial_start with shown items and correct item
        questionShownAt = Time.time;
        try
        {
            if (questionManager != null && currentLevel < questionManager.questions.Length)
            {
                var q = questionManager.questions[currentLevel];
                var opts = questionManager.CurrentOptions;
                var shown = new List<string>(opts != null ? opts.Length : 0);
                if (opts != null)
                {
                    for (int i = 0; i < opts.Length; i++)
                    {
                        var s = opts[i];
                        shown.Add(s != null ? s.name : "null");
                    }
                }
                string correct = q.correctAnswer != null ? q.correctAnswer.name : "unknown";
                ExperimentLogger.Instance?.LogDwarfTrialStart(currentLevel, shown, correct);
            }
        }
        catch (Exception) { }
    }


    // New overload: receives selected index to log detailed response
    public void CheckAnswer(int selectedIndex)
    {
        // Derive correctness and selected item name from current question data
        bool isCorrect = false;
        string selectedItemName = "unknown";
        try
        {
            if (questionManager != null && currentLevel < questionManager.questions.Length)
            {
                var q = questionManager.questions[currentLevel];
                var opts = questionManager.CurrentOptions;
                if (opts != null && selectedIndex >= 0 && selectedIndex < opts.Length)
                {
                    var sel = opts[selectedIndex];
                    selectedItemName = sel != null ? sel.name : "null";
                    isCorrect = (sel == q.correctAnswer);
                }
            }
        }
        catch (Exception) { }

        // Reaction time since the question was shown
        int rtMs = Mathf.Max(0, (int)Mathf.Round((Time.time - questionShownAt) * 1000f));
        try { ExperimentLogger.Instance?.LogDwarfTrialResponse(currentLevel, selectedItemName, isCorrect, rtMs); } catch (Exception) { }

        // Continue original flow using the existing bool-based handler
        CheckAnswer(isCorrect);
    }

    public void CheckAnswer(bool isCorrect)
    {
        if (isCorrect)
        {
            try {
                ExperimentLogger.Instance?.LogMemoryClick(true, currentLevel, lives);
                ExperimentLogger.Instance?.LogMemoryLevelResult(currentLevel, true, "answered_correct", lives);
            } catch (Exception) { }
            AdvanceLevel();
        }
        else
        {
            lives--;
            try { ExperimentLogger.Instance?.LogMemoryClick(false, currentLevel, lives); } catch (Exception) { }
            uiManager.UpdateLives(lives);
            if (lives > 0) uiManager.ShowWrongAnswer();
            if (lives <= 0)
            {
                try {
                    ExperimentLogger.Instance?.LogMemoryLevelResult(currentLevel, false, "game_over", lives);
                    ExperimentLogger.Instance?.LogDwarfMinigameEnd("memory", "game_over");
                } catch (Exception) { }
                uiManager.ShowGameOver();
            }
        }
    }
}
