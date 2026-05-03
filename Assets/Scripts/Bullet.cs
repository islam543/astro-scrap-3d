using UnityEngine;

/// <summary>
/// Player bullet with both manual raycast hit detection and physics callbacks.
/// The raycast prevents tunnelling; collision/trigger callbacks still work for prefabs with colliders.
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 30f;
    public float lifetime = 4f;
    public int damage = 50;
    public bool damageOnHit = true;
    public bool destroyOnHit = true;

    [Header("Impact")]
    public GameObject impactEffectPrefab;
    public float impactEffectLifetime = 0.35f;

    private Rigidbody rb;
    private bool hasHit = false;
    private static Material impactMaterial;

    void Awake()
    {
        EnsurePhysicsForCallbacks();
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        if (hasHit) return;
        if (rb == null) return;

        float step = speed * Time.fixedDeltaTime;
        Vector3 start = rb.position;
        Vector3 end = start + transform.forward * step;

        if (Physics.Raycast(start, transform.forward, out RaycastHit hit, step + 0.08f, ~0, QueryTriggerInteraction.Collide))
        {
            if (!IsPlayerCollider(hit.collider))
            {
                Debug.Log($"[Bullet] Raycast hit: {hit.collider.gameObject.name} | Tag: {hit.collider.tag}");
                HandleHit(hit.collider, hit.point, hit.normal);
                return;
            }
        }

        rb.MovePosition(end);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null) return;

        ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
        Vector3 point = collision.contactCount > 0 ? contact.point : transform.position;
        Vector3 normal = collision.contactCount > 0 ? contact.normal : -transform.forward;
        HandleHit(collision.collider, point, normal);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        HandleHit(other, transform.position, -transform.forward);
    }

    private void HandleHit(Collider hitCollider, Vector3 point, Vector3 normal)
    {
        if (hasHit || hitCollider == null || IsPlayerCollider(hitCollider))
            return;

        hasHit = true;

        if (damageOnHit && damage > 0)
            ApplyDamage(hitCollider);

        SpawnImpactEffect(point, normal);
        Debug.Log($"[Bullet] Impact: {hitCollider.gameObject.name}");

        if (destroyOnHit)
            Destroy(gameObject);
    }

    private void ApplyDamage(Collider hitCollider)
    {
        ZombieHealth zombieHealth = hitCollider.GetComponentInParent<ZombieHealth>();
        if (zombieHealth != null)
        {
            zombieHealth.TakeDamage(damage);
            return;
        }

        CyberMonsterHealth cyberHealth = hitCollider.GetComponentInParent<CyberMonsterHealth>();
        if (cyberHealth != null)
            cyberHealth.TakeDamage(damage);
    }

    private void SpawnImpactEffect(Vector3 point, Vector3 normal)
    {
        GameObject effect = impactEffectPrefab != null
            ? Instantiate(impactEffectPrefab, point, Quaternion.LookRotation(normal))
            : CreateDefaultImpactEffect(point);

        if (effect != null)
            Destroy(effect, impactEffectLifetime);
    }

    private GameObject CreateDefaultImpactEffect(Vector3 point)
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "BulletImpact";
        effect.transform.position = point;
        effect.transform.localScale = Vector3.one * 0.18f;

        Collider effectCollider = effect.GetComponent<Collider>();
        if (effectCollider != null)
            Destroy(effectCollider);

        MeshRenderer renderer = effect.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.material = GetImpactMaterial();

        return effect;
    }

    private static Material GetImpactMaterial()
    {
        if (impactMaterial != null) return impactMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        impactMaterial = new Material(shader);
        impactMaterial.color = new Color(1f, 0.8f, 0.25f);
        if (impactMaterial.HasProperty("_EmissionColor"))
        {
            impactMaterial.EnableKeyword("_EMISSION");
            impactMaterial.SetColor("_EmissionColor", new Color(1f, 0.55f, 0.1f) * 3f);
        }

        return impactMaterial;
    }

    private void EnsurePhysicsForCallbacks()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.08f;
            col = sphere;
        }

        col.isTrigger = true;
    }

    private bool IsPlayerCollider(Collider col)
    {
        return col.CompareTag("Player") || col.GetComponentInParent<PlayerHealth>() != null;
    }
}
