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
        }

        //ASHFALL: grounded DETERMINISTICO (reemplaza el OnCollisionStay original del pack).
        //El sistema por contactos fallaba dos veces: (1) rozar PAREDES contaba como piso;
        //(2) saltar rozando una RAMPA (normal hacia arriba = "piso legitimo") mantenia
        //grounded=true durante TODO el ascenso -> stamina drenando y bob "corriendo" en el
        //aire (confirmado con instrumentacion: 'Ramp_SO' vy=+2.2 grounded=True).
        //Regla nueva: piso = spherecast ANGOSTO bajo los PIES + NO estar subiendo.
        private bool CheckGrounded()
        {
            if (rigidBody.linearVelocity.y > 0.5f)
                return false;                       // subiendo en un salto: jamas es piso

            Bounds b = capsule.bounds;
            float r = capsule.radius * 0.5f;        // angosto: los roces laterales no cuentan
            float dist = b.extents.y - r + 0.08f;   // la esfera llega hasta apenas bajo los pies
            return Physics.SphereCast(b.center, r, Vector3.down, out RaycastHit hit, dist,
                                      ~(1 << gameObject.layer), QueryTriggerInteraction.Ignore)
                   && hit.normal.y > 0.6f;          // y la superficie debe ser horizontal
        }
			
        protected override void FixedUpdate()
        {
            //ASHFALL: piso calculado cada paso de fisica (ya no depende de colisiones).
            grounded = CheckGrounded();

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

            //Play Sounds!
            PlayFootstepSounds();

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

            //Dash (ASHFALL): Alt izquierdo, si hay stamina y no esta dasheando ya.
            var kb = Keyboard.current;
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
            bool sprintingNow = playerCharacter.IsRunning() && !IsDashing && grounded
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
            
            //Running speed calculation. (ASHFALL) Solo esprinta si queda stamina.
            if(playerCharacter.IsRunning() && stamina > 0f)
                movement *= speedRunning;
            else
            {
                //Multiply by the normal walking speed.
                movement *= speedWalking;
            }

            //World space velocity calculation. This allows us to add it to the rigidbody's velocity properly.
            movement = transform.TransformDirection(movement);

            #endregion
            
            //Update Velocity. (ASHFALL) Preservamos la Y para que funcione la gravedad/salto
            //(antes la anulaba a 0 -> el player flotaba pegado al piso, sin saltar ni caer).
            float yVel = rigidBody.linearVelocity.y;
            if (jumpQueued && grounded)
            {
                yVel = jumpForce;
                jumpQueued = false;
                Jumped?.Invoke();
            }
            //Dash (ASHFALL): durante el dash, velocidad fija en la direccion del dash.
            if (IsDashing)
                Velocity = new Vector3(dashDir.x * dashSpeed, yVel, dashDir.z * dashSpeed);
            else
                Velocity = new Vector3(movement.x, yVel, movement.z);
        }

        /// <summary>
        /// Plays Footstep Sounds. This code is slightly old, so may not be great, but it functions alright-y!
        /// </summary>
        private void PlayFootstepSounds()
        {
            //Check if we're moving on the ground. We don't need footsteps in the air.
            if (grounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f)
            {
                //Select the correct audio clip to play.
                audioSource.clip = playerCharacter.IsRunning() ? audioClipRunning : audioClipWalking;
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