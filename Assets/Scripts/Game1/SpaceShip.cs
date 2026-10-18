using UnityEngine;
using UnityEngine.SceneManagement;

public class SpaceShip : MonoBehaviour
{
    [SerializeField] private float flySpeed = 2f;
    [SerializeField] private float quitDelaySeconds = 5f;
    private bool shouldFly = false;
    private bool quitStarted = false;

    private void Update()
    {
        if (shouldFly)
        {
            // Keep flying animation/movement if desired
            transform.Translate(Vector2.up * flySpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Destroy(collision.gameObject);
            shouldFly = true;
            // 5 seconds after touching the spaceship
            if (!quitStarted)
            {
                quitStarted = true;
                StartCoroutine(QuitAfterDelayAndFlush());
            }
        }
    }

    private System.Collections.IEnumerator QuitAfterDelayAndFlush()
    {
        Debug.Log("Player touched spaceship in 'start' scene. Quitting in 5 seconds after sending logs...");
        yield return new WaitForSeconds(quitDelaySeconds);

        // Ensure logs are sent to Supabase (or saved locally) before quitting
        var logger = ExperimentLogger.Instance;
        if (logger != null)
        {
            yield return logger.FlushAndSaveSession();
        }

        DoQuit();
    }

    private static void DoQuit()
    {
        Debug.Log("Quitting game after logs were sent.");
#if UNITY_WEBGL && !UNITY_EDITOR
        // Application.Quit does nothing in the browser; S2 replaces this with the Ending scene
        SceneManager.LoadScene("main");
        return;
#endif
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}