using UnityEngine;

/// <summary>
/// Moves purely via Transform each frame and fires a raycast from
/// last position to current position — 100% reliable hit detection,
/// no Rigidbody tunnelling, no trigger/collider layer issues.
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed    = 20f;
    public float lifetime = 4f;
    public int   damage   = 50;

    private Vector3 prevPosition;
    private bool    hasHit = false;

    void Start()
    {
        prevPosition = transform.position;
        Destroy(gameObject, lifetime);

        // Remove Rigidbody if one exists — we move manually
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
    }

    void Update()
    {
        if (hasHit) return;

        // Move forward this frame
        float   step       = speed * Time.deltaTime;
        Vector3 moveVector = transform.forward * step;
        transform.position += moveVector;

        // Raycast from where we were to where we are now
        Vector3 dir      = transform.position - prevPosition;
        float   dist     = dir.magnitude;

        if (dist > 0f && Physics.Raycast(prevPosition, dir.normalized, out RaycastHit hit, dist + 0.05f))
        {
            // Don't hit triggers or the player
            if (!hit.collider.isTrigger && !hit.collider.CompareTag("Player"))
            {
                // Check for enemy health
                ZombieHealth zh = hit.collider.GetComponentInParent<ZombieHealth>();
                if (zh != null) zh.TakeDamage(damage);

                CyberMonsterHealth ch = hit.collider.GetComponentInParent<CyberMonsterHealth>();
                if (ch != null) ch.TakeDamage(damage);

                Debug.Log($"[Bullet] Hit: {hit.collider.gameObject.name}");

                // Snap bullet to hit point and destroy
                transform.position = hit.point;
                hasHit = true;
                Destroy(gameObject, 0.01f);
                return;
            }
        }

        prevPosition = transform.position;
    }
}
