using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 20f;
    public float attackRange    = 1.8f;

    [Header("Combat")]
    public int   attackDamage   = 10;
    public float attackCooldown = 1.2f;

    [Header("Movement")]
    public float chaseSpeed  = 3f;
    public float rotateSpeed = 6f;

    // ── internal ──────────────────────────────────────────────
    private NavMeshAgent  agent;
    private Animator      animator;
    private Transform     player;
    private PlayerHealth  playerHealth;
    private float         attackTimer = 0f;
    private bool          isDead      = false;
    private bool          useNavMesh  = false;

    private static readonly int HashSpeed  = Animator.StringToHash("Speed");
    private static readonly int HashAttack = Animator.StringToHash("Attack");
    private static readonly int HashDie    = Animator.StringToHash("Die");

    enum State { Idle, Chase, Attack }
    private State state = State.Idle;

    void Start()
    {
        agent    = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        // Locate player
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            player       = playerGO.transform;
            playerHealth = playerGO.GetComponent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = playerGO.GetComponentInChildren<PlayerHealth>();
        }

        // Check if a NavMesh is actually available under this zombie
        if (agent != null)
        {
            NavMeshHit hit;
            useNavMesh = NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas);
            if (useNavMesh)
            {
                agent.speed           = chaseSpeed;
                agent.stoppingDistance = attackRange * 0.85f;
                agent.angularSpeed    = 360f;
                Debug.Log("[ZombieAI] NavMesh found – using NavMeshAgent.");
            }
            else
            {
                agent.enabled = false;   // disable to avoid warnings
                Debug.Log("[ZombieAI] No NavMesh – using direct movement fallback.");
            }
        }
    }

    void Update()
    {
        if (isDead || player == null) return;

        float dist  = Vector3.Distance(transform.position, player.position);
        attackTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Idle:
                if (dist <= detectionRange) state = State.Chase;
                break;

            case State.Chase:
                DoChase(dist);
                break;

            case State.Attack:
                DoAttack(dist);
                break;
        }

        // Animate speed
        if (animator != null)
        {
            float spd = (useNavMesh && agent != null && agent.enabled)
                        ? agent.velocity.magnitude
                        : (state == State.Chase ? chaseSpeed : 0f);
            animator.SetFloat(HashSpeed, spd);
        }
    }

    void DoChase(float dist)
    {
        if (dist <= attackRange)
        {
            StopMoving();
            state = State.Attack;
            return;
        }

        if (dist > detectionRange * 1.5f)
        {
            StopMoving();
            state = State.Idle;
            return;
        }

        FaceTarget(player.position);

        if (useNavMesh && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            // Direct movement – walks straight toward player
            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0;
            transform.position += dir * chaseSpeed * Time.deltaTime;
        }
    }

    void DoAttack(float dist)
    {
        StopMoving();
        FaceTarget(player.position);

        if (dist > attackRange * 1.1f)
        {
            state = State.Chase;
            return;
        }

        if (attackTimer <= 0f)
        {
            attackTimer = attackCooldown;

            if (animator != null)
                animator.SetTrigger(HashAttack);

            if (playerHealth != null)
                playerHealth.TakeDamage(attackDamage);

            Debug.Log($"[ZombieAI] Attacked player for {attackDamage} dmg.");
        }
    }

    void StopMoving()
    {
        if (useNavMesh && agent != null && agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0;
        if (dir == Vector3.zero) return;
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look,
                                               rotateSpeed * Time.deltaTime);
    }

    // Called by ZombieHealth on death
    public void OnDead()
    {
        isDead = true;
        StopMoving();
        if (agent != null) agent.enabled = false;
        if (animator != null) animator.SetTrigger(HashDie);
        this.enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
