using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

public class PlayerShoot : MonoBehaviour
{
    [Header("Shooting")]
    public int damage = 50;
    public float fireRate = 0.25f;
    public float range = 100f;

    [Header("Bullet")]
    public GameObject bulletPrefab;
    [Tooltip("Dedicated non-visual barrel tip used as the bullet spawn point.")]
    public Transform shootPoint;
    [Tooltip("Legacy barrel reference kept for existing Inspector assignments. Do not assign the muzzle flash visual here.")]
    public Transform muzzlePoint;

    [Header("Effects")]
    public ParticleSystem muzzleFlash;
    public Light muzzleLight;
    public AudioClip shootSound;
    public AudioSource shootAudio;
    public CameraShake cameraShake;
    public float shakeDuration = 0.08f;
    public float shakeMagnitude = 0.06f;

    [Header("Layer Mask")]
    [Tooltip("Objects on this layer are skipped by the damage ray. Put your weapon/player model here if needed.")]
    public LayerMask ignoreLayers = 0;
    [HideInInspector] public bool fullAuto = false;

    private Camera fpsCam;
    private float nextFire = 0f;

    void Start()
    {
        fpsCam = Camera.main;

        if (fpsCam == null)
            fpsCam = GetComponentInParent<Camera>();

        if (cameraShake == null && fpsCam != null)
            cameraShake = fpsCam.GetComponent<CameraShake>();

        if (muzzleFlash == null)
            muzzleFlash = GetComponentInChildren<ParticleSystem>(true);

        EnsureShootPoint();

        if (muzzleLight != null)
            muzzleLight.enabled = false;
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPlayerAct)
            return;

        bool fire = Mouse.current != null && (
            fullAuto
                ? Mouse.current.leftButton.isPressed
                : Mouse.current.leftButton.wasPressedThisFrame
        );

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

        if (fpsCam == null)
            return;

        PlayShootEffects();

        Ray aimRay = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = GetAimPoint(aimRay);

        Transform origin = GetShootOrigin();
        Vector3 shootPosition = origin != null ? origin.position : transform.position;
        Vector3 dir = (targetPoint - shootPosition).normalized;

        if (dir.sqrMagnitude < 0.001f)
            dir = aimRay.direction;

        GameObject prefab = bulletPrefab != null ? bulletPrefab : GetOrCreateBulletPrefab();
        GameObject b = Instantiate(prefab, shootPosition, Quaternion.LookRotation(dir));
        b.SetActive(true);

        Bullet bullet = b.GetComponent<Bullet>();

        if (bullet == null)
            bullet = b.AddComponent<Bullet>();

        bullet.damage = damage;
        bullet.damageOnHit = true;
    }

    void PlayShootEffects()
    {
        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (muzzleLight != null)
            StartCoroutine(MuzzleLightFlash());

        if (shootSound != null)
        {
            if (shootAudio == null)
            {
                shootAudio = GetComponent<AudioSource>();
                if (shootAudio == null)
                    shootAudio = gameObject.AddComponent<AudioSource>();
            }

            shootAudio.PlayOneShot(shootSound);
        }

        if (cameraShake != null)
            cameraShake.Shake(shakeDuration, shakeMagnitude);
    }

    IEnumerator MuzzleLightFlash()
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(0.04f);
        muzzleLight.enabled = false;
    }

    Vector3 GetAimPoint(Ray aimRay)
    {
        int layerMask = ~ignoreLayers.value;

        RaycastHit[] hits = Physics.RaycastAll(
            aimRay,
            range,
            layerMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits.Length == 0)
            return aimRay.origin + aimRay.direction * range;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (IsPlayerCollider(hit.collider))
                continue;

            return hit.point;
        }

        return aimRay.origin + aimRay.direction * range;
    }

    bool IsPlayerCollider(Collider col)
    {
        if (col.CompareTag("Player"))
            return true;

        if (col.GetComponentInParent<PlayerHealth>() != null)
            return true;

        return false;
    }

    void EnsureShootPoint()
    {
        if (shootPoint != null && !IsMuzzleFlashTransform(shootPoint))
            return;

        Transform existingShootPoint = transform.Find("ShootPoint");
        if (existingShootPoint != null && !IsMuzzleFlashTransform(existingShootPoint))
        {
            shootPoint = existingShootPoint;
            return;
        }

        GameObject sp = new GameObject("ShootPoint");
        sp.transform.SetParent(transform, false);

        if (muzzlePoint != null)
        {
            sp.transform.position = muzzlePoint.position;
            sp.transform.rotation = muzzlePoint.rotation;
        }
        else
        {
            sp.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            sp.transform.localRotation = Quaternion.identity;
        }

        shootPoint = sp.transform;
    }

    Transform GetShootOrigin()
    {
        if (shootPoint == null || IsMuzzleFlashTransform(shootPoint))
            EnsureShootPoint();

        if (shootPoint != null && !IsMuzzleFlashTransform(shootPoint))
            return shootPoint;

        if (muzzlePoint != null && !IsMuzzleFlashTransform(muzzlePoint))
            return muzzlePoint;

        return transform;
    }

    bool IsMuzzleFlashTransform(Transform candidate)
    {
        if (candidate == null)
            return false;

        if (muzzleFlash != null && candidate == muzzleFlash.transform)
            return true;

        return candidate.GetComponent<ParticleSystem>() != null;
    }

    static GameObject _bulletTemplate;

    static GameObject GetOrCreateBulletPrefab()
    {
        if (_bulletTemplate != null)
            return _bulletTemplate;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "_BulletTemplate";
        go.transform.localScale = new Vector3(0.06f, 0.15f, 0.06f);
        go.transform.localEulerAngles = new Vector3(90f, 0f, 0f);

        CapsuleCollider capsuleCollider = go.GetComponent<CapsuleCollider>();

        if (capsuleCollider != null)
            capsuleCollider.isTrigger = true;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = shader != null
            ? new Material(shader)
            : new Material(Shader.Find("Sprites/Default"));

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
