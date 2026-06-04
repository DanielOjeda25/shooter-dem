using UnityEngine;
using UnityEngine.InputSystem; // Nuevo Input System de Unity 6

// Movimiento en primera persona usando CharacterController.
// Va en el GameObject "Player".
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 6f;        // metros por segundo
    public float gravity = -9.81f;  // negativa = tira hacia abajo

    private CharacterController controller;
    private float verticalVelocity; // velocidad de caida acumulada

    void Awake()
    {
        // Cacheamos el componente una sola vez (mas eficiente que buscarlo cada frame)
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 1) Leer WASD del teclado
        Vector2 input = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed) input.x -= 1f;
        }

        // 2) Direccion relativa a hacia donde mira el Player
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        move = Vector3.ClampMagnitude(move, 1f); // la diagonal no debe ser mas rapida

        // 3) Gravedad
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // valor pequeno para mantener pegado al suelo
        verticalVelocity += gravity * Time.deltaTime;

        // 4) Aplicar movimiento (horizontal + vertical) en un solo Move
        Vector3 velocity = move * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime); // deltaTime = independiente de los FPS
    }
}
