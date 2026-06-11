using UnityEngine;
using UnityEngine.InputSystem;

namespace ShooterDem
{
    /// <summary>
    /// Agarre de objetos físicos estilo Serious Sam 2 / Half-Life 2: con E agarrás un
    /// Rigidbody que tengas delante, flota frente a la cámara y lo llevás con vos;
    /// click izquierdo lo ARROJA, E de nuevo lo suelta suave.
    ///
    /// El sostén NO desactiva la física: cada FixedUpdate se le da al objeto la velocidad
    /// hacia el "punto de mano" (resorte de velocidad). Por eso el objeto sigue chocando,
    /// empujando enemigos, y si queda trabado detrás de una pared se suelta solo.
    /// Va en el GameObject raíz del player (usa la cámara principal para apuntar).
    /// </summary>
    public class PhysicsCarry : MonoBehaviour
    {
        [Header("Agarre (E)")]
        [Tooltip("Alcance del agarre desde la cámara.")]
        public float grabRange = 2.8f;
        [Tooltip("Masa máxima agarrable (el barril pesa 35; enemigos/props pesados quedan fuera).")]
        public float maxGrabMass = 60f;
        public LayerMask grabMask = ~0;

        [Header("Sostén")]
        [Tooltip("Distancia del objeto frente a la cámara.")]
        public float holdDistance = 1.9f;
        [Tooltip("Qué tan fuerte 'persigue' la mano (resorte). Más alto = más rígido.")]
        public float followStrength = 12f;
        [Tooltip("Velocidad máxima del objeto sostenido (que no sea un látigo).")]
        public float maxCarrySpeed = 14f;
        [Tooltip("Si queda trabado a esta distancia (pared en el medio), se suelta solo.")]
        public float breakDistance = 4f;

        [Header("Lanzamiento (click izq)")]
        [Tooltip("Velocidad de salida al arrojar (m/s) — constante, sin importar la masa.")]
        public float throwSpeed = 13f;

        // Estado para la UI (el HUD muestra "E agarrar" / "click arrojar · E soltar").
        public enum CarryState { None, CanGrab, Holding }
        public CarryState State { get; private set; }

        [Tooltip("Sacudida de camara al arrojar (kick del esfuerzo).")]
        public float throwShake = 0.25f;

        // Buses estaticos: "agarro" / "arrojo el objeto" (los escucha PlayerAudio para el
        // sonido + el viewmodel para la anim de empuje). Sin cableado de Inspector.
        public static event System.Action Grabbed;
        public static event System.Action Thrown;

        private Rigidbody held;          // lo que llevamos (null = manos vacías)
        private bool heldUsedGravity;    // para restaurar el objeto tal como estaba
        private Transform cam;
        private PlayerHealth health;

        void Awake() { health = GetComponent<PlayerHealth>(); }

        // Al morir se suelta lo que lleves (si no, el objeto quedaba flotando para siempre).
        void OnEnable()  { if (health != null) health.Died += Drop; }
        void OnDisable() { if (health != null) health.Died -= Drop; Drop(); }

        void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (cam == null)
            {
                cam = Camera.main != null ? Camera.main.transform : null;
                if (cam == null) return;
            }

            var kb = Keyboard.current;
            var mouse = Mouse.current;

            if (held == null)
            {
                // Estado para el prompt del HUD: ¿hay algo agarrable bajo la mira?
                State = FindGrabbable(out _) ? CarryState.CanGrab : CarryState.None;
                if (State == CarryState.CanGrab && kb != null && kb.eKey.wasPressedThisFrame)
                    TryGrab();
                return;
            }

            State = CarryState.Holding;
            // Sosteniendo: E = soltar suave · click izq = arrojar.
            // (El click está libre porque UnarmedMode deshabilita el Fire del arma.)
            if (kb != null && kb.eKey.wasPressedThisFrame) { Drop(); return; }
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) Throw();
        }

        void FixedUpdate()
        {
            if (held == null) return;

            // Resorte de velocidad: el objeto persigue el punto de mano. No se teletransporta
            // -> conserva colisiones (empuja cosas, una pared lo frena) y se siente con peso.
            Vector3 target = cam.position + cam.forward * holdDistance;
            Vector3 to = target - held.worldCenterOfMass;
            if (to.magnitude > breakDistance) { Drop(); return; }   // trabado: se suelta solo

            Vector3 vel = to * followStrength;
            if (vel.sqrMagnitude > maxCarrySpeed * maxCarrySpeed)
                vel = vel.normalized * maxCarrySpeed;
            held.linearVelocity = vel;
            held.angularVelocity *= 0.85f;   // amortigua el giro (que no rote como trompo)
        }

        // ¿Hay un objeto agarrable bajo la mira? (lo usan el prompt del HUD y el agarre:
        // misma regla exacta -> el texto nunca miente sobre lo que E puede hacer).
        bool FindGrabbable(out Rigidbody rb)
        {
            rb = null;
            int mask = grabMask & ~(1 << gameObject.layer);   // nunca agarrarse a sí mismo
            if (!Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, grabRange, mask, QueryTriggerInteraction.Ignore))
                return false;

            // Solo cuerpos físicos REALES y liviano-medibles: sin rigidbody, kinemáticos
            // (enemigos por NavMesh, plataformas) o demasiado pesados, no se agarran.
            rb = hit.rigidbody;
            return rb != null && !rb.isKinematic && rb.mass <= maxGrabMass;
        }

        void TryGrab()
        {
            if (!FindGrabbable(out Rigidbody rb)) return;

            held = rb;
            heldUsedGravity = rb.useGravity;
            rb.useGravity = false;           // en la "mano" no pesa (el resorte lo sostiene)
            Grabbed?.Invoke();               // sonido de agarre
        }

        void Drop()
        {
            if (held == null) return;
            held.useGravity = heldUsedGravity;
            held = null;
        }

        void Throw()
        {
            var rb = held;
            Drop();                                        // restaura gravedad ANTES del envión
            rb.linearVelocity = cam.forward * throwSpeed;  // velocidad constante: arco predecible
            CameraShake.Add(throwShake);                   // kick de camara: se siente el esfuerzo
            Thrown?.Invoke();                              // anim de empuje del viewmodel
        }
    }
}
