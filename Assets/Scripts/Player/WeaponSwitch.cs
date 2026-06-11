using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShooterDem
{
    /// <summary>
    /// Sistema de slots de viewmodel: cada slot es un GameObject de brazos (con su Animator).
    /// Slot 1 = desarmado, slot 2 = pistola, y así iremos sumando. Se elige con las teclas
    /// numéricas (1,2,...) o con la rueda del ratón. Al CAMBIAR, si el slot que dejamos tiene
    /// animación de guardar (trigger "Holster" en su Animator), la reproduce y espera antes de
    /// ocultarlo; el slot nuevo, al activarse, arranca por su estado Draw (sacar) solo.
    /// Va en el Player; los slots se asignan en el Inspector (orden = tecla 1,2,3...).
    /// </summary>
    public class WeaponSwitch : MonoBehaviour
    {
        [Tooltip("Viewmodels por slot. Índice 0 = tecla 1 (desarmado), 1 = tecla 2 (pistola), ...")]
        public GameObject[] slots;
        [Tooltip("Cuánto dura la animación de guardar (s) antes de ocultar el viewmodel.")]
        public float holsterTime = 0.4f;

        [Tooltip("Sonido 2D al cambiar de arma (changeGun).")]
        public AudioClip switchClip;
        [Range(0f, 1f)] public float switchVolume = 0.9f;

        private int current;
        private bool switching;
        private AudioSource audioSource;   // fuente 2D propia (feedback de interfaz)

        private static readonly int HolsterHash = Animator.StringToHash("Holster");

        void Start()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;   // 2D
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null) slots[i].SetActive(i == current);
        }

        void Update()
        {
            if (switching || slots == null || slots.Length == 0) return;
            var kb = Keyboard.current;
            int want = -1;

            // teclas numéricas: 1..4 mapean a slot 0..3
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) want = 0;
                else if (kb.digit2Key.wasPressedThisFrame) want = 1;
                else if (kb.digit3Key.wasPressedThisFrame) want = 2;
                else if (kb.digit4Key.wasPressedThisFrame) want = 3;
            }

            // rueda del ratón: cicla entre slots
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.5f) want = (current + 1) % slots.Length;
                else if (scroll < -0.5f) want = (current - 1 + slots.Length) % slots.Length;
            }

            if (want >= 0 && want < slots.Length && want != current && slots[want] != null)
                StartCoroutine(SwitchTo(want));
        }

        IEnumerator SwitchTo(int idx)
        {
            switching = true;
            if (switchClip != null) audioSource.PlayOneShot(switchClip, switchVolume);

            // guardar el actual: si tiene anim de Holster, reproducir y esperar
            var cur = current >= 0 && current < slots.Length ? slots[current] : null;
            if (cur != null)
            {
                var anim = cur.GetComponent<Animator>();
                if (anim != null && HasParam(anim, "Holster"))
                {
                    anim.SetTrigger(HolsterHash);
                    float t = 0f;
                    while (t < holsterTime) { t += Time.unscaledDeltaTime; yield return null; }
                }
                cur.SetActive(false);
            }

            // sacar el nuevo: al activarse, su Animator entra por Draw solo
            current = idx;
            if (slots[idx] != null) slots[idx].SetActive(true);
            switching = false;
        }

        static bool HasParam(Animator a, string name)
        {
            foreach (var p in a.parameters) if (p.name == name) return true;
            return false;
        }
    }
}
