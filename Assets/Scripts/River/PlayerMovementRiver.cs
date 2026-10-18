
using UnityEngine;
using System;

public class PlayerMovementRiver : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 10f;
    private Rigidbody2D rb;
    private bool isOnMushroom = false; 
    [NonSerialized] public string currentRockId = null; // updated on trigger enter/exit
    [NonSerialized] public string lastRockId = "unknown"; // updated on landing
    [NonSerialized] public string lastJumpId = null; // set on jump start

    private float defaultGravityScale;
    private float lastJumpTime = -10f;
    private const float jumpCooldown = 0.12f; // debounce to prevent multi-triggering

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        defaultGravityScale = rb.gravityScale; // Save the original gravity scale
    }

    void Update()
    {
        float moveX = DevInput.Horizontal(Input.GetAxisRaw("Horizontal")); 
        rb.linearVelocity = new Vector2(moveX * speed, rb.linearVelocity.y);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryJumpByInput();
        }

                if (Input.GetKey(KeyCode.DownArrow))
        {
            Debug.Log("Down Arrow is held down.");
            rb.gravityScale = defaultGravityScale * 6;
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }
    }
    // Called by input or auto-bounce; enforces grounded + cooldown
    public void Jump()  
    {
        // Guard: debounce rapid re-triggers
        if (Time.time - lastJumpTime < jumpCooldown) return;

        // Trial-level: log jump start from the current rock; store jumpId for landing
        try { lastJumpId = ExperimentLogger.Instance?.LogRiverJumpStart(currentRockId); } catch (System.Exception) { }
        lastRockId = currentRockId; // remember where we jumped from
        lastJumpTime = Time.time;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); 
    }

    // Separate wrapper for player input attempts to avoid auto-bounce recursion
    private void TryJumpByInput()
    {
        // Allow jump regardless of being on a rock; cooldown still applies
        Jump();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Mushroom"))
        {
            var rock = other.GetComponent<RockId>();
            currentRockId = rock != null && !string.IsNullOrEmpty(rock.rockId) ? rock.rockId : other.gameObject.name;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Mushroom"))
        {
            var rock = other.GetComponent<RockId>();
            string rid = rock != null && !string.IsNullOrEmpty(rock.rockId) ? rock.rockId : other.gameObject.name;
            if (currentRockId == rid)
            {
                currentRockId = null;
            }
        }
    }

}
