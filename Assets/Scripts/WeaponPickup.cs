using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Place this on a weapon GameObject sitting in the world.
/// Shows a prompt when the player is close, picked up on E press.
/// </summary>
public class WeaponPickup : MonoBehaviour
{
    [Header("Settings")]
    public string weaponName    = "Rifle";
    public float  pickupRadius  = 2.5f;

    [Header("Floating animation")]
    public float bobHeight   = 0.15f;
    public float bobSpeed    = 1.8f;
    public float rotateSpeed = 60f;

    private Vector3 startPos;
    private bool    playerNear = false;
    private bool    pickedUp   = false;

    void Start()
    {
        startPos = transform.position;

        if (GetComponent<Collider>() == null)
        {
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.radius    = 0.4f;
            sc.isTrigger = false;
        }
    }

    // Called by WeaponHolder.Drop() via SendMessage
    void ResetPosition()
    {
        pickedUp = false;
        startPos = transform.position;
    }

    void Update()
    {
        if (pickedUp) return;
        if (GameManager.Instance != null && !GameManager.Instance.CanPlayerAct) return;

        // Hover + spin
        float y = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, y, startPos.z);
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // Check distance to player
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        playerNear = dist <= pickupRadius;

        // E to pick up
        if (playerNear && Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            WeaponHolder holder = player.GetComponent<WeaponHolder>();
            if (holder != null) holder.PickUp(this);
        }
    }

    public void OnPickedUp()
    {
        pickedUp   = true;
        playerNear = false;
    }

    void OnGUI()
    {
        if (!playerNear || pickedUp) return;
        if (GameManager.Instance != null && !GameManager.Instance.CanPlayerAct) return;
        if (Camera.main == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(
            transform.position + Vector3.up * 0.6f);
        if (screenPos.z > 0)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize  = 16;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;

            GUI.Label(
                new Rect(screenPos.x - 100, Screen.height - screenPos.y - 12, 200, 28),
                $"[E]  Pick up {weaponName}",
                style
            );
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
