//using UnityEngine;

//public class PlayerMovementHome : MonoBehaviour
//{
//    public Sprite[] ACharDown; // 4 sprites for Down animation
//    public Sprite[] ACharUp;   // 4 sprites for Up animation
//    public Sprite[] ACharLeft; // 4 sprites for Left animation
//    public Sprite[] ACharRight; // 4 sprites for Right animation

//    public float moveSpeed = 5f; // Movement speed
//    public float jumpHeight = 2f; // Height change on "jump" (Y axis)

//    private SpriteRenderer spriteRenderer;
//    private Vector2 moveInput;
//    private Sprite[] currentDirectionSprites;
//    private int animationIndex = 0;
//    private float animationTimer = 0f;
//    private Rigidbody2D rb;
//    private bool isNearObject = false; // Check if player is near an object with Collider2D
//    private GameObject nearbyObject = null; // Reference to the object nearby with Collider2D

//    public float animationSpeed = 0.2f; // Time between frames

//    private void Start()
//    {
//        spriteRenderer = GetComponent<SpriteRenderer>();
//        currentDirectionSprites = ACharDown; // Default direction
//        rb = GetComponent<Rigidbody2D>();
//        rb.gravityScale = 0; // Ensure no gravity is affecting the player
//        rb.freezeRotation = true; // Lock rotation to prevent the player from rotating
//    }

//    private void Update()
//    {
//        // Get movement input
//        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

//        // Determine direction and set current sprites
//        if (moveInput.x > 0)
//        {
//            currentDirectionSprites = ACharRight;
//        }
//        else if (moveInput.x < 0)
//        {
//            currentDirectionSprites = ACharLeft;
//        }
//        else if (moveInput.y > 0)
//        {
//            currentDirectionSprites = ACharUp;
//        }
//        else if (moveInput.y < 0)
//        {
//            currentDirectionSprites = ACharDown;
//        }

//        // Handle animation only if moving
//        if (moveInput != Vector2.zero)
//        {
//            AnimateMovement();
//        }
//        else
//        {
//            // Reset to standing still frame
//            animationIndex = 0;
//            spriteRenderer.sprite = currentDirectionSprites[0];
//        }

//        // Check for "jumping" behavior (just changing position above object)
//        if (Input.GetKeyDown(KeyCode.Space))
//        {
//            if (isNearObject && nearbyObject != null)
//            {
//                // Ensure the player is above the object, modifying the Y position
//                Vector3 newPosition = nearbyObject.transform.position;
//                newPosition.z = transform.position.z; // Maintain the same Z level (if you want him to stay on the same plane)
//                newPosition.y = nearbyObject.transform.position.y + nearbyObject.GetComponent<Collider2D>().bounds.size.y / 2 + 0.5f; // Put player just above the object
//                transform.position = newPosition;
//            }
//        }
//    }

//    private void FixedUpdate()
//    {
//        // Update Rigidbody linear velocity (not MovePosition)
//        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, moveInput.y * moveSpeed); // Allow movement on both X and Y axes
//    }

//    private void AnimateMovement()
//    {
//        // Update animation frame
//        animationTimer += Time.deltaTime;
//        if (animationTimer >= animationSpeed)
//        {
//            animationTimer = 0f;
//            animationIndex = (animationIndex + 1) % currentDirectionSprites.Length;
//            spriteRenderer.sprite = currentDirectionSprites[animationIndex];
//        }
//    }

//    // Check if the player is near an object with Collider2D
//    private void OnTriggerStay2D(Collider2D collider)
//    {
//        if (collider.CompareTag("base")) // Replace "Pickup" with your tag
//        {
//            nearbyObject = collider.gameObject;
//            isNearObject = true;
//        }
//    }

//    private void OnTriggerExit2D(Collider2D collider)
//    {
//        if (collider.CompareTag("base")) // Replace "Pickup" with your tag
//        {
//            nearbyObject = null;
//            isNearObject = false;
//        }
//    }

//    public void SetSpeed(float newSpeed)
//    {
//        moveSpeed = newSpeed;
//    }
//}


//using UnityEngine;

//public class PlayerMovementHome : MonoBehaviour
//{
//    public Sprite[] ACharDown; // 4 sprites for Down animation
//    public Sprite[] ACharUp;   // 4 sprites for Up animation
//    public Sprite[] ACharLeft; // 4 sprites for Left animation
//    public Sprite[] ACharRight; // 4 sprites for Right animation

