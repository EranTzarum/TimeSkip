using UnityEngine;

public class PlayerMovementGame : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 10f;
    private Rigidbody2D rb;
    private bool isOnMushroom = false;

    // --- Add these lines ---
    private static Vector3 savedPosition = Vector3.zero;
    private static bool hasSavedPosition = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        // --- If we have a saved position from a previous visit, restore it ---
        if (hasSavedPosition)
        {
            transform.position = savedPosition;
        }
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveX * speed, rb.linearVelocity.y);

        // Only jump from the ground; unlimited air jumps let kids fly over the level
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private readonly ContactPoint2D[] contacts = new ContactPoint2D[8];

    private bool IsGrounded()
    {
        int n = rb.GetContacts(contacts);
        for (int i = 0; i < n; i++)
            if (contacts[i].normal.y > 0.5f) return true;
        return false;
    }

    // When the player object is disabled or destroyed, remember its position.
    void OnDisable()
    {
        hasSavedPosition = true;
        savedPosition = transform.position;
    }

    // אם תרצה להוסיף לוגיקה לפטרייה וכו', השאר כמות שהוא - לא נגעתי
    public void SetOnMushroom(bool value)
    {
        isOnMushroom = value;
    }
}
