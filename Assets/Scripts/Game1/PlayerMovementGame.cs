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
        float moveX = DevInput.Horizontal(Input.GetAxisRaw("Horizontal"));
        rb.linearVelocity = new Vector2(moveX * speed, rb.linearVelocity.y);

        // Ground jump + one air jump. Unlimited air jumps let kids fly over the level; ground-only
        // jumps got them stuck on obstacles the level was built around.
        bool grounded = IsGrounded();
        if (grounded) airJumpsLeft = airJumps;
        if ((Input.GetKeyDown(KeyCode.Space) || DevInput.ConsumeJump()) && (grounded || airJumpsLeft-- > 0))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    [SerializeField] private int airJumps = 1;
    private int airJumpsLeft;

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
