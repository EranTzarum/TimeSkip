using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverPanel : MonoBehaviour
{
    public void TryAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("main");
    }

    public void SkipToNextLevel()
    {
        SceneManager.LoadScene("dwarf");
    }    
    
    public void GoToStart()
    {
        SceneManager.LoadScene("Start");
    }
}
