using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShoot : MonoBehaviour
{
    [Header("Shooting")]
    public int   damage   = 50;
    public float fireRate = 0.25f;

    [Header("Bullet")]
    public GameObject bulletPrefab;
    public Transform  muzzlePoint;

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

        // Spawn bullet aimed from screen centre
        GameObject prefab = bulletPrefab != null ? bulletPrefab : GetOrCreateBulletPrefab();
        GameObject b      = Instantiate(prefab, muzzlePoint.position,
                                        Quaternion.LookRotation(dir));
        b.SetActive(true);

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet != null) bullet.damage = damage;
    }

    // ── Runtime bullet template ────────────────────────────────
    static GameObject _bulletTemplate;

    static GameObject GetOrCreateBulletPrefab()
    {
        if (_bulletTemplate != null) return _bulletTemplate;

        // Capsule oriented forward — no Rigidbody, no Collider needed
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "_BulletTemplate";
        go.transform.localScale        = new Vector3(0.06f, 0.15f, 0.06f);
        go.transform.localEulerAngles  = new Vector3(90f, 0f, 0f);

        // Remove the capsule collider — Bullet.cs uses raycasts only
        Object.Destroy(go.GetComponent<CapsuleCollider>());

        // Bright glowing orange material
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat.shader.name == "Hidden/InternalErrorShader")
            mat = new Material(Shader.Find("Standard")); // fallback
        mat.color = new Color(1f, 0.55f, 0f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.35f, 0f) * 4f);
        go.GetComponent<MeshRenderer>().material = mat;

        // Only the Bullet script — no Rigidbody
        go.AddComponent<Bullet>();

        go.SetActive(false);
        _bulletTemplate = go;
        return go;
    }
}
