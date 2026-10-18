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

    // Was a blank screen for 5 s plus the whole upload and model call (can be a minute on a cold Space)
    private const float MaxSaveWaitSeconds = 10f;

    private System.Collections.IEnumerator QuitAfterDelayAndFlush()
    {
        Debug.Log("Player touched spaceship in 'start' scene. Quitting in 5 seconds after sending logs...");
        ScreenMessage.Show("You fixed the spaceship and flew home!
Thanks for playing Time Skip.", 0.6f, 64);

        // Same timing as before (5 s, then save) so session features are unchanged.
        // Upload runs on the logger (it survives the scene change); the ending waits for it only up to a limit.
        yield return new WaitForSeconds(quitDelaySeconds);
        bool saved = false;
        var logger = ExperimentLogger.Instance;
        if (logger != null) logger.StartCoroutine(Flush(logger, () => saved = true));
        else saved = true;
        float until = Time.time + MaxSaveWaitSeconds;
        yield return new WaitUntil(() => saved || Time.time > until);

        DoQuit();
    }

    private static System.Collections.IEnumerator Flush(ExperimentLogger logger, System.Action done)
    {
        yield return logger.FlushAndSaveSession();
        done();
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