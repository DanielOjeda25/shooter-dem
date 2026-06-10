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
        public AudioClip[] landClips;       // al aterrizar (volumen segun el golpe)

        [Header("Vida baja")]
        public AudioClip heartbeatLoop;            // latido en bucle con poca vida
        [Range(0f, 1f)] public float heartbeatThreshold = 0.3f;   // % de vida que lo activa
        [Range(0f, 1f)] public float heartbeatVolume = 0.8f;

        [Range(0f, 1f)] public float volume = 1f;

        // Anti-eco del quejido: con 2+ enemigos golpeando casi a la vez, cada golpe
        // disparaba su PlayOneShot y las voces se encimaban como eco. Cooldown corto:
        // una sola voz por "tanda" de golpes (el dano se aplica igual; solo es la voz).
        const float HurtVoiceCooldown = 0.35f;
        private float lastHurtTime;

        private AudioSource source;
        private AudioSource heartbeatSource;   // fuente propia: es un LOOP, no puede compartir
        private PlayerHealth health;
        private Movement movement;   // player del pack (puede no estar -> null-safe)

        void Awake()
        {
            health = GetComponent<PlayerHealth>();
            movement = GetComponent<Movement>();
            // SIEMPRE fuente propia (no GetComponent): el AudioSource que ya vive en el
            // player es el de los PASOS del pack, y Movement lo PAUSA con timeScale 0
            // (pausa/game over). Compartirlo silenciaba el grito de muerte: el Lose()
            // congela el tiempo justo despues de Died y el Pause() cortaba el PlayOneShot.
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;   // 2D: feedback del jugador

            // El latido necesita su PROPIA fuente: es un loop (Play/Stop), y la principal
            // se usa con PlayOneShot; compartirla cortaria el loop con cada efecto.
            heartbeatSource = gameObject.AddComponent<AudioSource>();
            heartbeatSource.playOnAwake = false;
            heartbeatSource.spatialBlend = 0f;
            heartbeatSource.loop = true;
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
            LandingBob.Landed += OnLand;   // bus estatico (la camara detecta el aterrizaje)
            // Trepar un borde = esfuerzo similar al salto -> reusa los grunidos de salto.
            LedgeClimb.ClimbStarted += OnJump;
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
            LandingBob.Landed -= OnLand;
            LedgeClimb.ClimbStarted -= OnJump;
            if (heartbeatSource != null) heartbeatSource.Stop();
        }

        // Quejido SOLO en golpe no letal (en el letal suena el grito de muerte),
        // y como mucho una voz cada HurtVoiceCooldown (anti-eco con varios enemigos).
        void OnDamaged(int current, int max)
        {
            UpdateHeartbeat(current, max);

            if (current <= 0) return;
            if (Time.time < lastHurtTime + HurtVoiceCooldown) return;
            lastHurtTime = Time.time;
            PlayRandom(hurtClips);
        }

        // Latido: arranca al caer del umbral de vida y para al morir (no hay curacion aun;
        // si algun dia existe, este mismo check lo apaga al subir de vida).
        void UpdateHeartbeat(int current, int max)
        {
            if (heartbeatSource == null || heartbeatLoop == null) return;
            bool low = current > 0 && max > 0 && (float)current / max <= heartbeatThreshold;
            if (low && !heartbeatSource.isPlaying)
            {
                heartbeatSource.clip = heartbeatLoop;
                heartbeatSource.volume = heartbeatVolume;
                heartbeatSource.Play();
            }
            else if (!low && heartbeatSource.isPlaying)
            {
                heartbeatSource.Stop();
            }
        }

        void OnDied()
        {
            if (heartbeatSource != null) heartbeatSource.Stop();   // muerto: corta el latido
            PlayRandom(deathClips);
        }

        // Aterrizaje: volumen segun la fuerza del golpe (caida corta = suave, larga = fuerte).
        void OnLand(float impact)
        {
            if (landClips == null || landClips.Length == 0) return;
            var clip = landClips[Random.Range(0, landClips.Length)];
            if (clip != null) source.PlayOneShot(clip, volume * Mathf.Lerp(0.4f, 1f, impact));
        }

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
