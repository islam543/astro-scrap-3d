using UnityEngine;
using UnityEngine.InputSystem;   // New Input System

public class PlayerShoot : MonoBehaviour
{
    [Header("Shooting Settings")]
    public int damage = 50;
    public float range = 100f;
    public float fireRate = 0.2f;

    private Camera fpsCam;
    private float nextTimeToFire = 0f;

    void Start()
    {
        fpsCam = Camera.main;
        if (fpsCam == null)
            fpsCam = GetComponentInParent<Camera>();
    }

    void Update()
    {
        // Use new Input System mouse button
        bool firePressed = Mouse.current != null && Mouse.current.leftButton.isPressed;

        if (firePressed && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (fpsCam == null) return;

        Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * range, Color.red, 0.5f);

        if (Physics.Raycast(ray, out hit, range))
        {
            Debug.Log($"[PlayerShoot] Hit: {hit.transform.name} on {hit.collider.gameObject.name}");

            // Walk up the hierarchy to find ZombieHealth
            ZombieHealth zombie = hit.collider.GetComponentInParent<ZombieHealth>();

            if (zombie != null)
            {
                Debug.Log("[PlayerShoot] Zombie hit! Dealing damage.");
                zombie.TakeDamage(damage);
            }
            else
            {
                Debug.Log("[PlayerShoot] Hit something, but no ZombieHealth found.");
            }
        }
        else
        {
            Debug.Log("[PlayerShoot] Raycast hit nothing.");
        }
    }
}
