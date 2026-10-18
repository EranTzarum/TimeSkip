using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        // ��� �� ����� ������ �� �����
        SceneManager.LoadScene("Start");
    }

    public void JumpMenu()
    {
        // ��� �� ����� ������ �� �����
        SceneManager.LoadScene("JumpMenu");
    }

    public void River()
    {
        // ��� �� ����� ������ �� �����
        SceneManager.LoadScene("river");
    }

    public void ShowVideo()
    {
        // ��� �� ���� ������� �� ���� ����� �����
        SceneManager.LoadScene("VideoScene");
    }

    public void ShowInfo()
    {
        MenuOverlay.ShowInfo();
    }
    public void ShowStatistics()
    {
        MenuOverlay.ShowStatistics();
    }

    public void Forest()
    {
        // ��� �� ���� ��� �����
        SceneManager.LoadScene("forest");
    }
    public void Kitchen()
    {
        // ��� �� ���� ��� �����
        SceneManager.LoadScene("MemoryGame");
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("main");
    }


    public void QuitGame()
    {
        // ���� �� �����
        Application.Quit();
    }
}
