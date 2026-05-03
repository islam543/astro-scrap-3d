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
        if (GameManager.Instance != null && !GameManager.Instance.CanPlayerAct)
            return;

        bool fire = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (fire && Time.time >= nextFire)
        {
            nextFire = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (fpsCam == null)
            fpsCam = Camera.main;
        if (fpsCam == null) return;

        Ray aimRay = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = GetAimPoint(aimRay);
        Vector3 muzzlePosition = muzzlePoint != null ? muzzlePoint.position : transform.position;
        Vector3 dir = (targetPoint - muzzlePosition).normalized;
        if (dir.sqrMagnitude < 0.001f)
            dir = aimRay.direction;

        GameObject prefab = bulletPrefab != null ? bulletPrefab : GetOrCreateBulletPrefab();
        GameObject b = Instantiate(prefab, muzzlePosition, Quaternion.LookRotation(dir));
        b.SetActive(true);

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet == null)
            bullet = b.AddComponent<Bullet>();

        if (bullet != null)
        {
            bullet.damage = damage;
            bullet.damageOnHit = true;
        }
    }

    Vector3 GetAimPoint(Ray aimRay)
    {
        int layerMask = ~ignoreLayers.value;
        RaycastHit[] hits = Physics.RaycastAll(aimRay, range, layerMask, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0)
            return aimRay.origin + aimRay.direction * range;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (IsPlayerCollider(hit.collider)) continue;
            return hit.point;
        }

        return aimRay.origin + aimRay.direction * range;
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

        CapsuleCollider capsuleCollider = go.GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
            capsuleCollider.isTrigger = true;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        Material mat = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
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
