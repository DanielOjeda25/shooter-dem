using UnityEngine;

// Reacciones VISUALES al estado de movimiento (game feel). Lee PlayerMovement y:
//  - baja la camara (el "ojo") al agacharse, y un pelin al esprintar (hundimiento),
//  - repliega/baja el arma (su "holder") al esprintar y la reacomoda al parar.
// Cada efecto vive en su propio transform para no pelearse con otros sistemas:
//  - la CAMARA (posicion del ojo): MouseLook solo toca su rotacion, aqui su Y.
//  - el WEAPON HOLDER (padre del arma): el recoil mueve el arma (hijo), aqui el padre.
public class MovementFeel : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerMovement movement;
    public Transform cameraTransform;   // Main Camera (altura del ojo)
    public Transform weaponHolder;      // padre del arma

    [Header("Altura de camara (ojo)")]
    public float standEyeHeight = 0.6f;
    public float crouchEyeHeight = 0.2f;
    public float sprintDip = 0.06f;     // hundimiento extra al esprintar

    [Header("Pose del arma al esprintar")]
    public Vector3 sprintWeaponOffset = new Vector3(0f, -0.08f, -0.12f); // baja y repliega
    public Vector3 sprintWeaponTilt = new Vector3(10f, -5f, 0f);         // inclinacion (grados)

    [Header("Suavizado")]
    public float feelSpeed = 10f;

    private Vector3 holderRestPos;
    private Quaternion holderRestRot;

    void Start()
    {
        if (weaponHolder != null)
        {
            holderRestPos = weaponHolder.localPosition;
            holderRestRot = weaponHolder.localRotation;
        }
    }

    void LateUpdate()
    {
        if (movement == null) return;
        float t = feelSpeed * Time.deltaTime;

        // --- Camara: altura del ojo segun agachado + hundimiento por sprint ---
        if (cameraTransform != null)
        {
            float targetEye = (movement.IsCrouching ? crouchEyeHeight : standEyeHeight)
                              - (movement.IsSprinting ? sprintDip : 0f);
            Vector3 p = cameraTransform.localPosition;
            p.y = Mathf.Lerp(p.y, targetEye, t);
            cameraTransform.localPosition = p;
        }

        // --- Arma: pose de sprint (repliegue) o reposo ---
        if (weaponHolder != null)
        {
            bool sprint = movement.IsSprinting;
            Vector3 targetPos = holderRestPos + (sprint ? sprintWeaponOffset : Vector3.zero);
            Quaternion targetRot = holderRestRot * Quaternion.Euler(sprint ? sprintWeaponTilt : Vector3.zero);
            weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, targetPos, t);
            weaponHolder.localRotation = Quaternion.Slerp(weaponHolder.localRotation, targetRot, t);
        }
    }
}
