using UnityEngine;

namespace ShooterDem
{
    /// <summary>
    /// "Aterrizaje": al caer y tocar el suelo, la cámara baja un toque y rebota (resorte
    /// amortiguado). El golpe escala con la velocidad de caída. Sirve para saltos, caídas
    /// y el aterrizaje tras la explosión de un barril.
    /// Va en la CÁMARA (hijo del player); lee la velocidad vertical del Rigidbody del player.
    /// </summary>
    public class LandingBob : MonoBehaviour
    {
        [Tooltip("Cuánto baja la cámara en el aterrizaje más fuerte (metros).")]
        public float maxDip = 0.12f;
        [Tooltip("Velocidad de caída mínima para que se note el rebote.")]
        public float minImpactSpeed = 3f;
        [Tooltip("Velocidad de caída para el rebote máximo.")]
        public float maxImpactSpeed = 16f;
        [Tooltip("Rigidez del resorte (más alto = vuelve más rápido).")]
        public float spring = 140f;
        [Tooltip("Amortiguación (más alto = menos rebote).")]
        public float damping = 14f;

        // Bus estatico: "el player aterrizo" (0..1 = fuerza del impacto). Lo escucha
        // PlayerAudio para el sonido de caida, sin cableado en el Inspector.
        public static event System.Action<float> Landed;

        [Header("Bob al moverse (sincronizado con la anim de correr)")]
        [Tooltip("Amplitud del bamboleo a velocidad de CORRER (m).")]
        public float bobRunAmount = 0.035f;
        [Tooltip("PISADAS por segundo. Derivado de la anim: 2 zancadas / 0.6667s * timeScale " +
                 "del run en el Blend Tree (1.3) = 3.9. Si cambias el timeScale, recalcular.")]
        public float stepsPerSecond = 3.9f;
        [Tooltip("Velocidad donde ARRANCA el blend a run (= threshold idle del Blend Tree).")]
        public float blendStartSpeed = 4.3f;
        [Tooltip("Velocidad de run pleno (= threshold run del Blend Tree).")]
        public float blendEndSpeed = 6.8f;
        [Tooltip("Vaiven lateral como fraccion del vertical.")]
        [Range(0f, 1f)] public float swayFactor = 0.6f;

        private Rigidbody body;
        private Vector3 baseLocalPos;
        private float offset, velocity, prevYVel;
        private float bobTimer, bobAmp;   // fase del paso + amplitud suavizada

        void Start()
        {
            body = GetComponentInParent<Rigidbody>();
            baseLocalPos = transform.localPosition;
        }

        void LateUpdate()
        {
            float yv = body != null ? body.linearVelocity.y : 0f;

            // Aterrizaje: venía cayendo (prevYVel muy negativo) y se frenó de golpe.
            if (prevYVel < -minImpactSpeed && yv > prevYVel + 1.5f)
            {
                float impact = Mathf.InverseLerp(minImpactSpeed, maxImpactSpeed, -prevYVel);
                offset = -maxDip * impact;   // la cámara baja de golpe
                velocity = 0f;
                Landed?.Invoke(impact);      // avisa (sonido de caida, etc.)
            }
            prevYVel = yv;

            // Resorte amortiguado: el offset vuelve a 0 (baja y sube).
            float accel = -spring * offset - damping * velocity;
            velocity += accel * Time.deltaTime;
            offset += velocity * Time.deltaTime;

            // --- Bob de pasos: ESPEJO del Blend Tree de los brazos ---
            // La anim de correr NO cambia de velocidad con el movimiento (el blend cambia
            // PESOS, no ritmo) -> el bob tampoco: frecuencia CONSTANTE (= pisadas de la anim)
            // y la AMPLITUD sigue el mismo peso idle->run que el Blend Tree (4.3 a 6.8 m/s).
            // Asi caminar = sin bob (los brazos estan en idle) y esprintar = bob clavado.
            Vector3 bob = Vector3.zero;
            if (Time.timeScale > 0f)
            {
                Vector3 hv = body != null ? body.linearVelocity : Vector3.zero;
                hv.y = 0f;
                bool grounded = Mathf.Abs(yv) < 1.5f;   // sin bob en salto/caida
                float runWeight = grounded ? Mathf.InverseLerp(blendStartSpeed, blendEndSpeed, hv.magnitude) : 0f;
                bobAmp = Mathf.MoveTowards(bobAmp, runWeight, Time.deltaTime * 3f);
                if (bobAmp > 0.001f)
                {
                    // fase en pasos: |sin| tiene periodo PI -> una hundida por pisada
                    bobTimer += Time.deltaTime * stepsPerSecond * Mathf.PI;
                    float a = bobRunAmount * bobAmp;
                    // vertical: 1 golpe por pisada · lateral: 1 vaiven por ciclo de brazos (2 pisadas)
                    bob = new Vector3(Mathf.Sin(bobTimer) * a * swayFactor,
                                      -Mathf.Abs(Mathf.Sin(bobTimer)) * a, 0f);
                }
            }

            transform.localPosition = baseLocalPos + Vector3.up * offset + bob;
        }
    }
}