//    public float moveSpeed = 5f; // Movement speed
//    public float jumpHeight = 2f; // Height change on "jump" (Y axis)

//    private SpriteRenderer spriteRenderer;
//    private Vector2 moveInput;
//    private Sprite[] currentDirectionSprites;
//    private int animationIndex = 0;
//    private float animationTimer = 0f;
//    private Rigidbody2D rb;
//    private bool isNearObject = false; // Check if player is near an object with Collider2D
//    private GameObject nearbyObject = null; // Reference to the object nearby with Collider2D
//    private Vector3 initialPositionAboveObject; // To store initial position when jumping up
//    private bool isOnTopOfObject = false; // To track if player is on top of the object

//    public float animationSpeed = 0.2f; // Time between frames

//    private void Start()
//    {
//        spriteRenderer = GetComponent<SpriteRenderer>();
//        currentDirectionSprites = ACharDown; // Default direction
//        rb = GetComponent<Rigidbody2D>();
//        rb.gravityScale = 0; // Ensure no gravity is affecting the player
//        rb.freezeRotation = true; // Lock rotation to prevent the player from rotating
//    }

//    private void Update()
//    {
//        // Get movement input
//        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

//        // Determine direction and set current sprites
//        if (moveInput.x > 0)
//        {
//            currentDirectionSprites = ACharRight;
//        }
//        else if (moveInput.x < 0)
//        {
//            currentDirectionSprites = ACharLeft;
//        }
//        else if (moveInput.y > 0)
//        {
//            currentDirectionSprites = ACharUp;
//        }
//        else if (moveInput.y < 0)
//        {
//            currentDirectionSprites = ACharDown;
//        }

//        // Handle animation only if moving
//        if (moveInput != Vector2.zero)
//        {
//            AnimateMovement();
//        }
//        else
//        {
//            // Reset to standing still frame
//            animationIndex = 0;
//            spriteRenderer.sprite = currentDirectionSprites[0];
//        }

//        // Check for "jumping" behavior (just changing position above object)
//        if (Input.GetKeyDown(KeyCode.Space) && isNearObject && nearbyObject != null)
//        {
//            if (isOnTopOfObject)
//            {
//                // Move back down to the original position
//                transform.position = initialPositionAboveObject;
//                isOnTopOfObject = false;
//            }
//            else
//            {
//                // Move the player above the object
//                initialPositionAboveObject = transform.position; // Save the original position
//                Vector3 newPosition = nearbyObject.transform.position;
//                newPosition.z = transform.position.z; // Maintain the same Z level
//                newPosition.y = nearbyObject.transform.position.y + nearbyObject.GetComponent<Collider2D>().bounds.size.y / 2 + 0.5f; // Put player just above the object
//                transform.position = newPosition;
//                isOnTopOfObject = true;
//            }
//        }
//    }

//    private void FixedUpdate()
//    {
//        // Update Rigidbody linear velocity (not MovePosition)
//        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, moveInput.y * moveSpeed); // Allow movement on both X and Y axes
//    }

//    private void AnimateMovement()
//    {
//        // Update animation frame
//        animationTimer += Time.deltaTime;
//        if (animationTimer >= animationSpeed)
//        {
//            animationTimer = 0f;
//            animationIndex = (animationIndex + 1) % currentDirectionSprites.Length;
//            spriteRenderer.sprite = currentDirectionSprites[animationIndex];
//        }
//    }

//    // Check if the player is near an object with Collider2D
//    private void OnTriggerStay2D(Collider2D collider)
//    {
//        if (collider.CompareTag("base")) // Replace "base" with your desired tag
//        {
//            nearbyObject = collider.gameObject;
//            isNearObject = true;
//        }
//    }

//    private void OnTriggerExit2D(Collider2D collider)
//    {
//        if (collider.CompareTag("base")) // Replace "base" with your desired tag
//        {
//            nearbyObject = null;
//            isNearObject = false;
//        }
//    }

//    public void SetSpeed(float newSpeed)
//    {
//        moveSpeed = newSpeed;
//    }
//}




using UnityEngine;
using System.Collections;


public class PlayerMovementHome : MonoBehaviour
{
    public Sprite[] ACharDown; // 4 sprites for Down animation
    public Sprite[] ACharUp;   // 4 sprites for Up animation
    public Sprite[] ACharLeft; // 4 sprites for Left animation
    public Sprite[] ACharRight; // 4 sprites for Right animation

