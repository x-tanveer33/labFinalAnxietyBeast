using System;
using UnityEngine;

/// <summary>
/// Drives the beast Animator from <see cref="BeastAI"/> FSM states (Patrol, Chase, Attack).
/// Attach to the same GameObject as BeastAI; expects an Animator on this object or a child.
/// </summary>
[RequireComponent(typeof(BeastAI))]
public class BeastAnimatorFSM : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsChasingHash = Animator.StringToHash("IsChasing");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    private static readonly int StateHash = Animator.StringToHash("State");

    [Header("Animator State Names")]
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string walkStateName = "Walk";
    [SerializeField] private string chaseStateName = "Chase";
    [SerializeField] private string attackStateName = "Attack";

    [Header("Playback")]
    [SerializeField] private float patrolAnimSpeed = 0.65f;
    [SerializeField] private float chaseAnimSpeed = 1.15f;
    [SerializeField] private float crossFadeDuration = 0.2f;
    [SerializeField] private float idleSpeedThreshold = 0.05f;

    private BeastAI beastAI;
    private Animator animator;
    private BeastAI.BeastState lastAnimState;

    private void Awake()
    {
        beastAI = GetComponent<BeastAI>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        EnsureAnimatorController();
        beastAI.OnStateChanged += HandleFsmStateChanged;
        SyncAnimationToFsm(force: true);
    }

    private void OnDestroy()
    {
        if (beastAI != null)
            beastAI.OnStateChanged -= HandleFsmStateChanged;
    }

    private void Update()
    {
        if (animator == null || !animator.isActiveAndEnabled || beastAI == null)
            return;

        float speed = beastAI.CurrentMoveSpeed;
        bool isMoving = speed > idleSpeedThreshold;

        animator.SetFloat(SpeedHash, speed);
        animator.SetBool(IsMovingHash, isMoving);
        animator.SetBool(IsChasingHash, beastAI.CurrentState == BeastAI.BeastState.Chase);
        animator.SetBool(IsAttackingHash, beastAI.CurrentState == BeastAI.BeastState.Attack);
        animator.SetInteger(StateHash, (int)beastAI.CurrentState);

        if (beastAI.CurrentState == BeastAI.BeastState.Patrol)
            SyncPatrolLocomotion(isMoving);
    }

    private void HandleFsmStateChanged(BeastAI.BeastState state)
    {
        SyncAnimationToFsm(force: true);
    }

    private void SyncAnimationToFsm(bool force)
    {
        if (animator == null || beastAI == null)
            return;

        BeastAI.BeastState state = beastAI.CurrentState;
        if (!force && state == lastAnimState)
            return;

        lastAnimState = state;

        switch (state)
        {
            case BeastAI.BeastState.Patrol:
                SyncPatrolLocomotion(beastAI.CurrentMoveSpeed > idleSpeedThreshold);
                break;
            case BeastAI.BeastState.Chase:
                CrossFadeTo(chaseStateName, walkStateName, chaseAnimSpeed);
                break;
            case BeastAI.BeastState.Attack:
                CrossFadeTo(attackStateName, string.Empty, 1f);
                break;
        }
    }

    private void SyncPatrolLocomotion(bool isMoving)
    {
        if (beastAI.CurrentState != BeastAI.BeastState.Patrol)
            return;

        if (isMoving)
            CrossFadeTo(walkStateName, idleStateName, patrolAnimSpeed);
        else
            CrossFadeTo(idleStateName, walkStateName, 1f);
    }

    private void CrossFadeTo(string primaryState, string fallbackState, float playbackSpeed)
    {
        string targetState = ResolveStateName(primaryState, fallbackState);
        if (string.IsNullOrEmpty(targetState))
            return;

        animator.speed = playbackSpeed;
        animator.CrossFade(targetState, crossFadeDuration, 0);
    }

    private string ResolveStateName(string primaryState, string fallbackState)
    {
        if (HasState(primaryState))
            return primaryState;
        if (!string.IsNullOrEmpty(fallbackState) && HasState(fallbackState))
            return fallbackState;
        return null;
    }

    private bool HasState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
            return false;

        int hash = Animator.StringToHash(stateName);
        return animator.HasState(0, hash);
    }

    private void EnsureAnimatorController()
    {
        if (animator == null)
        {
            Debug.LogWarning("[BeastAnimatorFSM] No Animator found on beast or its children.");
            return;
        }

        if (animator.runtimeAnimatorController != null && animator.runtimeAnimatorController.name != "Missing")
            return;

        RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>("BeastAnimator");
        if (controller == null)
            controller = Resources.Load<RuntimeAnimatorController>("Beastanimator");

        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
            Debug.Log("[BeastAnimatorFSM] Assigned beast animator controller from Resources.");
        }
        else
        {
            Debug.LogWarning("[BeastAnimatorFSM] No beast animator controller found in Resources.");
        }
    }
}
