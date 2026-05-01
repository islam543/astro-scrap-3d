using UnityEngine;

/// <summary>
/// Bullet fired BY the Cyber Monster AT the player.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MonsterBullet : MonoBehaviour
{
    public float speed    = 18f;
    public float lifetime = 5f;

    private int       damage;
    private Rigidbody rb;
    private bool      initialised = false;

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
        initialised = true;
        rb.linearVelocity = direction * speed;
    }

    void OnCollisionEnter(Collision col)
    {
        PlayerHealth ph = col.collider.GetComponentInParent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(damage);
        Destroy(gameObject);
    }
}