    public float moveSpeed = 5f; // Movement speed
    public float jumpHeight = 2f; // Height change on "jump" (Y axis)
    public float smoothSpeed = 12f; // Smooth speed for Lerp (lower is smoother)

    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private Sprite[] currentDirectionSprites;
    private int animationIndex = 0;
    private float animationTimer = 0f;
    private Rigidbody2D rb;
    private bool isNearObject = false; // Check if player is near an object with Collider2D
    private GameObject nearbyObject = null; // Reference to the object nearby with Collider2D
    private Vector3 initialPositionAboveObject; // To store initial position when jumping up
    private bool isOnTopOfObject = false; // To track if player is on top of the object

    public float animationSpeed = 0.2f; // Time between frames

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentDirectionSprites = ACharDown; // Default direction
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // Ensure no gravity is affecting the player
        rb.freezeRotation = true; // Lock rotation to prevent the player from rotating
    }

    private void Update()
    {
        // Get movement input
        moveInput = new Vector2(DevInput.Horizontal(Input.GetAxisRaw("Horizontal")), DevInput.Vertical(Input.GetAxisRaw("Vertical")));

        // Determine direction and set current sprites
        if (moveInput.x > 0)
        {
            currentDirectionSprites = ACharRight;
        }
        else if (moveInput.x < 0)
        {
            currentDirectionSprites = ACharLeft;
        }
        else if (moveInput.y > 0)
        {
            currentDirectionSprites = ACharUp;
        }
        else if (moveInput.y < 0)
        {
            currentDirectionSprites = ACharDown;
        }

        // Handle animation only if moving
        if (moveInput != Vector2.zero)
        {
            AnimateMovement();
        }
        else
        {
            // Reset to standing still frame
            animationIndex = 0;
            spriteRenderer.sprite = currentDirectionSprites[0];
        }

        // Check for "jumping" behavior (just changing position above object)
        if (Input.GetKeyDown(KeyCode.Space) && isNearObject && nearbyObject != null)
        {
            if (isOnTopOfObject)
            {
                // Move back down to the original position (smooth transition)
                StartCoroutine(MoveSmoothly(transform.position, initialPositionAboveObject));
                isOnTopOfObject = false;
            }
            else
            {
                // Move the player above the object (smooth transition)
                initialPositionAboveObject = transform.position; // Save the original position
                Vector3 newPosition = nearbyObject.transform.position;
                newPosition.z = transform.position.z; // Maintain the same Z level
                newPosition.y = nearbyObject.transform.position.y + nearbyObject.GetComponent<Collider2D>().bounds.size.y / 2 + 0.5f; // Put player just above the object
                StartCoroutine(MoveSmoothly(transform.position, newPosition));
                isOnTopOfObject = true;
            }
        }
    }

    private void FixedUpdate()
    {
        // Update Rigidbody linear velocity (not MovePosition)
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, moveInput.y * moveSpeed); // Allow movement on both X and Y axes
    }

    private void AnimateMovement()
    {
        // Update animation frame
        animationTimer += Time.deltaTime;
        if (animationTimer >= animationSpeed)
        {
            animationTimer = 0f;
            animationIndex = (animationIndex + 1) % currentDirectionSprites.Length;
            spriteRenderer.sprite = currentDirectionSprites[animationIndex];
        }
    }

    // Coroutine to move smoothly between positions
    private IEnumerator MoveSmoothly(Vector3 start, Vector3 target)
    {
        float journeyLength = Vector3.Distance(start, target);
        float startTime = Time.time;

        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            float distanceCovered = (Time.time - startTime) * smoothSpeed;
            float fractionOfJourney = distanceCovered / journeyLength;
            transform.position = Vector3.Lerp(start, target, fractionOfJourney);
            yield return null;
        }

        transform.position = target; // Ensure the player ends exactly at the target
    }

    // Check if the player is near an object with Collider2D
    private void OnTriggerStay2D(Collider2D collider)
    {
        if (collider.CompareTag("base")) // Replace "base" with your desired tag
        {
            nearbyObject = collider.gameObject;
            isNearObject = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.CompareTag("base")) // Replace "base" with your desired tag
        {
            nearbyObject = null;
            isNearObject = false;
        }
    }

    public void SetSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
}
