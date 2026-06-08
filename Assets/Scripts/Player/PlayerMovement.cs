using UnityEngine;
using UnityEngine.InputSystem; // Nuevo Input System de Unity 6

namespace ShooterDem
{
// Movimiento en primera persona usando el CharacterController NATIVO de Unity
// (maneja colision, pendientes y step-offset por nosotros). Anade andar, esprintar
// (Shift), agacharse (Ctrl) y saltar (Espacio). Expone su estado para el "feel"
// visual del arma/camara (MovementFeel). Va en el GameObject "Player".
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Velocidades")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 3f;

    [Header("Salto / gravedad")]
    public float jumpHeight = 1.2f;   // metros que sube el salto
    public float gravity = -9.81f;    // negativa = tira hacia abajo
    public float coyoteTime = 0.12f;  // margen para saltar tras salir de un borde
    public float jumpBuffer = 0.12f;  // margen para registrar el salto justo antes de aterrizar

    [Header("Dash / esquiva (cargas, estilo Doom)")]
    public float dashSpeed = 22f;       // velocidad del impulso
    public float dashDuration = 0.18f;  // cuanto dura el impulso
    public int maxDashCharges = 2;      // dashes seguidos disponibles
    public float dashRecharge = 2f;     // segundos para recuperar 1 carga

    [Header("Stamina del sprint")]
    public float maxStamina = 100f;
    public float sprintDrain = 34f;        // mas alto: el sprint dura menos
    public float staminaRegen = 28f;       // recuperacion por segundo
    public float staminaRegenDelay = 0.7f; // respiro tras el ultimo uso antes de regenerar

    [Header("Agacharse")]
    public float crouchHeight = 1.3f;          // altura del collider agachado
    public float crouchTransitionSpeed = 10f;  // suavizado de la altura

    private CharacterController controller;
    private float verticalVelocity;   // velocidad vertical acumulada (caida/salto)
    private bool isCrouching;
    private bool isSprinting;

    private float standHeight;        // altura "de pie" (la del collider al arrancar)
    private float standCenterY;
    private float coyoteTimer;        // cuenta atras del coyote time
    private float jumpBufferTimer;    // cuenta atras del jump buffer
    private bool wasGrounded;         // para detectar el frame de aterrizaje
    private float dashTimer;          // tiempo restante del dash en curso
    private float dashCharges;        // 0..maxDashCharges (recarga continua)
    private Vector3 dashDir;          // direccion del dash actual
    private float stamina;
    private float lastStaminaUse;
    private bool exhausted;           // true al agotar stamina; se limpia al recuperar 30%

    public bool IsDashing => dashTimer > 0f;
    public float Stamina01 => maxStamina > 0f ? stamina / maxStamina : 0f;
    public float DashCharges => dashCharges;   // incluye la fraccion de la carga en recarga
    public int MaxDashCharges => maxDashCharges;

    // Se dispara al aterrizar; el float es la velocidad de caida (para el dip de camara).
    public event System.Action<float> Landed;

