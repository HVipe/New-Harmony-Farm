using System.Collections;
using UnityEngine;

public class TimedLocalForwardMovement : MonoBehaviour
{
    public Transform player;
    public float detectionRadius = 5f;
    public float maxEscapeSpeed = 5f;
    public float minMoveDistance = 1f;
    public float maxMoveDistance = 3f;
    public float minMoveTime = 2f;
    public float maxMoveTime = 4f;
    public float moveSpeed = 2f;
    public float turnSpeed = 2f;
    public float rayDistance = 2f;
    public LayerMask obstacleLayer;

    public float attractionRadius = 8f;
    public float attractionStayDuration = 3f;
    public float immuneTimeAfterPecking = 5f;
    public int peckingCount = 3;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isEscaping = false;
    private bool isAttracted = false;
    private bool immuneToAttraction = false;
    private float initialYPosition;
    private Animator animator;
    private Transform attractionTarget;
    private int currentPeckingCount;

    void Start()
    {
        targetPosition = transform.position;
        initialYPosition = transform.position.y;
        animator = GetComponent<Animator>();

        float randomDelay = Random.Range(0f, 1f);
        Invoke("StartRandomMovement", randomDelay);
    }

    void StartRandomMovement()
    {
        StartCoroutine(RandomMovement());
    }

    void Update()
    {
        DetectPlayer();

        if (!isEscaping && !immuneToAttraction && !isAttracted)
        {
            DetectAttractiveTarget();
        }

        if (isMoving || isEscaping || isAttracted)
        {
            animator.speed = isEscaping ? Mathf.Lerp(1, maxEscapeSpeed / moveSpeed, 0.5f) : 1;
        }
        else
        {
            animator.speed = 0;
        }

        if (isEscaping)
        {
            EscapeFromPlayer();
        }
        else if (isAttracted)
        {
            MoveTowardsAttraction();
        }
        else if (isMoving)
        {
            MoveTowardsTarget();
        }
    }

