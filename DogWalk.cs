using System.Collections;
using UnityEngine;

public class DogRandomWalk : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float turnSpeed = 2f;
    public float detectionRange = 2f; // Range to detect obstacles
    public LayerMask obstacleLayer; // Layer to identify obstacles
    public int detectionAngle = 180; // Angle for obstacle detection rays
    public int rayCount = 12; // Number of rays used for detection

    [Header("Animation")]
    [Range(0.1f, 3f)]
    public float animationSpeed = 1f; // Speed of animations

    [Header("Time Settings")]
    public Vector2 exitTimeRange = new Vector2(10f, 30f); // Time range for exiting movement
    public float recoveryTime = 5f; // Time to recover after stopping

    [Header("Map Bounds")]
    public Vector2 mapBoundsX = new Vector2(-10f, 10f); // Horizontal map limits
    public Vector2 mapBoundsZ = new Vector2(-10f, 10f); // Vertical map limits

    [Header("References")]
    public Transform jawPosition; // Reference for where carried objects are placed

    private Rigidbody rb;
    private Animator animator;
    private Vector3 movementDirection;
    private bool isActive = true;
    private DogFollow dogFollow;

    private void Awake()
    {
        if (jawPosition == null)
        {
            jawPosition = transform.Find("jawPosition");
            if (jawPosition == null)
            {
                Debug.LogWarning("JawPosition not found! Please assign it in the inspector.");
            }
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        dogFollow = GetComponent<DogFollow>();

        if (dogFollow == null)
        {
            Debug.LogError("Missing DogFollow component!");
            return;
        }

        animator.speed = animationSpeed;

        // Check if the dog should stop moving due to proximity to the player
        if (ShouldStopForPlayer())
        {
            StopDogMovement();
            return;
        }

        StartCoroutine(RandomMovement());
    }

    private void OnEnable()
    {
        DropCarriedObject();

        isActive = true;
        StopAllCoroutines();

        if (animator != null)
        {
            animator.SetBool("isSitting", false);
            animator.SetBool("isWalking", false);
        }

        StartCoroutine(RandomMovement());
    }

    // Drops any object currently held by the dog
    private void DropCarriedObject()
    {
        if (jawPosition != null && jawPosition.childCount > 0)
        {
            Transform carriedObject = jawPosition.GetChild(0);

            // Reset physics properties of the carried object
            Rigidbody objRb = carriedObject.GetComponent<Rigidbody>();
            if (objRb != null)
            {
                objRb.isKinematic = false;
                objRb.useGravity = true;
                objRb.AddForce(Vector3.up * 2f, ForceMode.Impulse); // Small upward force to ensure it doesn't sink
            }

            Collider objCollider = carriedObject.GetComponent<Collider>();
            if (objCollider != null)
            {
                objCollider.isTrigger = false;
                objCollider.enabled = true;
            }

            // Detach the object from the dog
            carriedObject.SetParent(null);

            // Drop the object slightly in front of the dog
            Vector3 dropPosition = transform.position + transform.forward * 1f + Vector3.up * 0.5f;
            carriedObject.position = dropPosition;
            carriedObject.rotation = Quaternion.identity;

            carriedObject.gameObject.SetActive(true);

            // Ensure the object is visible
            MeshRenderer renderer = carriedObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isActive = false;

        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isSitting", false);
        }

        movementDirection = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (!isActive) return;

        if (ShouldStopForPlayer())
        {
            StopDogMovement();
            return;
        }

        if (animator.GetBool("isSitting"))
        {
            StopDogMovement();
            return;
        }

        AvoidObstacles();
        if (movementDirection != Vector3.zero)
        {
            Move();
            AdjustAnimationSpeed();
        }
    }

    // Determines if the dog should stop due to proximity to the player
    private bool ShouldStopForPlayer()
    {
        if (dogFollow == null || dogFollow.player == null) return false;
        return Vector3.Distance(transform.position, dogFollow.player.position) <= dogFollow.stopDistance;
    }

    private void Move()
    {
        Vector3 newPosition = rb.position + movementDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
        rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
    }

    // Uses raycasting to avoid obstacles in the dog's path
    private void AvoidObstacles()
    {
        float angleStep = detectionAngle / (rayCount - 1);
        for (int i = 0; i < rayCount; i++)
        {
            float angle = -detectionAngle / 2 + angleStep * i;
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * transform.forward;

            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, rayDirection, detectionRange, obstacleLayer))
            {
                movementDirection = GetSafeDirection();
                return;
            }
        }
    }

    private IEnumerator RandomMovement()
    {
        yield return new WaitForSeconds(0.1f);

        while (isActive)
        {
            if (ShouldStopForPlayer())
            {
                StopDogMovement();
                yield break;
            }

            if (animator != null && !animator.GetBool("isWalking"))
            {
                animator.SetBool("isWalking", true);
            }

            movementDirection = GetRandomDirection();
            float moveDuration = Random.Range(2f, 5f);
            yield return new WaitForSeconds(moveDuration);

            movementDirection = Vector3.zero;
            if (animator != null)
            {
                animator.SetBool("isWalking", false);
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private Vector3 GetSafeDirection()
    {
        Vector3 randomDirection;
        int attempts = 0;

        do
        {
            randomDirection = GetRandomDirection();
            Vector3 potentialPosition = transform.position + randomDirection * detectionRange;

            if (IsPositionInBounds(potentialPosition))
            {
                break;
            }

            attempts++;
        } while (attempts < 10);

        return randomDirection.normalized;
    }

    private Vector3 GetRandomDirection()
    {
        Vector3 randomDirection = Random.insideUnitSphere;
        randomDirection.y = 0;
        return randomDirection.normalized;
    }

    private bool IsPositionInBounds(Vector3 position)
    {
        return position.x >= mapBoundsX.x && position.x <= mapBoundsX.y &&
               position.z >= mapBoundsZ.x && position.z <= mapBoundsZ.y;
    }

    // Adjusts animation speed based on movement speed
    private void AdjustAnimationSpeed()
    {
        float speedMultiplier = moveSpeed / 2f;
        animator.speed = Mathf.Clamp(animationSpeed * speedMultiplier, 0.5f, 3f);
    }

    public void StopDogMovement()
    {
        StopAllCoroutines();
        isActive = false;
        movementDirection = Vector3.zero;

        if (ShouldStopForPlayer())
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isSitting", true);
        }
        else
        {
            animator.SetBool("isWalking", false);
        }
    }

    public void ActivateDogMovement()
    {
        if (!ShouldStopForPlayer())
        {
            isActive = true;
            StartCoroutine(RandomMovement());
        }
    }

    // Visualizes detection range and map bounds in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        float y = transform.position.y;
        Gizmos.DrawLine(new Vector3(mapBoundsX.x, y, mapBoundsZ.x), new Vector3(mapBoundsX.x, y, mapBoundsZ.y));
        Gizmos.DrawLine(new Vector3(mapBoundsX.y, y, mapBoundsZ.x), new Vector3(mapBoundsX.y, y, mapBoundsZ.y));
        Gizmos.DrawLine(new Vector3(mapBoundsX.x, y, mapBoundsZ.x), new Vector3(mapBoundsX.y, y, mapBoundsZ.x));
        Gizmos.DrawLine(new Vector3(mapBoundsX.x, y, mapBoundsZ.y), new Vector3(mapBoundsX.y, y, mapBoundsZ.y));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
