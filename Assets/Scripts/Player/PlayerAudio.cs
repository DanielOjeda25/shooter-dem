using UnityEngine;
using InfimaGames.LowPolyShooterPack;   // Movement del pack (eventos de salto/dash/stamina)

namespace ShooterDem
{
    /// <summary>
    /// Voces y sonidos del jugador (2D, feedback directo):
    ///  - quejido al recibir daño y grito al morir (eventos de PlayerHealth)
    ///  - salto, dash y "sin stamina" (eventos del Movement del pack)
    /// Es 2D porque es feedback del propio jugador, no algo posicional.
    /// Va en el GameObject raíz del player, junto a PlayerHealth (+ Movement del pack).
    /// </summary>
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerAudio : MonoBehaviour
    {
        [Header("Vida (variantes = se elige una al azar)")]
        public AudioClip[] hurtClips;   // al recibir daño NO letal
        public AudioClip[] deathClips;  // al morir

        [Header("Movimiento (player del pack)")]
        public AudioClip[] jumpClips;       // al saltar
        public AudioClip[] dashClips;       // al dashear (Alt)
        public AudioClip[] noStaminaClips;  // al intentar dashear sin stamina

        [Range(0f, 1f)] public float volume = 1f;

        private AudioSource source;
        private PlayerHealth health;
        private Movement movement;   // player del pack (puede no estar -> null-safe)

        void Awake()
        {
            health = GetComponent<PlayerHealth>();
            movement = GetComponent<Movement>();
            source = GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;   // 2D: feedback del jugador
        }

        void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
            if (movement != null)
            {
                movement.Jumped += OnJump;
                movement.Dashed += OnDash;
                movement.StaminaDenied += OnNoStamina;
            }
        }

        void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
            if (movement != null)
            {
                movement.Jumped -= OnJump;
                movement.Dashed -= OnDash;
                movement.StaminaDenied -= OnNoStamina;
            }
        }

        // Quejido SOLO en golpe no letal (en el letal suena el grito de muerte).
        void OnDamaged(int current, int max)
        {
            if (current > 0) PlayRandom(hurtClips);
        }

        void OnDied() => PlayRandom(deathClips);

        void OnJump() => PlayRandom(jumpClips);
        void OnDash() => PlayRandom(dashClips);
        void OnNoStamina() => PlayRandom(noStaminaClips);

        void PlayRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;
            var clip = clips[Random.Range(0, clips.Length)];
            if (clip != null) source.PlayOneShot(clip, volume);
        }
    }
}
