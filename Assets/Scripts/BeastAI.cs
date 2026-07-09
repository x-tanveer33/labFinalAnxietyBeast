using UnityEngine;
using UnityEngine.AI;

public class BeastAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float waitTimeAtPoint = 2f;

    [Header("Detection & Chase")]
    public float detectionRadius = 15f;
    public float chaseSpeed = 5f;
    public float attackRange = 2.5f;

    [Header("Attack")]
    public float attackCooldown = 1f;
    public float beastAttackDamage = 25f;

    [Header("Audio")]
    public AudioSource footstepAudioSource;
    public AudioSource growlAudioSource;
    public AudioClip footstepClip;
    public AudioClip growlClip;
    public float footstepInterval = 0.6f;

    [Header("Level Scaling")]
    public int currentLevel = 1;

    private NavMeshAgent agent;
    private Transform player;
    private Animator animator;
    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private float lastAttackTime = 0f;
    private float footstepTimer = 0f;
    private bool hasDestination = false;

    private enum State { Patrol, Chase, Attack }
    private State currentState = State.Patrol;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
        {
            Debug.LogError("No Player found! Tag your player as 'Player'");
            enabled = false;
            return;
        }

        // Apply level scaling
        float speedMultiplier = 1f + (currentLevel - 1) * 0.1f;
        patrolSpeed *= speedMultiplier;
        chaseSpeed *= speedMultiplier;
        agent.speed = patrolSpeed;

        // Set initial destination if we have patrol points
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[0].position);
            hasDestination = true;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                if (distanceToPlayer <= detectionRadius)
                {
                    currentState = State.Chase;
                    agent.speed = chaseSpeed;
                }
                break;

            case State.Chase:
                ChasePlayer();
                if (distanceToPlayer <= attackRange)
                {
                    currentState = State.Attack;
                }
                else if (distanceToPlayer > detectionRadius * 1.3f)
                {
                    currentState = State.Patrol;
                    agent.speed = patrolSpeed;
                    // Reset patrol destination
                    if (patrolPoints.Length > 0)
                    {
                        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                        hasDestination = true;
                    }
                }
                break;

            case State.Attack:
                AttackPlayer();
                if (distanceToPlayer > attackRange)
                {
                    currentState = State.Chase;
                    agent.isStopped = false;
                    if (animator != null)
                        animator.SetBool("isAttacking", false);
                }
                break;
        }

        // Footstep sounds
        if (footstepAudioSource != null && footstepClip != null && currentState != State.Attack)
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepInterval)
            {
                footstepTimer = 0f;
                footstepAudioSource.PlayOneShot(footstepClip);
            }
        }
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) 
        {
            Debug.LogWarning("No patrol points assigned!");
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                hasDestination = true;
                waitTimer = 0f;
            }
        }
        else if (!hasDestination)
        {
            // Make sure we have a destination
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            hasDestination = true;
        }
    }

    void ChasePlayer()
    {
        agent.SetDestination(player.position);

        // Growl when chasing
        if (growlAudioSource != null && growlClip != null && !growlAudioSource.isPlaying)
        {
            growlAudioSource.PlayOneShot(growlClip);
        }
    }

    void AttackPlayer()
    {
        agent.isStopped = true;

        // Smooth rotation toward player
        Vector3 direction = new Vector3(player.position.x, transform.position.y, player.position.z) - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        if (animator != null)
            animator.SetBool("isAttacking", true);

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            Debug.Log("BEAST ATTACKS!");

            // Deal damage to player instead of instant kill
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(beastAttackDamage);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}