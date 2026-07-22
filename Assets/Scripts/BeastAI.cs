using System;
using UnityEngine;
using UnityEngine.AI;

public class BeastAI : MonoBehaviour
{
    public enum BeastState { Patrol, Chase, Attack }
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float waitTimeAtPoint = 2f;

    [Header("Detection & Chase")]
    public float detectionRadius = 8.5f;
    public float loseChaseDistance = 10f;
    public float chaseSpeed = 5f;
    public float attackRange = 2.2f;

    [Header("Attack")]
    public float attackCooldown = 1.85f;
    public float attackDamageDelay = 0.85f;
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
    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private float lastAttackTime = 0f;
    private float footstepTimer = 0f;
    private bool hasDestination = false;
    private bool isPatrolMoving = false;
    private bool isAttackingInProgress = false;

    private BeastState currentState = BeastState.Patrol;

    public BeastState CurrentState => currentState;
    public event Action<BeastState> OnStateChanged;

    public bool IsMoving
    {
        get
        {
            if (currentState == BeastState.Attack)
                return false;

            if (currentState == BeastState.Chase)
                return true;

            if (currentState == BeastState.Patrol)
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    return !agent.isStopped && agent.hasPath && agent.remainingDistance > 0.5f;
                }
                return isPatrolMoving;
            }

