using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to PlayerCapsule.
/// On pickup  → hides the default weapon visual, parents the pickup to the Main Camera
///              at exactly the same local transform the default weapon uses.
/// On drop    → restores the default weapon visual and tosses the pickup forward.
/// PlayerShoot stays on the Weapon GO (child of Main Camera) and is never moved.
/// </summary>
public class WeaponHolder : MonoBehaviour
{
    [Header("Hold transform – must match the default 'Weapon' child of Main Camera")]
    public Vector3 holdPosition = new Vector3(0.087f, -0.061f, 0.341f);
    public Vector3 holdRotation = new Vector3(0f, 353.71f, 0f);
    public Vector3 holdScale    = new Vector3(0.1f,  0.1f,   0.1f);

    [Header("Drop physics")]
    public float dropForce    = 4f;
    public float dropUpForce  = 1.5f;

    // ── internal refs ─────────────────────────────────────────
    private GameObject  equippedWeapon;
    private WeaponPickup equippedPickup;
    private Transform   cameraTransform;

    /// <summary>
    /// The visible weapon model that is a child of the 'Weapon' GO (e.g. "Your Weapon Example 1").
    /// We hide this while a pickup is held so two gun models don't overlap.
    /// </summary>
    private GameObject defaultWeaponVisual;

    // ── lifecycle ─────────────────────────────────────────────
    void Start()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cameraTransform = cam.transform;
        }
        else
        {
            Debug.LogWarning("[WeaponHolder] Main Camera not found. Weapon pickup will not work.");
            return;
        }

        FindDefaultWeaponVisual();
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPlayerAct)
            return;

        // G = drop current weapon
        if (equippedWeapon != null &&
            Keyboard.current != null &&
            Keyboard.current.gKey.wasPressedThisFrame)
        {
            Drop();
        }
    }

    // ── public API called by WeaponPickup ─────────────────────
    public void PickUp(WeaponPickup pickup)
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPlayerAct)
            return;

        if (cameraTransform == null)
        {
            Debug.LogWarning("[WeaponHolder] Cannot pick up – Main Camera reference is missing.");
            return;
        }

        // Drop whatever we're holding first
        if (equippedWeapon != null) Drop();

        pickup.OnPickedUp();
        equippedPickup = pickup;
        equippedWeapon = pickup.gameObject;

        // ── 1. Hide the default weapon visual so the two models don't overlap ──
        if (defaultWeaponVisual != null)
            defaultWeaponVisual.SetActive(false);
        else
            Debug.LogWarning("[WeaponHolder] defaultWeaponVisual is null – default weapon may still be visible.");

        // ── 2. Parent to Main Camera at exactly the same spot as the default weapon ──
        equippedWeapon.transform.SetParent(cameraTransform, false);
        equippedWeapon.transform.localPosition    = holdPosition;
        equippedWeapon.transform.localEulerAngles = holdRotation;
        equippedWeapon.transform.localScale       = holdScale;

        // ── 3. Kill physics so the gun doesn't fall or drift ──
        Rigidbody rb = equippedWeapon.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // ── 4. Disable colliders so the gun can't push enemies while held ──
        foreach (Collider c in equippedWeapon.GetComponentsInChildren<Collider>(true))
            c.enabled = false;

        Debug.Log($"[WeaponHolder] Picked up: {pickup.weaponName}");
    }

    // ── private helpers ───────────────────────────────────────
    void Drop()
    {
        if (equippedWeapon == null) return;

        // Restore the default weapon visual
        if (defaultWeaponVisual != null)
            defaultWeaponVisual.SetActive(true);

        // Unparent from camera (restore scale to original before unparenting)
        equippedWeapon.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        equippedWeapon.transform.SetParent(null);

        // Re-enable colliders
        foreach (Collider c in equippedWeapon.GetComponentsInChildren<Collider>(true))
            c.enabled = true;

        // Restore / add Rigidbody and throw it
        Rigidbody rb = equippedWeapon.GetComponent<Rigidbody>();
        if (rb == null) rb = equippedWeapon.AddComponent<Rigidbody>();
        rb.isKinematic = false;

        Vector3 throwDir = cameraTransform != null
                           ? cameraTransform.forward * dropForce + Vector3.up * dropUpForce
                           : Vector3.forward * dropForce;
        rb.AddForce(throwDir, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);

        // Let WeaponPickup re-enable hover so it can be picked up again
        if (equippedPickup != null)
        {
            equippedPickup.SendMessage("ResetPosition", SendMessageOptions.DontRequireReceiver);
            equippedPickup.enabled = true;
        }

        Debug.Log($"[WeaponHolder] Dropped: {equippedWeapon.name}");
        equippedWeapon = null;
        equippedPickup = null;
    }

    /// <summary>
    /// Auto-detects the weapon visual (first child with a MeshRenderer under the 'Weapon' GO).
    /// Call from Start(). You can also assign defaultWeaponVisual manually in the Inspector if needed.
    /// </summary>
    void FindDefaultWeaponVisual()
    {
        // The default weapon is a child of Main Camera named "Weapon"
        Transform weaponRoot = cameraTransform.Find("Weapon");
        if (weaponRoot == null)
        {
            Debug.LogWarning("[WeaponHolder] Could not find 'Weapon' child under Main Camera. " +
                             "Assign the weapon visual manually if the name differs.");
            return;
        }

        // The weapon visual is the first child that contains a MeshRenderer
        foreach (Transform child in weaponRoot)
        {
            if (child.GetComponentInChildren<MeshRenderer>(true) != null)
            {
                defaultWeaponVisual = child.gameObject;
                Debug.Log($"[WeaponHolder] Default weapon visual found: '{child.name}'");
                return;
            }
        }

        Debug.LogWarning("[WeaponHolder] 'Weapon' GO has no child with a MeshRenderer. " +
                         "The default gun visual won't be hidden on pickup.");
    }
}
