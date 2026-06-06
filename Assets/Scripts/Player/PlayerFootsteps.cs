using UnityEngine;

// Sonido de pasos y de sprint. Lee el estado de PlayerMovement y reproduce un paso
// al azar a un ritmo segun el estado (agachado lento, andar medio, sprint rapido)
// mientras el jugador este EN SUELO y MOVIENDOSE. El clip de sprint suena una vez
// al empezar a esprintar. Va en el Player, con su propio AudioSource (2D).
[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerMovement movement;

    [Header("Clips")]
    public AudioClip[] footstepClips;   // variantes de paso
    public AudioClip sprintClip;        // golpe unico al iniciar el sprint

    [Header("Ritmo (segundos entre pasos)")]
    public float walkInterval = 0.5f;
    public float sprintInterval = 0.35f;
    public float crouchInterval = 0.7f;

    private AudioSource audioSource;
    private float stepTimer;
    private bool wasSprinting;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        if (movement == null) movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (Time.timeScale == 0f || movement == null) return;

        // Sprint: un sonido al pasar de NO-sprint a sprint.
        if (movement.IsSprinting && !wasSprinting && sprintClip != null)
            audioSource.PlayOneShot(sprintClip);
        wasSprinting = movement.IsSprinting;

        // Pasos: solo en suelo y en movimiento.
        if (!movement.IsGrounded || !movement.IsMoving)
        {
            stepTimer = 0f;   // al reanudar, el primer paso suena enseguida
            return;
        }

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            stepTimer = movement.IsCrouching ? crouchInterval
                      : movement.IsSprinting ? sprintInterval
                      : walkInterval;
            PlayStep();
        }
    }

    void PlayStep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip != null) audioSource.PlayOneShot(clip);
    }
}
