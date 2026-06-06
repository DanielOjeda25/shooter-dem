using UnityEngine;
using UnityEngine.InputSystem; // Nuevo Input System de Unity 6

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

    [Header("Agacharse")]
    public float crouchHeight = 1.3f;          // altura del collider agachado
    public float crouchTransitionSpeed = 10f;  // suavizado de la altura

    private CharacterController controller;
    private float verticalVelocity;   // velocidad vertical acumulada (caida/salto)
    private bool isCrouching;
    private bool isSprinting;

    private float standHeight;        // altura "de pie" (la del collider al arrancar)
    private float standCenterY;

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

        // --- Sprint: Shift + avanzando + no agachado ---
        isSprinting = kb != null && kb.leftShiftKey.isPressed && input.y > 0.1f && !isCrouching;

        float speed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

        // --- Salto / gravedad ---
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // pequeno, para mantener pegado al suelo

        if (kb != null && kb.spaceKey.wasPressedThisFrame && controller.isGrounded && !isCrouching)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); // v = sqrt(2*g*h)

        verticalVelocity += gravity * Time.deltaTime;

        // --- Aplicar todo en un solo Move ---
        Vector3 velocity = move * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
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
