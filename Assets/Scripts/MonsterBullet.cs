using UnityEngine;

/// <summary>
/// Bullet fired BY the Cyber Monster AT the player.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MonsterBullet : MonoBehaviour
{
    public float speed    = 18f;
    public float lifetime = 5f;
    public float impactEffectLifetime = 0.3f;

    private int       damage;
    private Rigidbody rb;
    private bool      hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Destroy(gameObject, lifetime);
    }

    public void Init(Vector3 direction, int dmg)
    {
        damage      = dmg;
        rb.linearVelocity = direction * speed;
    }

    void OnCollisionEnter(Collision col)
    {
        if (col == null || col.collider == null) return;
        ContactPoint contact = col.contactCount > 0 ? col.GetContact(0) : default;
        Vector3 point = col.contactCount > 0 ? contact.point : transform.position;
        HandleHit(col.collider, point);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        HandleHit(other, transform.position);
    }

    private void HandleHit(Collider hitCollider, Vector3 point)
    {
        if (hasHit) return;
        hasHit = true;

        PlayerHealth ph = hitCollider.GetComponentInParent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(damage);

        SpawnImpactEffect(point);
        Destroy(gameObject);
    }

    private void SpawnImpactEffect(Vector3 point)
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "MonsterBulletImpact";
        effect.transform.position = point;
        effect.transform.localScale = Vector3.one * 0.22f;

        Collider effectCollider = effect.GetComponent<Collider>();
        if (effectCollider != null)
            Destroy(effectCollider);

        MeshRenderer renderer = effect.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = new Color(0.25f, 0.9f, 1f);
                renderer.material = material;
            }
        }

        Destroy(effect, impactEffectLifetime);
    }
}
