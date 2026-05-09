using UnityEngine;

/// <summary>
/// Attach to any 3D object in the scene to make it a health pickup.
/// Automatically handles trigger setup — no manual collider configuration needed.
/// The player just needs a "Player" tag and a PlayerHealth component anywhere in its hierarchy.
/// </summary>
public class HealthPickup : MonoBehaviour
{
    [Header("Heal Amount")]
    [Tooltip("How many HP this pickup restores")]
    public int healAmount = 25;

    [Header("Visuals & Feedback")]
    [Tooltip("Rotation speed in degrees per second")]
    public float spinSpeed = 90f;

    [Tooltip("Bob up/down amplitude in units")]
    public float bobAmplitude = 0.15f;

    [Tooltip("Bob speed")]
    public float bobSpeed = 2f;

    [Tooltip("Optional particle effect to spawn on pickup")]
    public GameObject pickupEffect;

    [Header("Audio")]
    [Tooltip("Sound played when picked up")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float pickupVolume = 0.9f;

    [Header("Respawn (optional)")]
    [Tooltip("If > 0 the pickup will respawn after this many seconds")]
    public float respawnTime = 30f;

    private Vector3 startPosition;
    private Renderer[] visuals;
    private Collider triggerCol;
    private bool isPickedUp = false;

    void Awake()
    {
        startPosition = transform.position;
        visuals       = GetComponentsInChildren<Renderer>(true);

        // Find or create a proper trigger collider.
        // MeshColliders can't be triggers unless convex, so we prefer any other collider.
        triggerCol = FindNonMeshCollider();

        if (triggerCol == null)
        {
            // No suitable collider found — add a SphereCollider so pickup works out of the box
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.radius    = 0.8f;
            sc.isTrigger = true;
            triggerCol   = sc;
            Debug.Log("[HealthPickup] No trigger collider found — added SphereCollider automatically.");
        }
        else
        {
            triggerCol.isTrigger = true;
        }
    }

    // Returns the first non-MeshCollider on this object, or null.
    Collider FindNonMeshCollider()
    {
        foreach (Collider c in GetComponents<Collider>())
            if (!(c is MeshCollider)) return c;
        return null;
    }

    void Update()
    {
        if (isPickedUp) return;

        // Spin
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        // Bob
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isPickedUp) return;

        // Accept any collider that belongs to the player (tag on the GO or any parent)
        if (!IsPlayer(other)) return;

        // Search the full hierarchy for PlayerHealth
        PlayerHealth ph = FindPlayerHealth(other.gameObject);
        if (ph == null)
        {
            Debug.LogWarning("[HealthPickup] Player entered trigger but no PlayerHealth found in hierarchy.");
            return;
        }

        if (ph.GetHealth() >= ph.GetMaxHealth())
        {
            Debug.Log("[HealthPickup] Player is already at full health — not consumed.");
            return;
        }

        ph.Heal(healAmount);
        Debug.Log($"[HealthPickup] Healed player for {healAmount} HP. New HP: {ph.GetHealth()}/{ph.GetMaxHealth()}");

        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);

        Pickup();
    }

    // Check if collider or any of its parents is tagged "Player"
    bool IsPlayer(Collider other)
    {
        Transform t = other.transform;
        while (t != null)
        {
            if (t.CompareTag("Player")) return true;
            t = t.parent;
        }
        return false;
    }

    // Search the object and its full parent chain for PlayerHealth
    PlayerHealth FindPlayerHealth(GameObject go)
    {
        // Search downward first (children)
        PlayerHealth ph = go.GetComponentInChildren<PlayerHealth>(true);
        if (ph != null) return ph;

        // Search upward through parents
        Transform t = go.transform.parent;
        while (t != null)
        {
            ph = t.GetComponent<PlayerHealth>();
            if (ph != null) return ph;
            ph = t.GetComponentInChildren<PlayerHealth>(true);
            if (ph != null) return ph;
            t = t.parent;
        }
        return null;
    }

    void Pickup()
    {
        isPickedUp = true;

        foreach (Renderer r in visuals) r.enabled = false;
        if (triggerCol != null) triggerCol.enabled = false;

        if (respawnTime > 0f)
            Invoke(nameof(Respawn), respawnTime);
        else
            Destroy(gameObject, 0.5f);
    }

    void Respawn()
    {
        isPickedUp = false;
        transform.position = startPosition;
        foreach (Renderer r in visuals) r.enabled = true;
        if (triggerCol != null) triggerCol.enabled = true;
        Debug.Log("[HealthPickup] Respawned.");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.5f);
        Gizmos.DrawSphere(transform.position, 0.8f);
    }
}
