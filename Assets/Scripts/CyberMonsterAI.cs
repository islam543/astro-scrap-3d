using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI brain for Cyber Monster 2.
/// States: Idle → Chase → Attack (sword or gun) → Dead
/// Animations are driven by CrossFade since the existing controller has no parameters.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class CyberMonsterAI : MonoBehaviour
{
    // ── Tuning ─────────────────────────────────────────────────
    [Header("Detection")]
    public float detectionRange = 50f;
    public float meleeRange     = 2.2f;
    public float gunRange       = 14f;          // beyond melee but in detection

    [Header("Combat")]
    public int   meleeDamage    = 20;
    public int   gunDamage      = 15;
    public float meleeCooldown  = 1.4f;
    public float gunCooldown    = 2.5f;

    [Header("Movement")]
    public float runSpeed       = 4.5f;
    public float walkSpeed      = 2f;

    [Header("Bullet (auto-created if null)")]
    public GameObject bulletPrefab;             // assigned at runtime if null
    public Transform  muzzlePoint;              // world-space spawn point for bullets

    // ── Internal ───────────────────────────────────────────────
    private NavMeshAgent agent;
    private Animator     anim;
    private Transform    player;
    private PlayerHealth playerHealth;

    private float  meleeTimer = 0f;
    private float  gunTimer   = 0f;
    private bool   isDead     = false;
    private bool   useNavMesh = false;

    enum State { Idle, Chase, MeleeAttack, GunAttack }
    private State state = State.Idle;

    private const float MinimumDetectionRange = 50f;

    // animation state names (must match controller exactly)
    const string ANIM_IDLE   = "Idle";
    const string ANIM_WALK   = "Walking";
    const string ANIM_RUN    = "Run";
    const string ANIM_SWORD  = "sword attack";
    const string ANIM_GUN    = "shoots gun_2";
    const string ANIM_DEATH  = "Death";

    // ── Lifecycle ──────────────────────────────────────────────
    void Awake()
    {
        detectionRange = Mathf.Max(detectionRange, MinimumDetectionRange);
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim  = GetComponentInChildren<Animator>();

        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            player       = playerGO.transform;
            playerHealth = playerGO.GetComponent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = playerGO.GetComponentInChildren<PlayerHealth>();
        }

        // Check NavMesh availability
        NavMeshHit hit;
        useNavMesh = NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas);
        if (useNavMesh)
        {
            agent.speed           = runSpeed;
            agent.stoppingDistance = meleeRange * 0.85f;
            agent.angularSpeed    = 360f;
        }
        else
        {
            agent.enabled = false;
        }

        // Build a muzzle point on the monster if none assigned
        if (muzzlePoint == null)
        {
            GameObject mp = new GameObject("MuzzlePoint_CM");
            mp.transform.SetParent(transform, false);
            mp.transform.localPosition = new Vector3(0f, 1.4f, 0.8f);
            muzzlePoint = mp.transform;
        }

        // Reset stale triggers so death anim never fires on spawn
        if (anim != null)
            foreach (AnimatorControllerParameter p in anim.parameters)
                if (p.type == AnimatorControllerParameterType.Trigger)
                    anim.ResetTrigger(p.name);

        PlayAnim(ANIM_IDLE);
    }

    void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        meleeTimer -= Time.deltaTime;
        gunTimer   -= Time.deltaTime;

        switch (state)
        {
            case State.Idle:
                if (dist <= detectionRange) EnterChase();
                break;

            case State.Chase:
                DoChase(dist);
                break;

            case State.MeleeAttack:
                DoMelee(dist);
                break;

            case State.GunAttack:
                DoGun(dist);
                break;
        }
        if (anim != null){
            anim.SetFloat("Speed", agent != null && agent.enabled ? agent.velocity.magnitude : (state == State.Chase ? runSpeed : 0f));
        }
    }

    // ── State methods ──────────────────────────────────────────
    void EnterChase()
    {
        state = State.Chase;
        SetAgentSpeed(runSpeed);
        PlayAnim(ANIM_RUN);
    }

    void DoChase(float dist)
    {
        MoveToward(player.position);
        FaceTarget(player.position);

        if (dist <= meleeRange)
        {
            StopAgent();
            state = State.MeleeAttack;
        }
        else if (dist <= gunRange && gunTimer <= 0f)
        {
            StopAgent();
            state = State.GunAttack;
        }
        else if (dist > detectionRange * 1.4f)
        {
            StopAgent();
            state = State.Idle;
            PlayAnim(ANIM_IDLE);
        }
    }

    void DoMelee(float dist)
    {
        StopAgent();
        FaceTarget(player.position);

        // Re-chase if player moved away
        if (dist > meleeRange * 1.3f)
        {
            state = State.Chase;
            SetAgentSpeed(runSpeed);
            PlayAnim(ANIM_RUN);
            return;
        }

        if (meleeTimer <= 0f)
        {
            meleeTimer = meleeCooldown;
            PlayAnim(ANIM_SWORD);
            playerHealth?.TakeDamage(meleeDamage);
            Debug.Log($"[CyberMonsterAI] Sword hit player for {meleeDamage}");
        }
    }

    void DoGun(float dist)
    {
        StopAgent();
        FaceTarget(player.position);

        if (dist <= meleeRange)
        {
            state = State.MeleeAttack;
            return;
        }
        if (dist > gunRange)
        {
            EnterChase();
            return;
        }

        if (gunTimer <= 0f)
        {
            gunTimer = gunCooldown;
            PlayAnim(ANIM_GUN);
            FireBulletAtPlayer();
        }
        else
        {
            // Walk slowly while waiting for gun cooldown
            PlayAnim(ANIM_WALK);
        }
    }

    // ── Actions ────────────────────────────────────────────────
    void FireBulletAtPlayer()
    {
        GameObject prefab = bulletPrefab != null ? bulletPrefab : GetOrCreateMonsterBulletPrefab();
        if (prefab == null) return;

        Vector3 dir = (player.position + Vector3.up * 1f - muzzlePoint.position).normalized;
        GameObject b = Instantiate(prefab, muzzlePoint.position,
                                   Quaternion.LookRotation(dir));
        b.SetActive(true);

        MonsterBullet mb = b.GetComponent<MonsterBullet>();
        if (mb != null) mb.Init(dir, gunDamage);

        Debug.Log("[CyberMonsterAI] Fired bullet at player.");
    }

    // ── Helpers ────────────────────────────────────────────────
    void MoveToward(Vector3 target)
    {
        if (useNavMesh && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(target);
        }
        else
        {
            Vector3 dir = (target - transform.position).normalized;
            dir.y = 0;
            transform.position += dir * runSpeed * Time.deltaTime;
        }
    }

    void StopAgent()
    {
        if (useNavMesh && agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();
    }

    void SetAgentSpeed(float s)
    {
        if (useNavMesh && agent.enabled)
            agent.speed = s;
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0;
        if (dir == Vector3.zero) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
                             Quaternion.LookRotation(dir), 8f * Time.deltaTime);
    }

    void PlayAnim(string stateName)
    {
        if (anim == null) return;
        // Always reset Die trigger before playing any non-death state
        // so a queued Die never fires unexpectedly mid-combat
        if (stateName != ANIM_DEATH)
            anim.ResetTrigger("Die");
        anim.CrossFade(stateName, 0.15f);
    }

    // ── Called by CyberMonsterHealth ──────────────────────────
    public void OnDead()
    {
        isDead = true;
        StopAgent();
        if (agent != null) agent.enabled = false;
        if (anim != null)
            anim.SetTrigger("Die");
        this.enabled = false;
    }

    static GameObject _monsterBulletTemplate;

    static GameObject GetOrCreateMonsterBulletPrefab()
    {
        if (_monsterBulletTemplate != null) return _monsterBulletTemplate;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "_MonsterBulletTemplate";
        go.transform.localScale = Vector3.one * 0.18f;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        Material mat = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0.25f, 0.9f, 1f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.1f, 0.8f, 1f) * 3f);
        go.GetComponent<MeshRenderer>().material = mat;

        go.SetActive(false);
        go.AddComponent<MonsterBullet>();
        _monsterBulletTemplate = go;
        return go;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, gunRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
