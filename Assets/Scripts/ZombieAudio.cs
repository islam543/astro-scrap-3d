using UnityEngine;

/// <summary>
/// Add this component to your Zombie prefab alongside ZombieAI.
/// Plays sounds for each zombie state: idle growl, chase roar, attack, death.
///
/// HOW TO USE:
///   1. Add this script to your Zombie prefab.
///   2. Assign audio clips in the Inspector (drag .mp3/.wav files from your Project window).
///   3. ZombieAI will automatically call the audio events.
///
/// REQUIRED: Add an AudioSource component to the Zombie (or this script adds one automatically).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ZombieAudio : MonoBehaviour
{
    [Header("Zombie Sound Clips")]
    [Tooltip("Looping idle growl — plays when zombie is wandering / not alert")]
    public AudioClip idleGrowlClip;

    [Tooltip("Played once when zombie first spots the player")]
    public AudioClip alertRoarClip;

    [Tooltip("Looping chase growl — plays while chasing player")]
    public AudioClip chaseGrowlClip;

    [Tooltip("Played each time the zombie attacks")]
    public AudioClip attackClip;

    [Tooltip("Played when the zombie dies")]
    public AudioClip deathClip;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float idleVolume   = 0.4f;
    [Range(0f, 1f)] public float alertVolume  = 0.9f;
    [Range(0f, 1f)] public float chaseVolume  = 0.6f;
    [Range(0f, 1f)] public float attackVolume = 1.0f;
    [Range(0f, 1f)] public float deathVolume  = 1.0f;

    [Header("Idle Growl Timing")]
    [Tooltip("Min seconds between idle growls")]
    public float idleGrowlMinInterval = 4f;
    [Tooltip("Max seconds between idle growls")]
    public float idleGrowlMaxInterval = 9f;

    private AudioSource audioSource;
    private float idleGrowlTimer;
    private ZombieAIState lastState = ZombieAIState.Idle;
    private bool isDead = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;   // 3D sound
        audioSource.rolloffMode  = AudioRolloffMode.Linear;
        audioSource.maxDistance  = 30f;
        audioSource.loop         = false;
        ResetIdleTimer();
    }

    // ── Called by ZombieAI on state changes ──────────────────

    public void OnStateChanged(ZombieAIState newState)
    {
        if (isDead) return;
        if (newState == lastState) return;

        ZombieAIState prev = lastState;
        lastState = newState;

        switch (newState)
        {
            case ZombieAIState.Idle:
                // Will growl on next timer tick
                ResetIdleTimer();
                break;

            case ZombieAIState.Chase:
                if (prev == ZombieAIState.Idle)
                    PlayOneShot(alertRoarClip, alertVolume);
                StartChaseLoop();
                break;

            case ZombieAIState.Attack:
                StopLoop();
                break;
        }
    }

    public void OnAttack()
    {
        if (isDead) return;
        PlayOneShot(attackClip, attackVolume);
    }

    public void OnDeath()
    {
        if (isDead) return;
        isDead = true;
        StopLoop();
        PlayOneShot(deathClip, deathVolume);
    }

    // ── Idle growl timer (runs in Update) ────────────────────

    void Update()
    {
        if (isDead) return;
        if (lastState != ZombieAIState.Idle) return;
        if (idleGrowlClip == null) return;

        idleGrowlTimer -= Time.deltaTime;
        if (idleGrowlTimer <= 0f)
        {
            PlayOneShot(idleGrowlClip, idleVolume);
            ResetIdleTimer();
        }
    }

    // ── Helpers ───────────────────────────────────────────────

    void StartChaseLoop()
    {
        if (chaseGrowlClip == null) return;
        audioSource.clip   = chaseGrowlClip;
        audioSource.volume = chaseVolume;
        audioSource.loop   = true;
        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    void StopLoop()
    {
        if (audioSource.isPlaying && audioSource.loop)
            audioSource.Stop();
        audioSource.loop = false;
    }

    void PlayOneShot(AudioClip clip, float volume)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip, volume);
    }

    void ResetIdleTimer()
    {
        idleGrowlTimer = Random.Range(idleGrowlMinInterval, idleGrowlMaxInterval);
    }
}

/// <summary>Mirrors the ZombieAI state enum so ZombieAudio stays decoupled.</summary>
public enum ZombieAIState { Idle, Chase, Attack }
