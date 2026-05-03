using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to PlayerCapsule.
/// Manages equipping / dropping weapons.
/// The equipped weapon is parented to MainCamera so it follows the view.
/// </summary>
public class WeaponHolder : MonoBehaviour
{
    [Header("Hold position (local to camera)")]
    public Vector3 holdPosition = new Vector3(0.25f, -0.22f, 0.45f);
    public Vector3 holdRotation = new Vector3(0f, 0f, 0f);

    [Header("Drop")]
    public float dropForce    = 4f;
    public float dropUpForce  = 1.5f;

    // ── internal ──────────────────────────────────────────────
    private GameObject equippedWeapon;
    private WeaponPickup equippedPickup;
    private Transform  cameraTransform;
    private PlayerShoot shootScript;

    void Start()
    {
        // Find the main camera under this player
        Camera cam = Camera.main;
        if (cam != null) cameraTransform = cam.transform;

        shootScript = GetComponentInChildren<PlayerShoot>(true);
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPlayerAct)
            return;

        // G to drop current weapon
        if (equippedWeapon != null &&
            Keyboard.current != null &&
            Keyboard.current.gKey.wasPressedThisFrame)
        {
            Drop();
        }
    }

    public void PickUp(WeaponPickup pickup)
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPlayerAct)
            return;

        // Drop whatever we're holding first
        if (equippedWeapon != null) Drop();

        pickup.OnPickedUp();
        equippedPickup  = pickup;
        equippedWeapon  = pickup.gameObject;

        // Parent to camera and set hold position
        equippedWeapon.transform.SetParent(cameraTransform, false);
        equippedWeapon.transform.localPosition = holdPosition;
        equippedWeapon.transform.localEulerAngles = holdRotation;

        // Remove physics while held
        Rigidbody rb = equippedWeapon.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Disable any colliders so it doesn't push enemies
        foreach (Collider c in equippedWeapon.GetComponentsInChildren<Collider>())
            c.enabled = false;

        // Move PlayerShoot's muzzle point to this weapon
        if (shootScript != null)
        {
            shootScript.transform.SetParent(equippedWeapon.transform, false);
            shootScript.transform.localPosition = Vector3.zero;

            // Update muzzle point to weapon tip
            if (shootScript.muzzlePoint != null)
                shootScript.muzzlePoint.localPosition = new Vector3(0f, 0f, 0.4f);
        }

        Debug.Log($"[WeaponHolder] Picked up: {pickup.weaponName}");
    }

    void Drop()
    {
        if (equippedWeapon == null) return;

        // Unparent
        equippedWeapon.transform.SetParent(null);

        // Re-enable colliders
        foreach (Collider c in equippedWeapon.GetComponentsInChildren<Collider>())
            c.enabled = true;

        // Add Rigidbody and toss forward
        Rigidbody rb = equippedWeapon.GetComponent<Rigidbody>();
        if (rb == null) rb = equippedWeapon.AddComponent<Rigidbody>();
        rb.isKinematic = false;

        Vector3 throwDir = cameraTransform != null
                           ? cameraTransform.forward * dropForce + Vector3.up * dropUpForce
                           : Vector3.forward * dropForce;
        rb.AddForce(throwDir, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);

        // Re-enable the pickup script so it can be picked up again
        if (equippedPickup != null)
        {
            // Reset its start position so hover works from new location
            equippedPickup.SendMessage("ResetPosition", SendMessageOptions.DontRequireReceiver);
            equippedPickup.enabled = true;
        }

        Debug.Log($"[WeaponHolder] Dropped: {equippedWeapon.name}");

        equippedWeapon = null;
        equippedPickup = null;
    }
}
