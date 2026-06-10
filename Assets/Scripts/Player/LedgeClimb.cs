using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShooterDem
{
    /// <summary>
    /// Trepado de bordes (mantle): cerca de un muro con una plataforma encima, ESPACIO
    /// detecta el borde y sube al jugador con una curva fluida (subir → adelante).
    /// Detección con 2 raycasts: pecho→pared y, desde encima de la pared, hacia abajo
    /// para encontrar el "techo" plano. Durante la subida el Movement queda suspendido
    /// y el rigidbody en kinemático (nada de física peleando con la curva).
    /// Va en el GameObject raíz del player (junto a Rigidbody + CapsuleCollider + Movement).
    /// </summary>
    public class LedgeClimb : MonoBehaviour
    {
        [Header("Detección")]
        [Tooltip("Distancia máxima a la pared para agarrarse (desde el borde de la cápsula).")]
        public float maxGrabDistance = 0.9f;
        [Tooltip("Altura mínima del borde sobre los pies (menos que esto = es un escalón, no hace falta).")]
        public float minLedgeHeight = 0.9f;
        [Tooltip("Altura máxima alcanzable (plataformas más altas que el jugador).")]
        public float maxLedgeHeight = 2.4f;
        [Tooltip("Qué capas se pueden trepar (el layer propio se excluye solo).")]
        public LayerMask climbMask = ~0;

        [Header("Subida")]
        [Tooltip("Duración total del mantle (segundos).")]
        public float climbDuration = 0.55f;

        // Bus estático: "empezó un trepado" (para anim del viewmodel / sonido a futuro).
        public static event System.Action ClimbStarted;

        private Rigidbody body;
        private CapsuleCollider capsule;
        private InfimaGames.LowPolyShooterPack.Movement movement;
        private InputAction jumpAction;
        private bool climbing;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            movement = GetComponent<InfimaGames.LowPolyShooterPack.Movement>();
        }

        void Start()
        {
            var playerInput = GetComponentInChildren<PlayerInput>(true);
            if (playerInput != null) jumpAction = playerInput.actions.FindAction("Jump");
        }

        void Update()
        {
            if (climbing || Time.timeScale <= 0f) return;
            if (jumpAction != null && jumpAction.WasPressedThisFrame())
                TryClimb();
        }

        void TryClimb()
        {
            int mask = climbMask & ~(1 << gameObject.layer);   // nunca treparse a si mismo
            Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
            float feetY = capsule.bounds.min.y;

            // 1) ¿hay PARED al frente? (rayo a la altura del pecho)
            Vector3 chest = new Vector3(transform.position.x, feetY + 1.2f, transform.position.z);
            if (!Physics.Raycast(chest, fwd, out var wall, maxGrabDistance + capsule.radius, mask, QueryTriggerInteraction.Ignore))
                return;

            // 2) ¿hay TECHO plano encima de esa pared? (rayo hacia abajo, un poco metido)
            Vector3 above = wall.point + fwd * 0.15f;
            above.y = feetY + maxLedgeHeight + 0.1f;
            if (!Physics.Raycast(above, Vector3.down, out var top, maxLedgeHeight + 0.1f, mask, QueryTriggerInteraction.Ignore))
                return;
            if (top.normal.y < 0.6f) return;             // superficie inclinada: no es un borde

            float h = top.point.y - feetY;
            if (h < minLedgeHeight || h > maxLedgeHeight) return;

            // 3) ¿hay LUGAR para pararse arriba? (cápsula fantasma en el destino)
            Vector3 stand = top.point + fwd * 0.25f;
            Vector3 capBottom = stand + Vector3.up * (capsule.radius + 0.05f);
            Vector3 capTop = stand + Vector3.up * (capsule.height - capsule.radius + 0.05f);
            if (Physics.CheckCapsule(capBottom, capTop, capsule.radius * 0.9f, mask, QueryTriggerInteraction.Ignore))
                return;

            StartCoroutine(Climb(stand));
        }

        IEnumerator Climb(Vector3 stand)
        {
            climbing = true;
            movement.Suspended = true;
            body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
            ClimbStarted?.Invoke();

            // destino: transform tal que los PIES queden sobre el borde
            float feetOffset = transform.position.y - capsule.bounds.min.y;
            Vector3 start = transform.position;
            Vector3 target = stand + Vector3.up * (feetOffset + 0.02f);
            Vector3 mid = new Vector3(start.x, target.y + 0.05f, start.z);   // fase 1: subir

            float upDur = climbDuration * 0.55f, fwdDur = climbDuration * 0.45f, t = 0f;
            while (t < upDur)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(start, mid, Smooth(t / upDur));
                yield return null;
            }
            t = 0f;
            while (t < fwdDur)   // fase 2: adelante, sobre la plataforma
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(mid, target, Smooth(t / fwdDur));
                yield return null;
            }
            transform.position = target;

            body.isKinematic = false;
            body.linearVelocity = Vector3.zero;
            movement.Suspended = false;
            climbing = false;
        }

        static float Smooth(float x) => x * x * (3f - 2f * x);   // easing suave (smoothstep)
    }
}