    // Estado expuesto para el feel visual y el audio (pasos).
    public bool IsSprinting => isSprinting;
    public bool IsCrouching => isCrouching;
    public bool IsGrounded => controller.isGrounded;
    // True si nos desplazamos en horizontal (ignora la caida vertical).
    public bool IsMoving =>
        new Vector3(controller.velocity.x, 0f, controller.velocity.z).sqrMagnitude > 0.5f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        standHeight = controller.height;       // tomamos la altura actual como "de pie"
        standCenterY = controller.center.y;
        stamina = maxStamina;
        dashCharges = maxDashCharges;
    }

    void Update()
    {
        // En pausa/game over (timeScale 0) no nos movemos.
        if (Time.timeScale == 0f) return;

        var kb = Keyboard.current;

        // --- Agacharse (con comprobacion de techo al levantarse) ---
        bool crouchHeld = kb != null && kb.leftCtrlKey.isPressed;
        UpdateCrouch(crouchHeld);

        // --- Direccion de movimiento (WASD relativo a la mirada) ---
        Vector2 input = Vector2.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed) input.x -= 1f;
        }
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        move = Vector3.ClampMagnitude(move, 1f); // la diagonal no debe ir mas rapida

        // --- Sprint: Shift + avanzando + no agachado, y CON stamina ---
        // Si la agotas, quedas "exhausted" hasta recuperar el 30% (evita parpadeo cerca de 0).
        if (stamina <= 0f) exhausted = true;
        else if (exhausted && stamina >= maxStamina * 0.3f) exhausted = false;
        bool wantsSprint = kb != null && kb.leftShiftKey.isPressed && input.y > 0.1f && !isCrouching;
        isSprinting = wantsSprint && !exhausted;

        float speed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

        // --- Salto / gravedad (con coyote time + jump buffer) ---
        bool grounded = controller.isGrounded;

        // Aterrizaje: al pasar de aire a suelo avisamos con la velocidad de caida
        // (para el dip de camara). ANTES de resetear verticalVelocity.
        if (grounded && !wasGrounded && -verticalVelocity > 0.1f)
            Landed?.Invoke(-verticalVelocity);
        wasGrounded = grounded;

        // Coyote: en suelo se recarga; en el aire se gasta (deja saltar un pelin tarde).
        coyoteTimer = grounded ? coyoteTime : coyoteTimer - Time.deltaTime;
        // Buffer: al pulsar salto se abre la ventana; si no, se cierra (deja pulsar un pelin pronto).
        if (kb != null && kb.spaceKey.wasPressedThisFrame) jumpBufferTimer = jumpBuffer;
        else jumpBufferTimer -= Time.deltaTime;

        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // pequeno, para mantener pegado al suelo

        // Salta si hay salto en cola Y aun queda coyote, y no estamos agachados.
        if (jumpBufferTimer > 0f && coyoteTimer > 0f && !isCrouching)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); // v = sqrt(2*g*h)
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;   // consumido (no doble salto)
        }

        verticalVelocity += gravity * Time.deltaTime;

        // --- Dash / esquiva (Alt izq): cargas estilo Doom (cada dash gasta 1, se recargan) ---
        if (dashCharges < maxDashCharges) dashCharges += Time.deltaTime / dashRecharge;
        dashCharges = Mathf.Min(dashCharges, maxDashCharges);

        if (kb != null && kb.leftAltKey.wasPressedThisFrame && dashCharges >= 1f && !isCrouching)
        {
            // Hacia donde te mueves; si estas quieto, hacia donde miras.
            dashDir = move.sqrMagnitude > 0.01f ? move.normalized : transform.forward;
            dashDir.y = 0f;
            dashDir = dashDir.normalized;
            dashTimer = dashDuration;
            dashCharges -= 1f;   // gasta una carga
        }

        // Durante el dash, la horizontal se sustituye por el impulso (ignora walk/sprint).
        Vector3 horizontal;
        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
            horizontal = dashDir * dashSpeed;
        }
        else
        {
            horizontal = move * speed;
        }

        // --- Aplicar todo en un solo Move (horizontal + vertical) ---
        Vector3 velocity = horizontal + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // --- Stamina: el sprint la gasta; se regenera tras un respiro sin usarla ---
        if (isSprinting)
        {
            stamina -= sprintDrain * Time.deltaTime;
            lastStaminaUse = Time.time;
        }
        else if (Time.time >= lastStaminaUse + staminaRegenDelay)
        {
            stamina += staminaRegen * Time.deltaTime;
        }
        stamina = Mathf.Clamp(stamina, 0f, maxStamina);
    }

    void UpdateCrouch(bool crouchHeld)
    {
        // Si quiere levantarse pero hay techo, sigue agachado.
        if (isCrouching && !crouchHeld && !HasHeadroom())
            crouchHeld = true;

        isCrouching = crouchHeld;

        // Interpolamos la altura del collider y recolocamos el centro para que los
        // pies sigan en el suelo (base fija): centro.y = standCenterY - (delta/2).
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        float h = Mathf.MoveTowards(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        controller.height = h;
        controller.center = new Vector3(
            controller.center.x,
            standCenterY - (standHeight - h) / 2f,
            controller.center.z);
    }

    bool HasHeadroom()
    {
        // Rayo hacia arriba desde la cabeza; si golpea algo, hay techo y no podemos
        // ponernos de pie.
        Vector3 top = transform.position + controller.center + Vector3.up * (controller.height / 2f);
        float checkDist = (standHeight - crouchHeight) + 0.1f;
        return !Physics.Raycast(top, Vector3.up, checkDist);
    }
}
}
