using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerShoot : MonoBehaviour
{
    [Header("Shooting")]
    public int   damage   = 50;
    public float fireRate = 0.25f;
    public float range    = 100f;

    [Header("Bullet")]
    public GameObject bulletPrefab;
    public Transform  muzzlePoint;

    [Header("Layer Mask")]
    [Tooltip("Objects on this layer are skipped by the damage ray (put your weapon model here).")]
    public LayerMask ignoreLayers = 0;   // assign in Inspector if needed

    private Camera fpsCam;
    private float  nextFire = 0f;

    void Start()
    {
        fpsCam = Camera.main;
        if (fpsCam == null) fpsCam = GetComponentInParent<Camera>();

        if (muzzlePoint == null)
        {
            GameObject mp = new GameObject("MuzzlePoint");
            mp.transform.SetParent(transform, false);
            mp.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            muzzlePoint = mp.transform;
        }
    }

    void Update()
    {
        bool fire = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (fire && Time.time >= nextFire)
        {
            nextFire = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (fpsCam == null) return;

        Ray     aimRay = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 dir    = aimRay.direction;

        ApplyCameraRayDamage(aimRay);

        // Spawn visual bullet from muzzle — damage is handled by camera ray above
        GameObject prefab = bulletPrefab != null ? bulletPrefab : GetOrCreateBulletPrefab();
        GameObject b      = Instantiate(prefab, muzzlePoint.position,
                                        Quaternion.LookRotation(dir));
        b.SetActive(true);

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.damage      = 0;
            bullet.damageOnHit = false;  // visual only; camera ray already handled damage
        }
    }

    void ApplyCameraRayDamage(Ray aimRay)
    {
        // ~ignoreLayers inverts the mask: all layers EXCEPT the ones we want to skip
        int layerMask = ~ignoreLayers.value;
        RaycastHit[] hits = Physics.RaycastAll(aimRay, range, layerMask, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0)
        {
            Debug.Log("[PlayerShoot] Camera ray hit: nothing");
            return;
        }

        // Sort closest first
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            // Skip the player's own colliders (body, capsule, etc.)
            if (IsPlayerCollider(hit.collider)) continue;

            // Skip objects with no health component — keep searching for an enemy behind them.
            // This prevents the gun model (separate object with no health) from blocking shots.
            ZombieHealth zombieHealth = hit.collider.GetComponentInParent<ZombieHealth>();
            if (zombieHealth != null)
            {
                Debug.Log($"[PlayerShoot] Hit zombie: {zombieHealth.gameObject.name} for {damage}");
                zombieHealth.TakeDamage(damage);
                return;
            }

            CyberMonsterHealth cyberHealth = hit.collider.GetComponentInParent<CyberMonsterHealth>();
            if (cyberHealth != null)
            {
                Debug.Log($"[PlayerShoot] Hit cyber monster: {cyberHealth.gameObject.name} for {damage}");
                cyberHealth.TakeDamage(damage);
                return;
            }

            // Hit a solid wall/terrain — stop looking further
            if (!hit.collider.isTrigger)
            {
                Debug.Log($"[PlayerShoot] Ray blocked by: {hit.collider.gameObject.name}");
                return;
            }

            // Trigger collider with no health — keep searching through it
        }

        Debug.Log("[PlayerShoot] Camera ray hit only player colliders or non-solid objects");
    }

    bool IsPlayerCollider(Collider col)
    {
        // Tag check
        if (col.CompareTag("Player")) return true;
        // Hierarchy check — catches weapon models that are children of the player
        if (col.GetComponentInParent<PlayerHealth>() != null) return true;
        return false;
    }

    // ── Runtime bullet template ────────────────────────────────
    static GameObject _bulletTemplate;

    static GameObject GetOrCreateBulletPrefab()
    {
        if (_bulletTemplate != null) return _bulletTemplate;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "_BulletTemplate";
        go.transform.localScale       = new Vector3(0.06f, 0.15f, 0.06f);
        go.transform.localEulerAngles = new Vector3(90f, 0f, 0f);

        UnityEngine.Object.Destroy(go.GetComponent<CapsuleCollider>());

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat.shader.name == "Hidden/InternalErrorShader")
            mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.55f, 0f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.35f, 0f) * 4f);
        go.GetComponent<MeshRenderer>().material = mat;

        go.AddComponent<Bullet>();
        go.SetActive(false);
        _bulletTemplate = go;
        return go;
    }
}
