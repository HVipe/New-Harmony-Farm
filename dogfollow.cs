using UnityEngine;
using System.Collections;

public class DogFollow : MonoBehaviour
{
    [Header("References")]
    public Transform player; // Reference to the player
    private Animator animator;
    private Rigidbody rb;

    [Header("Movement Settings")]
    public float detectionRange = 10f; // Range at which the dog detects the player
    public float stopDistance = 1.5f; // Distance at which the dog stops moving toward the player
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float rotationThreshold = 1f; // Minimum angle to trigger rotation toward the player

    private bool isSitting = false;
    private bool isMoving = false;
    private bool isTransitioning = false;
    private bool isInitialized = false;

    void Awake()
    {
        InitializeComponents();
    }

    // Ensures all required components are initialized
    private void InitializeComponents()
    {
        if (!isInitialized)
        {
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody>();
            if (animator == null)
            {
                Debug.LogError("Animator component not found on the dog!");
            }
            if (rb == null)
            {
                Debug.LogError("Rigidbody component not found on the dog!");
            }
            isInitialized = true;
        }
    }

    void Start()
    {
        if (player == null)
        {
            Debug.LogWarning("Player reference not set for dog follow behavior!");
        }
    }

    void OnEnable()
    {
        InitializeComponents();
        if (player == null || animator == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        isSitting = false;
        isMoving = false;
        isTransitioning = false;

        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isSitting", false);

            // Determine initial state based on the player's distance
            if (distanceToPlayer <= stopDistance)
            {
                animator.SetBool("isSitting", true);
                isSitting = true;
            }
            else
            {
                animator.SetBool("isWalking", true);
                isMoving = true;
            }
        }
    }

    void Update()
    {
        if (player == null || animator == null) return;
        FollowPlayer();
    }

    // Handles the logic for following the player
    void FollowPlayer()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > stopDistance)
        {
            // If sitting, transition to standing and walking
            if (!isTransitioning && isSitting)
            {
                StartCoroutine(StandUpAndWalk());
            }
            else if (!isTransitioning)
            {
                isMoving = true;
                animator.SetBool("isWalking", true);
                ContinueMoving();
            }
        }
        else
        {
            // If close to the player, transition to sitting
            if (!isTransitioning && !isSitting)
            {
                StartCoroutine(TransitionToSit());
            }
            FacePlayer();
        }
    }

    // Rotates the dog to face the player
    void FacePlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0;

        float angleDifference = Vector3.Angle(transform.forward, directionToPlayer);

        if (angleDifference > rotationThreshold)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // Moves the dog toward the player
    void ContinueMoving()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed));

        Vector3 movePosition = direction * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movePosition);

        if (animator != null)
        {
            float currentSpeed = rb.velocity.magnitude;
            animator.SetFloat("Speed", currentSpeed);
        }
    }

    // Coroutine to transition from sitting to walking
    IEnumerator StandUpAndWalk()
    {
        if (isTransitioning || animator == null) yield break;

        isTransitioning = true;

        animator.SetBool("isWalking", true);
        yield return new WaitForSeconds(0.1f);

        animator.SetBool("isSitting", false);
        isSitting = false;
        isMoving = true;

        yield return new WaitForSeconds(0.2f);

        isTransitioning = false;
    }

    // Coroutine to transition from walking to sitting
    IEnumerator TransitionToSit()
    {
        if (isTransitioning || animator == null) yield break;

        isTransitioning = true;

        animator.SetBool("isSitting", true);
        isSitting = true;
        isMoving = false;

        yield return new WaitForSeconds(0.2f);

        animator.SetBool("isWalking", false);

        isTransitioning = false;
    }

    // Visualizes detection and stop distances in the editor
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stopDistance);
        }
    }
}
