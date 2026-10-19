using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    // The save waits only up to a limit; the model answer gets a bit longer (a cold HF Space can be slow)
    private const float MaxSaveWaitSeconds = 10f;
    private const float MaxModelWaitSeconds = 25f;

    private System.Collections.IEnumerator QuitAfterDelayAndFlush()
    {
        Debug.Log("Player touched spaceship in 'start' scene. Quitting in 5 seconds after sending logs...");
        var thanks = ScreenMessage.Card("You fixed the time machine!", "Thanks for playing Time Skip", "Saving...");

        // Same timing as before (5 s, then save) so session features are unchanged.
        // Upload runs on the logger (it survives the scene change); the ending waits for it only up to a limit.
        yield return new WaitForSeconds(quitDelaySeconds);
        bool saved = false;
        var logger = ExperimentLogger.Instance;
        if (logger != null) logger.StartCoroutine(Flush(logger, () => saved = true));
        else saved = true;
        float until = Time.time + MaxSaveWaitSeconds;
        yield return new WaitUntil(() => saved || Time.time > until);

        // Result card (clinician view for the jam prototype), then ENTER: statistics, ENTER again: main menu
        Destroy(thanks);
        var body = ResultCard();
        float modelUntil = Time.time + MaxModelWaitSeconds;
        while (ExperimentLogger.LastModelResponse == null && Time.time < modelUntil)
            yield return null;
        body.text = ResultText(ExperimentLogger.LastModelResponse);

        yield return null; // a key held from before must not skip the card
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter));
        GoToStatistics();
    }

    private static Text ResultCard()
    {
        var canvas = ScreenMessage.Canvas("ResultCard", 60);
        var panel = ScreenMessage.Panel(canvas.transform, new Vector2(0.2f, 0.18f), new Vector2(0.8f, 0.86f), lineOnTop: true);
        var title = ScreenMessage.Label(panel.transform, "Session complete", 60, TextAnchor.MiddleCenter, new Vector2(0f, 0.82f), new Vector2(1f, 1f));
        title.color = ScreenMessage.Gold;
        var body = ScreenMessage.Label(panel.transform, "The model is reading this session...", 40, TextAnchor.MiddleCenter, new Vector2(0f, 0.3f), new Vector2(1f, 0.82f));
        body.fontStyle = FontStyle.Normal;
        var note = ScreenMessage.Label(panel.transform, "Research prototype - not a diagnosis", 28, TextAnchor.MiddleCenter, new Vector2(0f, 0.17f), new Vector2(1f, 0.3f));
        note.fontStyle = FontStyle.Normal;
        note.color = new Color(0.8f, 0.83f, 0.92f);
        ScreenMessage.Label(panel.transform, $"Press {ScreenMessage.Key("ENTER")} for the statistics", 36, TextAnchor.MiddleCenter, new Vector2(0f, 0.02f), new Vector2(1f, 0.17f));
        return body;
    }

    // Model answer: {"prediction":[0|1], "probabilities":[[p0, p1]]}; p1 is the ADHD class
    private static string ResultText(string resp)
    {
        var m = string.IsNullOrEmpty(resp) ? null
            : Regex.Match(resp, @"""probabilities""\s*:\s*\[\s*\[\s*[-\d.eE+]+\s*,\s*([-\d.eE+]+)");
        if (m == null || !m.Success)
            return "<size=46><b>ADHD signal: not available</b></size>\n\nThe model did not answer this time\n(offline, or the service is waking up).";
        double p = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        bool flagged = Regex.IsMatch(resp, @"""prediction""\s*:\s*\[\s*1\s*\]");
        string color = flagged ? "#FF8A65" : "#81C784";
        string verdict = flagged ? "ADHD pattern detected" : "No ADHD pattern detected";
        return $"<size=40>ADHD signal</size>\n<size=110><b><color={color}>{p * 100:0}%</color></b></size>\n<size=42><b>{verdict}</b></size>";
    }

    private static void GoToStatistics()
    {
        MenuOverlay.OpenStatisticsOnLoad = true;
        SceneManager.LoadScene("main");
    }

    private static System.Collections.IEnumerator Flush(ExperimentLogger logger, System.Action done)
    {
        yield return logger.FlushAndSaveSession();
        done();
    }
}
