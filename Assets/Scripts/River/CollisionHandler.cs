using UnityEngine;
using System;

public class CollisionHandler : MonoBehaviour
{
    public UIManager uiManager;
    private Camera mainCamera;
    private AudioSource audioSource;
    
    // Latch to ensure finish (win) is logged only once
    private bool hasFinishedLogged = false;

    [SerializeField] private AudioClip fishCollisionSound;
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip screwCollectSound;
    
    void Start()
    {
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
        // Add AudioSource component if it doesn't exist
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private bool PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
            return true;
        }
        return false;
    }

    void Update()
    {
        CheckOutOfBounds();
    }

    private bool outOfBoundsHandled = false;

    void CheckOutOfBounds()
    {
        if (outOfBoundsHandled || mainCamera == null) return; // was re-firing every frame when GameController was missing
        Vector3 viewPos = mainCamera.WorldToViewportPoint(transform.position);
        
        // Check if player is out of camera view (viewport coordinates are normalized 0 to 1)
        if (viewPos.x < 0 || viewPos.x > 1 || viewPos.y < 0 || viewPos.y > 1)
        {
            outOfBoundsHandled = true;
            try
            {
                bool isForest = false;
                try { isForest = string.Equals(ExperimentLogger.Instance?.GetCurrentArea(), "forest", StringComparison.OrdinalIgnoreCase); } catch { }
                ExperimentLogger.Instance?.LogOutcome(isForest ? "Forest" : "River", "lose", reason: "out_of_bounds");
            }
            catch (Exception) { }
            uiManager.ShowTryAgain();
            if (GameController.Instance != null)
            {
                GameController.Instance.EndGame();
                Destroy(gameObject);
            }
        }   
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Fish"))
        {
            PlaySound(fishCollisionSound);
            // Trial-level: log collision with piranha and finish lose once
            try
            {
                // Log trial-level collision and finish in either area; logger remaps type to forest_* in Forest
                ExperimentLogger.Instance?.LogRiverCollision("piranha", collision.GetContact(0).point);
                if (!hasFinishedLogged)
                {
                    hasFinishedLogged = true;
                    ExperimentLogger.Instance?.LogRiverFinish("lose");
                }
            }
            catch (Exception) { }
            try
            {
                bool isForest = false;
                try { isForest = string.Equals(ExperimentLogger.Instance?.GetCurrentArea(), "forest", StringComparison.OrdinalIgnoreCase); } catch { }
                ExperimentLogger.Instance?.LogOutcome(isForest ? "Forest" : "River", "lose", reason: "fish_collision");
            }
            catch (Exception) { }
            uiManager.ShowTryAgain();
            if (GameController.Instance != null)
            {
                GameController.Instance.EndGame();
            }
            else
            {
                Debug.LogError("GameController instance not found.");
            }
        }
        else if (collision.gameObject.CompareTag("RiverBankEnd"))
        {
            // Guard against multiple win logs caused by repeated collisions
            if (hasFinishedLogged) return;
            hasFinishedLogged = true;

            PlaySound(victorySound);
            // Trial-level finish (win)
            try
            {
                // Log a trial-level finish in either area; logger remaps type name for Forest
                ExperimentLogger.Instance?.LogRiverFinish("win");
            }
            catch (Exception) { }
            try
            {
                bool isForest = false;
                try { isForest = string.Equals(ExperimentLogger.Instance?.GetCurrentArea(), "forest", StringComparison.OrdinalIgnoreCase); } catch { }
                ExperimentLogger.Instance?.LogOutcome(isForest ? "Forest" : "River", "win", reason: "reached_end");
            }
            catch (Exception) { }
            uiManager.ShowWellDone();
            if (GameController.Instance != null)
            {
                GameController.Instance.EndGame();
            }
            else
            {
                Debug.LogError("GameController instance not found.");
            }
        }
        else if (collision.gameObject.CompareTag("Mushroom"))
        {
            PlaySound(jumpSound);
            // Trial-level: landing on a rock is a successful jump land
            try
            {
                var rock = collision.gameObject.GetComponent<RockId>();
                string toRockId = (rock != null && !string.IsNullOrEmpty(rock.rockId)) ? rock.rockId : collision.gameObject.name;
                var pmrLocal = GetComponent<PlayerMovementRiver>();
                string jumpId = pmrLocal != null ? pmrLocal.lastJumpId : null;
                // Log landing in either area; logger remaps type name for Forest
                ExperimentLogger.Instance?.LogRiverJumpLand(jumpId, toRockId, true);
            }
            catch (Exception) { }
            var pmr = GetComponent<PlayerMovementRiver>();
            if (pmr != null)
            {
                var rock = collision.gameObject.GetComponent<RockId>();
                string toRockId2 = (rock != null && !string.IsNullOrEmpty(rock.rockId)) ? rock.rockId : collision.gameObject.name;
                pmr.lastRockId = toRockId2;
                pmr.currentRockId = toRockId2;
                pmr.Jump(); // start next jump; Jump() will emit jump_start
            }
            else
            {
                GetComponent<PlayerMovementRiver>().Jump();
            }
        }
        else if (collision.gameObject.CompareTag("Screw"))
        {
            PlaySound(screwCollectSound);
            // Trial-level forest resolve: collected
            try
            {
                // Prevent double-collect by disabling collider immediately
                var col = collision.collider != null ? collision.collider : collision.gameObject.GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
                var tracker = collision.gameObject.GetComponent<ForestItemTrack>();
                if (tracker != null)
                {
                    tracker.Resolve("collected", "player");
                }
            }
            catch (Exception) { }
            Destroy(collision.gameObject);
            uiManager.CollectScrew();
            // Win must not depend on whether a victory sound is assigned
            if (uiManager.screwsCollected == uiManager.totalScrews)
            {
                PlaySound(victorySound);
                try { ExperimentLogger.Instance?.LogOutcome("Forest", "win", reason: "collected_all_screws"); } catch (Exception) { }
                uiManager.ShowWellDone();
                if (GameController.Instance != null)
                {
                    GameController.Instance.EndGame();
                }
                else
                {
                    Debug.LogError("GameController instance not found.");
                }
            }
        }
        else if (collision.gameObject.CompareTag("Water"))
        {
            // Trial-level: collision with water
            try
            {
                // Log trial-level water collision in either area; logger remaps type for Forest
                ExperimentLogger.Instance?.LogRiverCollision("water", collision.GetContact(0).point);
                if (!hasFinishedLogged)
                {
                    hasFinishedLogged = true;
                    ExperimentLogger.Instance?.LogRiverFinish("lose");
                }
            }
            catch (Exception) { }
            try
            {
                bool isForest = false;
                try { isForest = string.Equals(ExperimentLogger.Instance?.GetCurrentArea(), "forest", StringComparison.OrdinalIgnoreCase); } catch { }
                ExperimentLogger.Instance?.LogOutcome(isForest ? "Forest" : "River", "lose", reason: "water_collision");
            }
            catch (Exception) { }
            uiManager.ShowTryAgain();
            if (GameController.Instance != null)
            {
                GameController.Instance.EndGame();
            }
        }
    }
}