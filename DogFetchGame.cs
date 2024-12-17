using UnityEngine;

public class DogFetchGame : MonoBehaviour
{
    [Header("References")]
    public Transform jawPosition;
    private Animator animator;
    private Rigidbody rb;

    [Header("Fetch Settings")]
    public float fetchStopDistance = 1.5f;
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;

    private Transform targetObject;
    private Rigidbody targetRigidbody;
    private Collider targetCollider;
    private Collider[] dogColliders;

    public bool IsFetching => targetObject != null;

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        dogColliders = GetComponents<Collider>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody component is missing on the dog object.");
        }

        if (jawPosition == null)
        {
            Debug.LogError("JawPosition is not set for the dog.");
        }
    }

    private void Update()
    {
        if (targetObject != null)
        {
            // Check if the target object is picked up by the player
            if (IsTargetPickedUp())
            {
                Debug.Log("Target object was picked up by player, canceling fetch behavior.");
                ResetBehavior();
                return;
            }
            MoveTowardsTarget(targetObject.position);
        }
    }

    // Checks if the target object has been picked up by the player
    private bool IsTargetPickedUp()
    {
        if (targetObject == null) return true;

        // Check if the object has a parent (typically becomes a child of the player's hand)
        if (targetObject.parent != null && targetObject.parent != targetObject.root)
            return true;

        // Check if the object's kinematic state has changed (indicating it was picked up)
        if (targetRigidbody != null && targetRigidbody.isKinematic)
            return true;

        return false;
    }

    public void SetTargetObject(Transform target)
    {
        targetObject = target;
        targetCollider = target.GetComponent<Collider>();
        targetRigidbody = target.GetComponent<Rigidbody>();

        if (targetRigidbody == null)
        {
            Debug.LogError("Target object does not have a Rigidbody component.");
        }

        animator.SetBool("isWalking", true);
        animator.SetBool("isSitting", false);

        IgnoreCollisionWithDog(targetCollider);
    }

    private void MoveTowardsTarget(Vector3 targetPosition)
    {
        if (rb == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        if (distanceToTarget > fetchStopDistance)
        {
            // Move towards the target object
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0;

            // Avoid collisions with the player if the dog is close to them
            DogBehaviorManager behaviorManager = GetComponent<DogBehaviorManager>();
            if (behaviorManager != null && behaviorManager.player != null)
            {
                Vector3 toPlayer = behaviorManager.player.position - transform.position;
                float distanceToPlayer = toPlayer.magnitude;

                if (distanceToPlayer < 3f && Vector3.Dot(direction, toPlayer.normalized) > 0.5f)
                {
                    // Calculate an avoidance direction perpendicular to the player's direction
                    Vector3 avoidDirection = Vector3.Cross(Vector3.up, toPlayer.normalized);
                    if (Vector3.Dot(avoidDirection, direction) < 0)
                    {
                        avoidDirection = -avoidDirection;
                    }
                    // Blend the original and avoidance directions
                    direction = (direction + avoidDirection).normalized;
                }
            }

            // Apply rotation
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRotation, rotationSpeed * Time.deltaTime));

            // Apply movement
            Vector3 movePosition = direction * moveSpeed * Time.deltaTime;
            rb.MovePosition(transform.position + movePosition);

            animator.SetBool("isWalking", true);
        }
        else
        {
            PickUpObject();
        }
    }

    private void PickUpObject()
    {
        if (targetObject == null) return;

        DisablePhysics(targetObject);

        // Attach the target object to the dog's jaw
        targetObject.SetParent(jawPosition);
        targetObject.localPosition = Vector3.zero;
        targetObject.localRotation = Quaternion.identity;

        Debug.Log("Object picked up by the dog.");

        ResetBehavior();
    }

    private void ResetBehavior()
    {
        targetObject = null;
        targetCollider = null;
        targetRigidbody = null;

        // Reset the state in the DogBehaviorManager
        DogBehaviorManager behaviorManager = GetComponent<DogBehaviorManager>();
        if (behaviorManager != null)
        {
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            behaviorManager.FetchCompleted();
        }

        this.enabled = false;
    }

    private void DisablePhysics(Transform obj)
    {
        if (obj == null) return;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    private void IgnoreCollisionWithDog(Collider targetCollider)
    {
        if (targetCollider == null || dogColliders == null) return;

        foreach (var dogCollider in dogColliders)
        {
            if (dogCollider != null)
            {
                Physics.IgnoreCollision(dogCollider, targetCollider, true);
            }
        }
    }
}

