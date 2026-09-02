using UnityEngine;
using UnityEngine.AI;

public class enemyAI : MonoBehaviour
{
    public Transform player;
    public float visionDist = 10f;
    public float visionAngle = 360f;
    public float detecLimit = 2f;
    private float detecTimer = 0f;

    public float searchTimer = 0f;
    public float searchLimit = 2f;

    public float detectionGraceTime = 0.2f;
    private float detectionLostTimer = 0f;

    public Transform eyePoint;
    public Transform[] patrolPoints;
    public float patrolPointDistance = 1f;

    public float lostSightGraceTime = 0.2f;
    public float lostSightTimer = 0f;

    [Header("Chase Audio")]
    public AudioSource audioSource;
    public AudioClip chaseAudio;

    private int currentPatrolPoint = 0;
    private NavMeshAgent enemy;

    public enum STATE
    {
        IDLE,
        DETECTING,
        CHASING,
        SEARCHING
    }

    private STATE currentState = STATE.IDLE;
    private Vector3 lastKnownPosition;

    public STATE GetCurrentState()
    {
        return currentState;
    }

    public float GetDetectionProgress()
    {
        if (detecLimit <= 0f)
            return 1f;

        return detecTimer / detecLimit;
    }

    public float GetSearchProgress()
    {
        if (searchLimit <= 0f)
            return 1f;

        return searchTimer / searchLimit;
    }

    void Start()
    {
        enemy = GetComponent<NavMeshAgent>();

        if (audioSource != null)
        {
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.Stop();
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case STATE.IDLE:
                Idle();
                break;

            case STATE.DETECTING:
                Detecting();
                break;

            case STATE.CHASING:
                Chasing();
                break;

            case STATE.SEARCHING:
                Searching();
                break;
        }
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0)
            return;

        enemy.SetDestination(patrolPoints[currentPatrolPoint].position);

        if (enemy.remainingDistance <= patrolPointDistance)
        {
            currentPatrolPoint++;

            if (currentPatrolPoint >= patrolPoints.Length)
                currentPatrolPoint = 0;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (eyePoint == null)
            return;

        Gizmos.color = Color.yellow;

        Vector3 forward = eyePoint.forward * visionDist;
        Gizmos.DrawRay(eyePoint.position, forward);

        Quaternion leftRotation =
            Quaternion.Euler(0, -visionAngle / 2f, 0);

        Quaternion rightRotation =
            Quaternion.Euler(0, visionAngle / 2f, 0);

        Vector3 leftDirection =
            leftRotation * eyePoint.forward * visionDist;

        Vector3 rightDirection =
            rightRotation * eyePoint.forward * visionDist;

        Gizmos.DrawRay(eyePoint.position, leftDirection);
        Gizmos.DrawRay(eyePoint.position, rightDirection);
    }

    bool CanSeePlayer()
    {
        Vector3 directionToPlayer =
            player.position - eyePoint.position;

        float distanceToPlayer =
            directionToPlayer.magnitude;

        if (distanceToPlayer > visionDist)
            return false;

        Debug.DrawRay(
            eyePoint.position,
            directionToPlayer.normalized * distanceToPlayer,
            Color.red
        );

        if (Physics.Raycast(
            eyePoint.position,
            directionToPlayer.normalized,
            out RaycastHit hit,
            distanceToPlayer))
        {
            if (hit.collider.CompareTag("Player"))
                return true;
        }

        return false;
    }

    void Idle()
    {
        StopChaseAudio();

        Patrol();

        if (CanSeePlayer())
        {
            detecTimer = 0f;
            currentState = STATE.DETECTING;
        }
    }

    void Detecting()
    {
        StopChaseAudio();

        if (CanSeePlayer())
        {
            detecTimer += Time.deltaTime;
            detectionLostTimer = 0f;

            if (detecTimer >= detecLimit)
            {
                lastKnownPosition = player.position;
                detecTimer = 0f;

                currentState = STATE.CHASING;

                PlayChaseAudio();
            }
        }
        else
        {
            detectionLostTimer += Time.deltaTime;

            if (detectionLostTimer >= detectionGraceTime)
            {
                detecTimer = 0f;
                detectionLostTimer = 0f;
                currentState = STATE.IDLE;
            }
        }
    }

    void Chasing()
    {
        PlayChaseAudio();

        if (CanSeePlayer())
        {
            lostSightTimer = 0f;

            lastKnownPosition = player.position;
            enemy.SetDestination(lastKnownPosition);
        }
        else
        {
            lostSightTimer += Time.deltaTime;

            if (lostSightTimer >= lostSightGraceTime)
            {
                lostSightTimer = 0f;

                StopChaseAudio();

                currentState = STATE.SEARCHING;
                searchTimer = 0f;

                enemy.SetDestination(lastKnownPosition);
            }
        }
    }

    void Searching()
    {
        StopChaseAudio();

        enemy.SetDestination(lastKnownPosition);

        if (CanSeePlayer())
        {
            lastKnownPosition = player.position;
            currentState = STATE.CHASING;
            lostSightTimer = 0f;

            PlayChaseAudio();
            return;
        }

        searchTimer += Time.deltaTime;

        if (searchTimer >= searchLimit)
        {
            currentState = STATE.IDLE;
            searchTimer = 0f;
            detecTimer = 0f;

            enemy.ResetPath();

            StopChaseAudio();
        }
    }

    void PlayChaseAudio()
    {
        if (audioSource == null || chaseAudio == null)
            return;

        if (!audioSource.isPlaying)
        {
            audioSource.clip = chaseAudio;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void StopChaseAudio()
    {
        if (audioSource == null)
            return;

        if (audioSource.isPlaying)
            audioSource.Stop();
    }
}