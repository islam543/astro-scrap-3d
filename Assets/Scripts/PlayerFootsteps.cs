using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Plays a footstep sound while the player is moving on the ground.
/// Attach to PlayerCapsule. Requires an AudioSource on the same GameObject.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CharacterController))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip footstepClip;          // assign the u_6j... clip here

    [Header("Tuning")]
    [Tooltip("How many seconds between each footstep trigger")]
    public float stepInterval = 0.45f;      // adjust to match clip rhythm

    [Tooltip("Minimum speed before footsteps play")]
    public float minSpeed = 0.3f;

    [Range(0f, 1f)]
    public float volume = 0.7f;

    // ── internal ──────────────────────────────────────────────
    private AudioSource      audioSource;
    private CharacterController cc;
    private float            stepTimer = 0f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        cc          = GetComponent<CharacterController>();

        // Configure AudioSource for footsteps
        audioSource.clip        = footstepClip;
        audioSource.loop        = false;
        audioSource.playOnAwake = false;
        audioSource.volume      = volume;
        audioSource.spatialBlend = 0f;    // 2D sound (heard by the player, not positional)
    }

    void Update()
    {
        // Only play when grounded and actually moving
        bool  grounded    = cc.isGrounded;
        float horizSpeed  = new Vector3(cc.velocity.x, 0, cc.velocity.z).magnitude;

        if (grounded && horizSpeed > minSpeed)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                stepTimer = stepInterval;
                PlayStep();
            }
        }
        else
        {
            // Reset timer so first step after landing plays immediately
            stepTimer = 0f;
            if (audioSource.isPlaying)
                audioSource.Stop();
        }
    }

    void PlayStep()
    {
        if (footstepClip == null) return;
        // Slight random pitch variation makes it feel less robotic
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(footstepClip, volume);
    }
}
