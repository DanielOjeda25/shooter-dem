using UnityEngine;
using UnityEngine.InputSystem;

namespace ShooterDem
{
    /// <summary>
    /// Viewmodel de PISTOLA: maneja el Animator de brazos+arma (AC_Pistol). El idle
    /// loopea solo; R dispara el trigger de RECARGA (el clip mueve manos + corredera +
    /// cargador, animados como objetos desde Blender). Primer test de arma animada:
    /// el disparo y la munición se cablean después.
    /// Va en el GameObject del viewmodel de pistola (junto al Animator).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PistolViewmodel : MonoBehaviour
    {
        [Tooltip("Sonidos de recarga (2D). Varias variantes = se elige una al azar.")]
        public AudioClip[] reloadClips;
        [Range(0f, 1f)] public float reloadVolume = 1f;

        private Animator animator;
        private AudioSource audioSource;
        private static readonly int ReloadHash = Animator.StringToHash("Reload");
        private static readonly int InspectHash = Animator.StringToHash("Inspect");

        void Awake()
        {
            animator = GetComponent<Animator>();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;   // 2D: el arma está "en mano"
        }

        void Update()
        {
            if (Time.timeScale <= 0f) return;
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                animator.SetTrigger(ReloadHash);
                AudioUtil.PlayRandom(audioSource, reloadClips, reloadVolume);
            }

            // T = inspeccionar el arma (flourish; el clip deja las partes quietas/cargadas).
            if (kb != null && kb.tKey.wasPressedThisFrame)
                animator.SetTrigger(InspectHash);
        }
    }
}
