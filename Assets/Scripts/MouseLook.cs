using UnityEngine;
using UnityEngine.InputSystem; // Nuevo Input System de Unity 6

// Mirar con el raton. Va en la "Main Camera" (hija del Player).
// Horizontal -> gira el cuerpo del Player.  Vertical -> inclina la camara.
public class MouseLook : MonoBehaviour
{
    [Header("Sensibilidad")]
    public float sensitivity = 0.1f;

    [Header("Referencias")]
    public Transform playerBody; // arrastra aqui el GameObject "Player"

    private float pitch = 0f; // angulo vertical acumulado (arriba/abajo)

    void Start()
    {
        // Bloquear el cursor en el centro y ocultarlo (tipico de un FPS)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 delta = mouse.delta.ReadValue() * sensitivity;

        // Horizontal: girar el cuerpo del Player sobre el eje Y
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * delta.x);

        // Vertical: inclinar solo la camara, con limite para no dar la voltereta
        pitch -= delta.y;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }
}