            return false;
        }
    }

    public float CurrentMoveSpeed
    {
        get
        {
            if (currentState == BeastState.Attack)
                return 0f;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
                return agent.velocity.magnitude;

            if (currentState == BeastState.Chase)
                return chaseSpeed;

            return isPatrolMoving ? patrolSpeed : 0f;
        }
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        // Snap initial transform to ground/terrain
        Vector3 initialPos = transform.position;
        RaycastHit groundHit;
        Vector3 checkOrigin = new Vector3(initialPos.x, initialPos.y + 10f, initialPos.z);
        if (Physics.Raycast(checkOrigin, Vector3.down, out groundHit, 30f))
        {
            initialPos.y = groundHit.point.y;
            transform.position = initialPos;
            Debug.Log("[BeastAI] Snapped initial transform to ground: " + initialPos);
        }
        else if (Terrain.activeTerrain != null)
        {
            initialPos.y = Terrain.activeTerrain.SampleHeight(initialPos) + Terrain.activeTerrain.transform.position.y;
            transform.position = initialPos;
            Debug.Log("[BeastAI] Snapped initial transform to Terrain: " + initialPos);
        }

        if (agent != null) agent.enabled = true;

        if (GetComponent<BeastAnimatorFSM>() == null)
            gameObject.AddComponent<BeastAnimatorFSM>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            playerObj = GameObject.Find("ThirdPersonController");
        }
        if (playerObj == null)
        {
            CharacterController cc = FindAnyObjectByType<CharacterController>();
            if (cc != null) playerObj = cc.gameObject;
        }

        if (playerObj != null)
        {
            player = playerObj.transform;
            playerObj.tag = "Player";
            Debug.Log("[BeastAI] Found and bound player: " + playerObj.name);
        }
        else
        {
            Debug.LogError("[BeastAI] No Player found!");
            enabled = false;
            return;
        }

        // Apply level scaling
        float speedMultiplier = 1f + (currentLevel - 1) * 0.1f;
        patrolSpeed *= speedMultiplier;
        chaseSpeed *= speedMultiplier;

        // Check if agent is on NavMesh; if not, disable NavMeshAgent component to prevent console spam
        if (agent != null)
        {
            if (!agent.isOnNavMesh)
            {
                agent.enabled = false;
                Debug.LogWarning("[BeastAI] Agent is not on NavMesh. Disabling agent component and using manual movement fallback.");
            }
            else
            {
                agent.speed = patrolSpeed;
                StartCoroutine(CalibrateBaseOffset());
            }
        }

        // Initial setup for patrol destination if we have points and NavMesh
        if (patrolPoints.Length > 0 && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(patrolPoints[0].position);
            hasDestination = true;
        }
    }

    private System.Collections.IEnumerator CalibrateBaseOffset()
    {
        // Wait one frame for NavMeshAgent to snap to NavMesh surface
        yield return null;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            RaycastHit groundHit;
            Vector3 checkPos = transform.position;
            Vector3 checkOrigin = new Vector3(checkPos.x, checkPos.y + 5f, checkPos.z);
            if (Physics.Raycast(checkOrigin, Vector3.down, out groundHit, 15f))
            {
                float diff = transform.position.y - groundHit.point.y;
                agent.baseOffset -= diff;
                Debug.Log("[BeastAI] Auto-calibrated agent baseOffset by: " + (-diff) + ". New baseOffset: " + agent.baseOffset);
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        // Ground raycast snapping to prevent the Beast from floating in the air
        RaycastHit groundHit;
        Vector3 checkOrigin = new Vector3(transform.position.x, transform.position.y + 5f, transform.position.z);
        if (Physics.Raycast(checkOrigin, Vector3.down, out groundHit, 15f))
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                Vector3 pos = transform.position;
                pos.y = groundHit.point.y;
                transform.position = pos;
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case BeastState.Patrol:
                Patrol();
                if (distanceToPlayer <= detectionRadius)
                    SetState(BeastState.Chase);
                break;

            case BeastState.Chase:
                ChasePlayer();
                if (distanceToPlayer <= attackRange)
                    SetState(BeastState.Attack);
                else if (distanceToPlayer > loseChaseDistance)
                {
                    SetState(BeastState.Patrol);
                    hasDestination = false;
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.ResetPath();
                    }
                    Debug.Log("[BeastAI] Distance created (" + distanceToPlayer.ToString("F1") + "m > " + loseChaseDistance + "m). Stopping chase.");
                }
                break;

            case BeastState.Attack:
                AttackPlayer();
                if (distanceToPlayer > attackRange)
                {
                    SetState(BeastState.Chase);
                    bool useNavMesh = (agent != null && agent.enabled && agent.isOnNavMesh);
                    if (useNavMesh)
                        agent.isStopped = false;
                }
                break;
        }

        // Footstep sounds (only when moving and not attacking)
        if (footstepAudioSource != null && footstepClip != null && currentState != BeastState.Attack)
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepInterval)
            {
                footstepTimer = 0f;
                footstepAudioSource.PlayOneShot(footstepClip);
            }
        }
    }

    private void MoveTowardsTarget(Vector3 targetPos, float speed)
    {
        bool useNavMesh = (agent != null && agent.enabled && agent.isOnNavMesh);
        if (useNavMesh)
        {
            agent.isStopped = false;
            agent.speed = speed;
            agent.SetDestination(targetPos);
        }
        else
        {
            // Rotate towards target (horizontal only)
            Vector3 direction = (targetPos - transform.position);
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
            // Move transform directly horizontally
            Vector3 nextPos = Vector3.MoveTowards(transform.position, new Vector3(targetPos.x, transform.position.y, targetPos.z), speed * Time.deltaTime);

            // Snap directly to the terrain/floor mesh to ensure feet touch the ground and don't float in the air
            RaycastHit hit;
            Vector3 origin = new Vector3(nextPos.x, nextPos.y + 10f, nextPos.z);
            if (Physics.Raycast(origin, Vector3.down, out hit, 30f))
            {
                nextPos.y = hit.point.y;
            }
            else if (Terrain.activeTerrain != null)
            {
                nextPos.y = Terrain.activeTerrain.SampleHeight(nextPos) + Terrain.activeTerrain.transform.position.y;
            }
            transform.position = nextPos;
        }
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) 
        {
            isPatrolMoving = false;
            Debug.LogWarning("[BeastAI] No patrol points assigned!");
            return;
        }

        Vector3 targetPos = patrolPoints[currentPatrolIndex].position;
        bool useNavMesh = (agent != null && agent.enabled && agent.isOnNavMesh);

        if (useNavMesh)
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                isPatrolMoving = false;
                waitTimer += Time.deltaTime;
                if (waitTimer >= waitTimeAtPoint)
                {
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                    agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                    hasDestination = true;
                    waitTimer = 0f;
                }
            }
            else
            {
                isPatrolMoving = true;
                if (!hasDestination)
                {
                    agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                    hasDestination = true;
                }
            }
        }
        else
        {
            float distanceToTarget = Vector3.Distance(new Vector3(transform.position.x, targetPos.y, transform.position.z), targetPos);
            if (distanceToTarget < 0.8f)
            {
                isPatrolMoving = false;
                waitTimer += Time.deltaTime;
                if (waitTimer >= waitTimeAtPoint)
                {
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                    waitTimer = 0f;
                }
            }
            else
            {
                isPatrolMoving = true;
                MoveTowardsTarget(targetPos, patrolSpeed);
            }
        }
    }

    void ChasePlayer()
    {
        MoveTowardsTarget(player.position, chaseSpeed);

        // Growl when chasing
        if (growlAudioSource != null && growlClip != null && !growlAudioSource.isPlaying)
        {
            growlAudioSource.PlayOneShot(growlClip);
        }
    }

    void AttackPlayer()
    {
        bool useNavMesh = (agent != null && agent.enabled && agent.isOnNavMesh);
        if (useNavMesh)
        {
            agent.isStopped = true;
        }

        // Smooth rotation toward player
        Vector3 direction = new Vector3(player.position.x, transform.position.y, player.position.z) - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        if (!isAttackingInProgress && Time.time - lastAttackTime >= attackCooldown)
        {
            StartCoroutine(PerformAttackCycle());
        }
    }

    private System.Collections.IEnumerator PerformAttackCycle()
    {
        isAttackingInProgress = true;
        lastAttackTime = Time.time;

        Debug.Log("[BeastAI] Starting attack animation strike...");

        // Wait until the attack animation reaches its strike impact / end
        yield return new WaitForSeconds(attackDamageDelay);

        // Apply damage ONCE when the strike lands
        if (player != null && currentState == BeastState.Attack)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= attackRange * 1.5f)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(beastAttackDamage);
                    Debug.Log("[BeastAI] Attack strike landed! Dealt " + beastAttackDamage + " damage to player.");
                }
            }
        }

        // Wait for the remainder of the attack animation and cooldown
        float remainingCooldown = Mathf.Max(0.1f, attackCooldown - attackDamageDelay);
        yield return new WaitForSeconds(remainingCooldown);

        isAttackingInProgress = false;
    }

    private void SetState(BeastState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}