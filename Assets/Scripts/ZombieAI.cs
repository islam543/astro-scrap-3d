using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    [Header("References")]
    public Transform playerTarget;

    [Header("Detection")]
    public float detectionRange = 50f;
    public float attackRange    = 1.8f;

    [Header("Combat")]
    public int   attackDamage   = 10;
    public float attackCooldown = 1.2f;

    [Header("Movement")]
    public float chaseSpeed  = 1.6f;
    public float rotateSpeed = 6f;

    // ── Animator parameter names (must match your Animator Controller) ──
    private static readonly int ParamSpeed  = Animator.StringToHash("Speed");
    private static readonly int ParamAttack = Animator.StringToHash("Attack");
    private static readonly int ParamDie    = Animator.StringToHash("Die");

    private NavMeshAgent  agent;
    private Animator      animator;
    private PlayerHealth  playerHealth;
    private float         attackTimer = 0f;
    private bool          isDead      = false;
    private bool          useNavMesh  = false;
    private bool          warnedMissingPlayer = false;
    private bool          warnedMissingPlayerHealth = false;

    // Cached parameter existence flags so we never spam warnings
    private bool hasParamSpeed;
    private bool hasParamAttack;
    private bool hasParamDie;

    enum State { Idle, Chase, Attack }
    private State state = State.Idle;

    private const float MinimumDetectionRange = 50f;

    // ────────────────────────────────────────────────────────────────────

    void Awake()
    {
        detectionRange = Mathf.Max(detectionRange, MinimumDetectionRange);
        NormalizeSphereColliderCenter();
    }

    void Start()
    {
        agent    = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        FindPlayer();
        SetupAnimator();
        SetupMovement();

        attackTimer = 0f;
        Debug.Log($"[ZombieAI] Ready. Detection={detectionRange}, AttackRange={attackRange}, " +
                  $"Damage={attackDamage}, Cooldown={attackCooldown}");
    }

    void Update()
    {
        if (isDead) return;

        if (playerTarget == null)
        {
            FindPlayer();
            StopMoving();
            SetState(State.Idle);
            return;
        }

        float dist = Vector3.Distance(transform.position, playerTarget.position);
        attackTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Idle:
                SetAnimSpeed(0f);
                StopMoving();
                if (dist <= detectionRange)
                {
                    Debug.Log($"[ZombieAI] Player detected at {dist:0.0}m.");
                    SetState(State.Chase);
                }
                break;

            case State.Chase:
                if (dist > detectionRange)
                {
                    Debug.Log($"[ZombieAI] Player left detection range at {dist:0.0}m. Returning to Idle.");
                    StopMoving();
                    SetState(State.Idle);
                    break;
                }

                if (dist <= attackRange)
                {
                    Debug.Log($"[ZombieAI] Reached attack range at {dist:0.0}m.");
                    StopMoving();
                    SetState(State.Attack);
                    break;
                }

                SetAnimSpeed(1f);
                ChasePlayer();
                break;

            case State.Attack:
                SetAnimSpeed(0f);
                if (dist > attackRange)
                {
                    Debug.Log($"[ZombieAI] Player moved out of attack range at {dist:0.0}m. Chasing again.");
                    SetState(State.Chase);
                    break;
                }

                AttackPlayer();
                break;
        }
    }

    // ── Animator helpers ─────────────────────────────────────────────────

    void SetupAnimator()
    {
        hasParamSpeed  = false;
        hasParamAttack = false;
        hasParamDie    = false;

        if (animator == null)
        {
            Debug.Log("[ZombieAI] No Animator found. Zombie logic will still work without animations.");
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[ZombieAI] Animator has no Runtime Animator Controller assigned. " +
                             "Please create one and assign it to the Animator component.");
            return;
        }

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.nameHash == ParamSpeed)  hasParamSpeed  = true;
            if (p.nameHash == ParamAttack) hasParamAttack = true;
            if (p.nameHash == ParamDie)    hasParamDie    = true;
        }

        Debug.Log($"[ZombieAI] Animator '{animator.runtimeAnimatorController.name}' found. " +
                  $"Speed={hasParamSpeed}, Attack={hasParamAttack}, Die={hasParamDie}");

        if (!hasParamSpeed || !hasParamAttack || !hasParamDie)
            Debug.LogWarning("[ZombieAI] Animator is missing parameters. " +
                             "Add Float 'Speed', Trigger 'Attack', Trigger 'Die' in the Animator Controller.");
    }

    void SetAnimSpeed(float value)
    {
        if (animator != null && hasParamSpeed)
            animator.SetFloat(ParamSpeed, value);
    }

    void TriggerAttackAnim()
    {
        if (animator != null && hasParamAttack)
            animator.SetTrigger(ParamAttack);
    }

    public void TriggerDeathAnim()
    {
        if (animator != null && hasParamDie)
            animator.SetTrigger(ParamDie);
    }

    // ── AI methods ───────────────────────────────────────────────────────

    void FindPlayer()
    {
        if (playerTarget != null)
        {
            playerHealth = playerTarget.GetComponent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = playerTarget.GetComponentInChildren<PlayerHealth>();
            return;
        }

        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null)
        {
            if (!warnedMissingPlayer)
            {
                warnedMissingPlayer = true;
                Debug.LogWarning("[ZombieAI] No GameObject tagged Player found. Zombie will stay idle.");
            }
            return;
        }

        warnedMissingPlayer = false;
        playerTarget = playerGO.transform;
        playerHealth = playerGO.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = playerGO.GetComponentInChildren<PlayerHealth>();

        Debug.Log($"[ZombieAI] Player target found: {playerGO.name}");
        if (playerHealth == null)
            Debug.LogWarning("[ZombieAI] Player found, but PlayerHealth is missing. Attacks will not damage the player.");
    }

    void SetupMovement()
    {
        if (agent == null)
        {
            Debug.Log("[ZombieAI] No NavMeshAgent found. Using direct movement.");
            return;
        }

        NavMeshHit hit;
        useNavMesh = agent.enabled && NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas);
        if (useNavMesh)
        {
            agent.speed            = chaseSpeed;
            agent.stoppingDistance = attackRange;
            agent.angularSpeed     = 360f;
            Debug.Log("[ZombieAI] NavMeshAgent ready.");
        }
        else
        {
            agent.enabled = false;
            Debug.Log("[ZombieAI] No baked NavMesh under zombie. Using direct movement.");
        }
    }

    void ChasePlayer()
    {
        if (playerTarget == null) return;

        FaceTarget(playerTarget.position);

        if (useNavMesh && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(playerTarget.position);
            return;
        }

        // Fallback: simple transform movement
        Vector3 dir = playerTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.position += dir.normalized * chaseSpeed * Time.deltaTime;
    }

    void AttackPlayer()
    {
        StopMoving();
        if (playerTarget != null)
            FaceTarget(playerTarget.position);

        if (attackTimer > 0f) return;

        attackTimer = attackCooldown;
        TriggerAttackAnim();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log($"[ZombieAI] Attacked player for {attackDamage} damage.");
        }
        else
        {
            if (!warnedMissingPlayerHealth)
            {
                warnedMissingPlayerHealth = true;
                Debug.LogWarning("[ZombieAI] Attack happened, but PlayerHealth is missing. No damage applied.");
            }
        }
    }

    void StopMoving()
    {
        if (useNavMesh && agent != null && agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();
    }

    void SetState(State newState)
    {
        if (state == newState) return;
        state = newState;
        Debug.Log($"[ZombieAI] State changed to {state}.");
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0;
        if (dir == Vector3.zero) return;
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, rotateSpeed * Time.deltaTime);
    }

    // Called by ZombieHealth on death
    public void OnDead()
    {
        if (isDead) return;
        isDead = true;
        StopMoving();
        if (agent != null) agent.enabled = false;
        TriggerDeathAnim();
        Debug.Log("[ZombieAI] Dead. AI stopped.");
        this.enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    void NormalizeSphereColliderCenter()
    {
        foreach (SphereCollider col in GetComponentsInChildren<SphereCollider>(true))
        {
            Vector3 center = col.center;
            center.y = 1f;
            col.center = center;
        }
    }
}
