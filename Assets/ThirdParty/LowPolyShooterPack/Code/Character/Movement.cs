// Copyright 2021, Infima Games. All Rights Reserved.

using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class Movement : MovementBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("Audio Clips")]
        
        [Tooltip("The audio clip that is played while walking.")]
        [SerializeField]
        private AudioClip audioClipWalking;

        [Tooltip("The audio clip that is played while running.")]
        [SerializeField]
        private AudioClip audioClipRunning;

        [Header("Speeds")]

        [SerializeField]
        private float speedWalking = 5.0f;

        [Tooltip("How fast the player moves while running."), SerializeField]
        private float speedRunning = 9.0f;

        [Tooltip("Fuerza del salto (ASHFALL: el free sample no traia salto)."), SerializeField]
        private float jumpForce = 5.0f;

        [Header("Parkour (ASHFALL)")]
        [Tooltip("Correccion de trayectoria EN EL AIRE (m/s2). El momentum del despegue se " +
                 "conserva; esto solo permite corregir el rumbo (estandar Quake/Source).")]
        [SerializeField] private float airAcceleration = 30f;
        [Tooltip("Altura maxima de escalon que se sube solo al caminar (mas alto = mantle).")]
        [SerializeField] private float maxStepHeight = 0.45f;
        [Tooltip("Empuje hacia abajo en el piso: mantiene pegado al BAJAR rampas (sin saltitos).")]
        [SerializeField] private float groundStick = 1.5f;

        [Header("Crouch (ASHFALL)")]
        [Tooltip("Velocidad agachado (Ctrl izquierdo).")]
        [SerializeField] private float crouchSpeed = 2.5f;
        [Tooltip("Altura agachado como fraccion de la altura de pie.")]
        [SerializeField] private float crouchHeightFactor = 0.6f;
        [Tooltip("Velocidad de la transicion de altura (m/s).")]
        [SerializeField] private float crouchTransitionSpeed = 6f;

        [Header("Stamina / Dash (ASHFALL)")]
        [Tooltip("Dash con Alt. DESACTIVADO por decision de diseno (daba demasiada ventaja); " +
                 "el codigo queda por si se retoma a futuro.")]
        [SerializeField] private bool dashEnabled = false;
        [SerializeField] private float staminaMax = 100f;
        [SerializeField] private float staminaRegenPerSecond = 25f;       // regen cuando NO esprintas
        [SerializeField] private float staminaSprintDrainPerSecond = 18f; // drena al esprintar
        [Tooltip("Envion del dash (Alt izquierdo)."), SerializeField] private float dashSpeed = 20f;
        [SerializeField] private float dashDuration = 0.18f;
        [SerializeField] private float dashCost = 35f;

        #endregion

        #region PROPERTIES

        //Velocity.
        private Vector3 Velocity
        {
            //Getter.
            get => rigidBody.linearVelocity;
            //Setter.
            set => rigidBody.linearVelocity = value;
        }

        // Para el HUD (ASHFALL): stamina 0..1 y si esta dasheando.
        public float Stamina01 => staminaMax > 0f ? Mathf.Clamp01(stamina / staminaMax) : 0f;
        public bool IsDashing => dashTimer > 0f;
        // Fuente unica de verdad del "estoy en el piso" (la usan bob de camara, stamina, salto).
        public bool Grounded => grounded;
        public bool IsCrouching => crouching;
        // Para LedgeClimb (vault a sprint): si el jugador mantiene el correr.
        public bool SprintHeld => playerCharacter != null && playerCharacter.IsRunning();
        // FUENTE UNICA de "esprintando DE VERDAD": Shift + sin agotar + de pie. La consumen
        // la velocidad, los pasos y el bob de camara -> imposible que se desincronicen
        // (antes cada uno preguntaba distinto: a 0 stamina caminabas pero sonaba/bobeaba carrera).
        public bool SprintingEffective => SprintHeld && !exhausted && !crouching;

        // Eventos para el audio del jugador (ASHFALL): los escucha PlayerAudio.
        public event System.Action Jumped;
        public event System.Action Dashed;
        public event System.Action StaminaDenied;   // intento de dash sin stamina suficiente

        #endregion

        #region FIELDS

        /// <summary>
        /// Attached Rigidbody.
        /// </summary>
        private Rigidbody rigidBody;
        /// <summary>
        /// Attached CapsuleCollider.
        /// </summary>
        private CapsuleCollider capsule;
        /// <summary>
        /// Attached AudioSource.
        /// </summary>
        private AudioSource audioSource;
        
        /// <summary>
        /// True if the character is currently grounded.
        /// </summary>
        private bool grounded;

        // Salto (ASHFALL): se encola al presionar y se consume en FixedUpdate.
        // La cola CADUCA (jump buffer): sin esto, un Espacio presionado en el aire quedaba
        // armado para siempre y el salto+sonido disparaban al ATERRIZAR.
        private bool jumpQueued;
        private float jumpQueuedTimer;
        private const float JumpBufferTime = 0.15f;   // ventana para "pulsar un pelin antes"
        private InputAction jumpAction;

        // Stamina / Dash (ASHFALL).
        private float stamina;
        // "Agotado" (v1 restaurado): al vaciar la stamina quedas SIN sprint hasta recuperar
        // el 30%. Sin esta histeresis, en el borde del 0 el sprint parpadea on/off cada frame.
        private bool exhausted;
        private float dashTimer;
        private Vector3 dashDir;
        private ShooterDem.PlayerHealth playerHealth;   // para i-frames durante el dash

        // Knockback (ASHFALL): mientras dura, MoveCharacter cede el control a la fisica.
        private float knockbackTimer;

        // Suspension (ASHFALL): el trepado de bordes toma el control TOTAL del movimiento
        // (LedgeClimb mueve el transform a mano). Al suspender se cancela el salto en cola.
        private bool suspended;
        public bool Suspended
        {
            get => suspended;
            set { suspended = value; if (value) jumpQueued = false; }
        }

        /// <summary>
        /// ASHFALL: empuja al player con una velocidad dada y le quita el control del movimiento
        /// por 'duration' segundos (para explosiones de barril -> vuelo + caida real).
        /// </summary>
        public void ApplyKnockback(Vector3 velocity, float duration)
        {
            if (rigidBody == null) rigidBody = GetComponent<Rigidbody>();
            rigidBody.linearVelocity = velocity;
            knockbackTimer = Mathf.Max(knockbackTimer, duration);
        }

        /// <summary>
        /// Player Character.
        /// </summary>
        private CharacterBehaviour playerCharacter;
        /// <summary>
        /// The player character's equipped weapon.
        /// </summary>
        private WeaponBehaviour equippedWeapon;
        
        /// <summary>
        /// Array of RaycastHits used for ground checking.
        /// </summary>
        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        #endregion

        #region UNITY FUNCTIONS

        /// <summary>
        /// Awake.
        /// </summary>
        protected override void Awake()
        {
            //Get Player Character.
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
        }

        /// Initializes the FpsController on start.
        protected override  void Start()
        {
            //Rigidbody Setup.
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
            //Cache the CapsuleCollider.
            capsule = GetComponent<CapsuleCollider>();

            //Audio Source Setup.
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = audioClipWalking;
            audioSource.loop = true;

            //Salto (ASHFALL): gravedad activa + cache de la accion "Jump" del PlayerInput.
            rigidBody.useGravity = true;
            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null) jumpAction = playerInput.actions.FindAction("Jump");

            //Stamina / Dash (ASHFALL).
            stamina = staminaMax;
            playerHealth = GetComponent<ShooterDem.PlayerHealth>();

            //Parkour (ASHFALL): friccion CERO en la capsula -> el cuerpo no se queda "pegado"
            //a las paredes al rozarlas en un salto. No causa resbalones en rampas porque la
            //velocidad en el piso se setea cada tick (no depende de la friccion).
            var noFriction = new PhysicsMaterial("PlayerNoFriction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
            capsule.material = noFriction;

            //Crouch (ASHFALL): alturas base de la capsula.
            standHeight = capsule.height;
            standCenterY = capsule.center.y;
        }

        //ASHFALL: grounded DETERMINISTICO (reemplaza el OnCollisionStay original del pack).
        //El sistema por contactos fallaba dos veces: (1) rozar PAREDES contaba como piso;
        //(2) saltar rozando una RAMPA mantenia grounded=true durante el ascenso.
        //
        //v2: la regla "vy > 0.5 = aire" curaba el (2) pero rompia las RAMPAS: correr rampa
        //arriba TAMBIEN da vy positiva -> no podias saltar desde una rampa (y la stamina se
        //congelaba "en el aire" sin estarlo). El discriminador correcto no es "subir", es
        //"SEPARARSE de la superficie": Dot(velocidad, normal del piso).
        //  - correr rampa arriba: te moves PARALELO a la rampa -> dot ~ 0 -> es piso.
        //  - saltar: te ALEJAS de la superficie -> dot grande -> es aire.
        //Mas un colchon breve tras saltar (los primeros frames el cast aun roza el piso).
        private float groundedSuppressTimer;                       // tras saltar, ignora piso
        private const float JumpGroundedSuppressTime = 0.15f;

        // Coyote time (ASHFALL): ventana de gracia para saltar JUSTO despues de salir de un
        // borde/rampa (el companero del jump buffer: uno perdona pulsar antes, el otro despues).
        private float coyoteTimer;
        private const float CoyoteTime = 0.12f;

        // Parkour (ASHFALL): normal del piso actual (para proyectar el movimiento en rampas)
        // y estado del crouch (alturas base de la capsula cacheadas en Start).
        private Vector3 groundNormal = Vector3.up;
        private bool crouching;
        private float standHeight;
        private float standCenterY;

        private bool CheckGrounded()
        {
            if (groundedSuppressTimer > 0f)
                return false;                       // acabamos de saltar: aun no es piso

            Bounds b = capsule.bounds;
            float r = capsule.radius * 0.5f;        // angosto: los roces laterales no cuentan
            float dist = b.extents.y - r + 0.08f;   // la esfera llega hasta apenas bajo los pies
            if (!Physics.SphereCast(b.center, r, Vector3.down, out RaycastHit hit, dist,
                                    ~(1 << gameObject.layer), QueryTriggerInteraction.Ignore))
                return false;
            if (hit.normal.y < 0.6f)
                return false;                       // demasiado inclinado: no es caminable

            // ¿Nos estamos separando de la superficie (saltando)? Entonces no es piso.
            if (Vector3.Dot(rigidBody.linearVelocity, hit.normal) >= 0.5f)
                return false;

            groundNormal = hit.normal;              // para proyectar el movimiento en rampas
            return true;
        }

        //(ASHFALL) Step assist: un Rigidbody NO tiene "step offset" (eso era del
        //CharacterController viejo) -> un escalon de 30cm frenaba en seco. Si algo BAJO
        //bloquea el paso y a la altura del escalon esta libre, subimos justo la diferencia.
        //Cubre 0..maxStepHeight; de ahi para arriba es trabajo del mantle (LedgeClimb).
        private void TryStepUp(Vector3 moveVel)
        {
            Vector3 dir = new Vector3(moveVel.x, 0f, moveVel.z);
            if (dir.sqrMagnitude < 0.5f) return;            // sin intencion real de avanzar
            dir.Normalize();

            int mask = ~(1 << gameObject.layer);
            Bounds b = capsule.bounds;
            float feetY = b.min.y;
            float reach = capsule.radius + 0.25f;

            // 1) ¿algo bajo bloquea el paso? (rayo a la altura del tobillo, desde el centro)
            Vector3 low = new Vector3(b.center.x, feetY + 0.08f, b.center.z);
            if (!Physics.Raycast(low, dir, out RaycastHit lowHit, reach, mask, QueryTriggerInteraction.Ignore))
                return;
            if (lowHit.normal.y > 0.6f) return;             // es una rampa: la maneja la proyeccion

            // 2) ¿a la altura del escalon esta libre? (si no, es un muro -> mantle)
            Vector3 high = low + Vector3.up * maxStepHeight;
            if (Physics.Raycast(high, dir, reach, mask, QueryTriggerInteraction.Ignore))
                return;

            // 3) ¿donde esta la superficie del escalon? (rayo hacia abajo pasado el borde)
            Vector3 over = high + dir * (reach * 0.9f);
            if (!Physics.Raycast(over, Vector3.down, out RaycastHit top, maxStepHeight + 0.05f, mask, QueryTriggerInteraction.Ignore))
                return;
            if (top.normal.y < 0.6f) return;

            float lift = top.point.y - feetY;
            if (lift <= 0.02f || lift > maxStepHeight) return;

            // Subimos exactamente la diferencia (+ un pelin de margen para no rozar el borde).
            rigidBody.MovePosition(rigidBody.position + Vector3.up * (lift + 0.02f) + dir * 0.05f);
        }

        //(ASHFALL) ¿Hay espacio para ponerse de pie? (no levantarse a traves de un techo)
        private bool HasHeadroom()
        {
            Bounds b = capsule.bounds;
            float need = (standHeight - capsule.height) + 0.05f;
            Vector3 top = new Vector3(b.center.x, b.max.y - capsule.radius, b.center.z);
            return !Physics.SphereCast(top, capsule.radius * 0.9f, Vector3.up, out _, need,
                                       ~(1 << gameObject.layer), QueryTriggerInteraction.Ignore);
        }
			
        protected override void FixedUpdate()
        {
            //ASHFALL: piso calculado cada paso de fisica (ya no depende de colisiones).
            if (groundedSuppressTimer > 0f) groundedSuppressTimer -= Time.fixedDeltaTime;
            grounded = CheckGrounded();

            //Coyote: en el piso se recarga; en el aire se gasta (deja saltar un pelin tarde).
            if (grounded) coyoteTimer = CoyoteTime;
            else coyoteTimer -= Time.fixedDeltaTime;

            //Move.
            MoveCharacter();
        }

        /// Moves the camera to the character, processes jumping and plays sounds every frame.
        protected override  void Update()
        {
            //ASHFALL: congelado (pausa / game over) o suspendido (trepando un borde)
            //-> cortar pasos y no procesar salto/dash.
            if (Time.timeScale <= 0f || suspended)
            {
                if (audioSource != null && audioSource.isPlaying) audioSource.Pause();
                return;
            }

            //Get the equipped weapon!
            equippedWeapon = playerCharacter.GetInventory().GetEquipped();

            //Pasos (ASHFALL): los maneja PlayerAudio con clips discretos (step1/step2) al
            //ritmo del estado; el loop del pack quedaba crudo. Se desactiva aqui.
            //PlayFootstepSounds();

            //Salto (ASHFALL): detectar el press aqui; se aplica en FixedUpdate.
            if (jumpAction != null && jumpAction.WasPressedThisFrame())
            {
                jumpQueued = true;
                jumpQueuedTimer = JumpBufferTime;
            }
            //La cola caduca si no se consume a tiempo (evita el salto fantasma al aterrizar).
            if (jumpQueued)
            {
                jumpQueuedTimer -= Time.deltaTime;
                if (jumpQueuedTimer <= 0f) jumpQueued = false;
            }

            var kb = Keyboard.current;

            //Crouch (ASHFALL): Ctrl izquierdo mantiene agachado; al soltar se levanta SOLO si
            //hay techo libre. La capsula encoge desde ARRIBA (los pies quedan fijos) y la
            //camara baja via LandingBob.ExtraEyeOffset (el unico que escribe su posicion).
            bool wantCrouch = kb != null && kb.leftCtrlKey.isPressed;
            if (crouching && !wantCrouch && !HasHeadroom()) wantCrouch = true;   // techo: sigue abajo
            crouching = wantCrouch;

            float targetH = crouching ? standHeight * crouchHeightFactor : standHeight;
            if (!Mathf.Approximately(capsule.height, targetH))
            {
                float h = Mathf.MoveTowards(capsule.height, targetH, crouchTransitionSpeed * Time.deltaTime);
                capsule.height = h;
                capsule.center = new Vector3(capsule.center.x,
                                             standCenterY - (standHeight - h) * 0.5f,
                                             capsule.center.z);
            }
            ShooterDem.LandingBob.ExtraEyeOffset = -(standHeight - capsule.height) * 0.9f;

            //Dash (ASHFALL): Alt izquierdo, si hay stamina y no esta dasheando ya.
            if (dashEnabled && dashTimer <= 0f && kb != null && kb.leftAltKey.wasPressedThisFrame)
            {
                if (stamina >= dashCost)
                {
                    Vector2 mv = playerCharacter.GetInputMovement();
                    Vector3 dir = new Vector3(mv.x, 0f, mv.y);
                    dashDir = dir.sqrMagnitude > 0.01f
                        ? transform.TransformDirection(dir.normalized)
                        : transform.forward;
                    dashTimer = dashDuration;
                    stamina -= dashCost;
                    Dashed?.Invoke();
                }
                else
                {
                    StaminaDenied?.Invoke();   // sin stamina para dashear
                }
            }
            //Timer del dash + i-frames mientras dura.
            if (dashTimer > 0f) dashTimer -= Time.deltaTime;
            if (playerHealth != null) playerHealth.Invulnerable = IsDashing;

            //Stamina: drena al esprintar moviendose EN EL PISO; regenera SOLO en el piso.
            //En el aire se CONGELA (ni drena ni regenera): la instrumentacion mostro que
            //regenerar en el aire hacia imposible vaciarla (saltar+correr = sprint infinito).
            //"Exhausted" (v1 restaurado): a 0 quedas sin sprint hasta el 30% -> mientras
            //tanto la regen corre AUNQUE mantengas Shift (antes Shift a 0 bloqueaba la regen).
            if (stamina <= 0f) exhausted = true;
            else if (exhausted && stamina >= staminaMax * 0.3f) exhausted = false;

            bool sprintingNow = SprintingEffective && !IsDashing && grounded
                                && rigidBody.linearVelocity.sqrMagnitude > 0.1f;
            float staminaDelta = sprintingNow ? -staminaSprintDrainPerSecond
                                : (grounded ? staminaRegenPerSecond : 0f);
            stamina = Mathf.Clamp(stamina + staminaDelta * Time.deltaTime, 0f, staminaMax);
        }

        #endregion

        #region METHODS

        private void MoveCharacter()
        {
            //ASHFALL: trepando un borde, el LedgeClimb mueve el transform (rigidbody kinematico).
            if (suspended)
                return;

            //ASHFALL: durante el knockback (explosion) la FISICA manda -> no pisamos la velocidad,
            //asi el empujon (arriba + atras) y la gravedad se sienten de verdad (vuelo + caida).
            if (knockbackTimer > 0f)
            {
                knockbackTimer -= Time.fixedDeltaTime;
                return;
            }

            #region Calculate Movement Velocity

            //Get Movement Input!
            Vector2 frameInput = playerCharacter.GetInputMovement();
            //Calculate local-space direction by using the player's input.
            var movement = new Vector3(frameInput.x, 0.0f, frameInput.y);
            
            //(ASHFALL) Velocidad: agachado < andar < sprint (la fuente unica decide el sprint).
            if (crouching)
                movement *= crouchSpeed;
            else if (SprintingEffective)
                movement *= speedRunning;
            else
                movement *= speedWalking;

            //World space velocity calculation. This allows us to add it to the rigidbody's velocity properly.
            movement = transform.TransformDirection(movement);

            #endregion
            
            //--- (ASHFALL) Velocidad final: control TOTAL en el piso, MOMENTUM en el aire ---
            Vector3 vel;
            if (grounded)
            {
                //En el piso el control es instantaneo (arcade, estilo Serious Sam), pero
                //PROYECTADO sobre el plano del piso: bajar una rampa ya no es "ir recto y
                //caer a saltitos" -> el movimiento sigue la superficie a velocidad real.
                vel = Vector3.ProjectOnPlane(movement, groundNormal);
                if (movement.sqrMagnitude > 0.01f && vel.sqrMagnitude > 0.0001f)
                    vel = vel.normalized * movement.magnitude;
                vel += Vector3.down * groundStick;   //pegado extra en transiciones de rampa

                TryStepUp(movement);                 //escalones bajos: se suben solos
            }
            else
            {
                //En el aire se CONSERVA el momentum del despegue (soltar W ya no corta el
                //arco del salto); el input solo CORRIGE el rumbo con aceleracion limitada.
                Vector3 hVel = new Vector3(Velocity.x, 0f, Velocity.z);
                if (movement.sqrMagnitude > 0.01f)
                    hVel = Vector3.MoveTowards(hVel, movement, airAcceleration * Time.fixedDeltaTime);
                vel = hVel;
                vel.y = Velocity.y;                  //la gravedad sigue su curso
            }

            //Salto (consume jump buffer + coyote). Pisa la Y que haya puesto la proyeccion.
            if (jumpQueued && (grounded || coyoteTimer > 0f))
            {
                vel.y = jumpForce;
                jumpQueued = false;
                grounded = false;                                  // este frame ya estas en el aire
                coyoteTimer = 0f;                                  // consumido (no doble salto)
                groundedSuppressTimer = JumpGroundedSuppressTime;  // colchon: el cast aun roza el piso
                Jumped?.Invoke();
            }

            //Dash (ASHFALL): durante el dash, velocidad fija en la direccion del dash.
            if (IsDashing)
                Velocity = new Vector3(dashDir.x * dashSpeed, vel.y, dashDir.z * dashSpeed);
            else
                Velocity = vel;
        }

        /// <summary>
        /// Plays Footstep Sounds. This code is slightly old, so may not be great, but it functions alright-y!
        /// </summary>
        private void PlayFootstepSounds()
        {
            //Check if we're moving on the ground. We don't need footsteps in the air.
            if (grounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f)
            {
                //(ASHFALL) Clip segun el sprint REAL, no el Shift: agotado o agachado = pasos
                //de caminar aunque mantengas Shift (antes sonaba carrera caminando).
                audioSource.clip = SprintingEffective ? audioClipRunning : audioClipWalking;
                //Play it!
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            //Pause it if we're doing something like flying, or not moving!
            else if (audioSource.isPlaying)
                audioSource.Pause();
        }

        #endregion
    }
}