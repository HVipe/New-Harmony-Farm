using UnityEngine;
using System.Collections.Generic;

public class DogBehaviorManager : MonoBehaviour
{
    [Header("Behavior Scripts")]
    public DogRandomWalk randomWalk;
    public DogFollow follow;
    public DogFetchGame fetchGame;
    public Transform player;

    [Header("Settings")]
    public float switchDistance = 10f;
    public float fetchDetectionRange = 15f;
    public float groundVelocityThreshold = 0.1f;
    public float fetchCooldown = 1f; // Cooldown time for the fetch behavior

    private Dictionary<Transform, Vector3> fetchObjectPositions = new Dictionary<Transform, Vector3>();
    private bool isFetching = false;
    private float lastFetchTime = 0f; // Tracks the time of the last fetch completion

    private void Awake()
    {
        if (randomWalk == null) randomWalk = GetComponent<DogRandomWalk>();
        if (follow == null) follow = GetComponent<DogFollow>();
        if (fetchGame == null) fetchGame = GetComponent<DogFetchGame>();
    }

    private void Start()
    {
        if (player != null && follow != null)
        {
            follow.player = player;
        }
        InitializeBehaviors();
    }

    public void InitializeBehaviors()
    {
        // Clear previous states
        fetchObjectPositions.Clear();

        if (randomWalk != null)
        {
            randomWalk.enabled = false;
        }
        if (follow != null)
        {
            follow.enabled = true;
        }
        if (fetchGame != null)
        {
            fetchGame.enabled = false;
        }

        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isSitting", false); // Reset sitting state
        }
    }

    private void Update()
    {
        if (player == null) return;

        if (isFetching)
        {
            // Check if fetch is completed
            if (fetchGame != null && !fetchGame.enabled)
            {
                FetchCompleted();
            }
            return;
        }

        // Check if new fetch can be initiated (respecting cooldown)
        if (Time.time - lastFetchTime >= fetchCooldown)
        {
            if (follow != null && follow.enabled && DetectFetchObject())
            {
                isFetching = true;
                return;
            }
        }

        // Switch between follow and random walk behaviors based on player distance
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= switchDistance)
        {
            EnableFollowBehavior();
        }
        else
        {
            EnableRandomWalkBehavior();
        }
    }

    private bool DetectFetchObject()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, fetchDetectionRange);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Fetch"))
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Transform fetchObject = col.transform;

                    // Record the object's position if not already tracked
                    if (!fetchObjectPositions.ContainsKey(fetchObject))
                    {
                        fetchObjectPositions[fetchObject] = fetchObject.position;
                        return false; // First detection, record position but do not trigger fetch
                    }

                    // Check if the object has moved and is now stationary
                    bool hasMoved = Vector3.Distance(fetchObjectPositions[fetchObject], fetchObject.position) > 0.1f;
                    bool isStopped = rb.velocity.magnitude <= groundVelocityThreshold;

                    if (hasMoved && isStopped)
                    {
                        EnableFetchGame(fetchObject);
                        // Update position record
                        fetchObjectPositions[fetchObject] = fetchObject.position;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private void EnableFetchGame(Transform fetchObject)
    {
        if (randomWalk != null)
        {
            randomWalk.enabled = false;
        }
        if (follow != null)
        {
            follow.enabled = false;
        }

        if (fetchGame != null)
        {
            fetchGame.enabled = true;
            fetchGame.SetTargetObject(fetchObject);
        }
    }

    private void EnableFollowBehavior()
    {
        if (randomWalk != null && randomWalk.enabled)
        {
            randomWalk.enabled = false;
        }

        if (follow != null && !follow.enabled)
        {
            follow.enabled = true;
        }
    }

    private void EnableRandomWalkBehavior()
    {
        if (follow != null && follow.enabled)
        {
            follow.enabled = false;
        }

        if (randomWalk != null && !randomWalk.enabled)
        {
            randomWalk.enabled = true;
        }

        // Ensure the walking animation is correctly set
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("isWalking", true);
            animator.SetBool("isSitting", false);
        }
    }

    public void FetchCompleted()
    {
        isFetching = false;
        lastFetchTime = Time.time; // Record the completion time
        InitializeBehaviors();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, switchDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, fetchDetectionRange);
    }
}