    void DetectAttractiveTarget()
    {
        if (immuneToAttraction) return; // �����������ʱ�Σ�ֱ�ӷ���

        Collider[] colliders = Physics.OverlapSphere(transform.position, attractionRadius);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("attractive"))
            {
                attractionTarget = collider.transform;
                isAttracted = true;
                isMoving = false;
                break;
            }
        }
    }


    void MoveTowardsAttraction()
    {
        if (attractionTarget == null)
        {
            isAttracted = false;
            return;
        }

        Vector3 direction = (attractionTarget.position - transform.position).normalized;

        AdjustDirectionIfObstacle(ref direction); // ���������Աܿ��ϰ���

        Vector3 newPosition = transform.position + direction * moveSpeed * Time.deltaTime;
        newPosition.y = initialYPosition;

        if (!IsOtherChickenAtPosition(newPosition))
        {
            transform.position = newPosition; // ֱ���޸� transform.position
        }

        SmoothRotate(direction);

        if (Vector3.Distance(transform.position, attractionTarget.position) < 0.5f)
        {
            StartCoroutine(PerformPecking());
        }
    }

    void MoveTowardsTarget()
    {
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance < 0.1f)
        {
            isMoving = false;
            transform.position = targetPosition;
            return;
        }

        Vector3 direction = (targetPosition - transform.position).normalized;

        AdjustDirectionIfObstacle(ref direction); // ���������Աܿ��ϰ���

        Vector3 newPosition = transform.position + direction * moveSpeed * Time.deltaTime;
        newPosition.y = initialYPosition;

        if (!IsOtherChickenAtPosition(newPosition))
        {
            transform.position = newPosition; // ֱ���޸� transform.position
        }

        SmoothRotate(direction);
    }

    void EscapeFromPlayer()
    {
        isMoving = true;
        Vector3 directionAwayFromPlayer = (transform.position - player.position).normalized;

        AdjustDirectionIfObstacle(ref directionAwayFromPlayer); // ���������Աܿ��ϰ���

        Vector3 escapePosition = transform.position + directionAwayFromPlayer * maxEscapeSpeed * Time.deltaTime;
        escapePosition.y = initialYPosition;

        if (!IsOtherChickenAtPosition(escapePosition))
        {
            transform.position = escapePosition; // ֱ���޸� transform.position
        }

        SmoothRotate(directionAwayFromPlayer);
    }

    void AdjustDirectionIfObstacle(ref Vector3 direction)
    {
        float angleStep = 15f; // ÿ�ε����ĽǶ�
        int maxChecks = 12; // �������� (180�� / 15��)

        for (int i = 0; i < maxChecks; i++)
        {
            // ��鵱ǰ�����Ƿ����ϰ��������С��
            if (!Physics.Raycast(transform.position + Vector3.up * 0.1f, direction, rayDistance, obstacleLayer) &&
                !IsOtherChickenInDirection(direction))
            {
                return; // ��ǰ����û���ϰ��������С��������ԭ����
            }

            // �����ǰ�������ϰ��������С����˳ʱ�����ʱ���������
            direction = Quaternion.Euler(0, angleStep, 0) * direction;
        }

        // ������з��򶼱��赲���򱣳�ԭ����������
    }

    bool IsOtherChickenInDirection(Vector3 direction)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, direction, out hit, rayDistance))
        {
            if (hit.collider.CompareTag("Chicken"))
            {
                return true; // ��⵽����С��
            }
        }
        return false;
    }

    bool IsOtherChickenAtPosition(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.5f); // ���Ŀ��λ����Χ0.5�׷�Χ���Ƿ�������С��
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Chicken") && collider.transform != transform)
            {
                return true; // ��⵽����С��
            }
        }
        return false;
    }

    void SmoothRotate(Vector3 direction)
    {
        if (direction == Vector3.zero) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
    }

    void DetectPlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= detectionRadius && !isEscaping)
        {
            isEscaping = true;
            animator.ResetTrigger("PeckTrigger");
            animator.SetBool("isWalking", false);
        }
    }

    IEnumerator RandomMovement()
    {
        while (true)
        {
            if (!isEscaping && !isMoving)
            {
                Vector3 randomDirection = GetValidRandomDirection();
                float randomMoveDistance = Random.Range(minMoveDistance, maxMoveDistance);
                targetPosition = transform.position + randomDirection * randomMoveDistance;
                targetPosition.y = initialYPosition;

                isMoving = true;
                animator.SetBool("isWalking", true);
                SmoothRotate(randomDirection);

                yield return new WaitUntil(() => !isMoving);
                float waitTime = Random.Range(minMoveTime, maxMoveTime);
                yield return new WaitForSeconds(waitTime);
            }
            yield return null;
        }
    }

    IEnumerator PerformPecking()
    {
        isAttracted = false;
        currentPeckingCount = 0;
        isMoving = false;
        animator.SetBool("isWalking", false);

        while (currentPeckingCount < peckingCount)
        {
            if (isEscaping) yield break;
            animator.SetTrigger("PeckTrigger");
            yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
            currentPeckingCount++;
        }

        animator.SetTrigger("WalkTrigger");
        immuneToAttraction = true;
        StartCoroutine(ImmuneToAttractionCooldown());
        StartCoroutine(RandomMovement());
    }

    IEnumerator ImmuneToAttractionCooldown()
    {
        yield return new WaitForSeconds(immuneTimeAfterPecking);
        immuneToAttraction = false;
    }

    Vector3 GetValidRandomDirection()
    {
        Vector3 randomDirection = transform.forward;
        for (int i = 0; i < 10; i++)
        {
            randomDirection = Random.insideUnitSphere.normalized;
            randomDirection.y = 0;

            if (!Physics.Raycast(transform.position + Vector3.up * 0.1f, randomDirection, rayDistance, obstacleLayer))
            {
                break;
            }
        }

        return randomDirection;
    }
}