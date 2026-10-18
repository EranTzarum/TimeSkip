using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class QuestionData
{
    public string question;
    public Sprite correctAnswer;
    public Sprite[] answerOptions;
    public Sprite sceneImage;
}

public class QuestionManager : MonoBehaviour
{
    public UIManagerMG uiManagerMG;
    public Button[] answerButtons;
    public QuestionData[] questions;
    private int correctAnswerIndex;

    public string GetQuestion(int level)
    {
        if (level < questions.Length)
            return questions[level].question;
        return "No question available";
    }

    public int GetTotalQuestions()
    {
        return questions.Length;
    }

    // Options in the order shown on screen for the current level (deduped + shuffled).
    public Sprite[] CurrentOptions { get; private set; } = new Sprite[0];

    public void PreloadQuestion(int level)
    {
        if (level >= questions.Length) return;

        // Dedupe: Q1 listed the correct sprite 3 times. Shuffle: the correct answer was always slot 0 in Q3/Q4.
        List<Sprite> answers = questions[level].answerOptions.Where(s => s != null).Distinct().ToList();
        for (int i = answers.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (answers[i], answers[j]) = (answers[j], answers[i]);
        }
        CurrentOptions = answers.ToArray();
        correctAnswerIndex = answers.IndexOf(questions[level].correctAnswer);

        SetAnswers(answers);
    }

    private void SetAnswers(List<Sprite> answers)
    {
        int buttonCount = answerButtons.Length;
        float buttonSpacing = 150f; // Adjust spacing dynamically
        float totalWidth = buttonSpacing * (answers.Count - 1);

        for (int i = 0; i < buttonCount; i++)
        {
            if (i < answers.Count)
            {
                answerButtons[i].image.sprite = answers[i];
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].transform.localPosition = new Vector3(-totalWidth / 2 + i * buttonSpacing, 0, 0);
                int index = i;
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => AnswerSelected(index));
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void AnswerSelected(int index)
    {
        // Forward the selected index; GameManager will compute correctness and log detailed response
        GameManager.Instance.CheckAnswer(index);
    }
}
