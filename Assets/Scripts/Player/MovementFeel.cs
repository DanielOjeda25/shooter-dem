using UnityEngine;

namespace ShooterDem
{
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

    [Header("Head-bob (andar/correr)")]
    public float walkBobSpeed = 9f;
    public float sprintBobSpeed = 13f;
    public float bobAmpY = 0.045f;   // vaiven vertical del ojo
    public float bobAmpX = 0.03f;    // balanceo lateral

    [Header("Aterrizaje")]
    public float landingDipScale = 0.04f;  // hundimiento por unidad de velocidad de caida
    public float maxLandingDip = 0.18f;    // tope del hundimiento

    [Header("Dash (punch de FOV)")]
    public float dashFovPunch = 12f;       // grados extra de FOV durante el dash
    public float fovLerpSpeed = 9f;

    private Vector3 holderRestPos;
    private Quaternion holderRestRot;
    private float eyeBase, baseX, baseZ;   // base de la camara (sin bob/dip)
    private float bobTimer, bobBlend, dip; // estado del head-bob y del dip de aterrizaje
    private Camera cam;                    // para el punch de FOV
    private float baseFov;

    void OnEnable()  { if (movement != null) movement.Landed += OnLanded; }
    void OnDisable() { if (movement != null) movement.Landed -= OnLanded; }

    void Start()
    {
        if (cameraTransform != null)
        {
            baseX = cameraTransform.localPosition.x;
            baseZ = cameraTransform.localPosition.z;
            eyeBase = cameraTransform.localPosition.y;
            cam = cameraTransform.GetComponent<Camera>();
            if (cam != null) baseFov = cam.fieldOfView;
        }
        else eyeBase = standEyeHeight;

        if (weaponHolder != null)
        {
            holderRestPos = weaponHolder.localPosition;
            holderRestRot = weaponHolder.localRotation;
        }
    }

    // Hundimiento de camara al aterrizar, escalado por la velocidad de caida (con tope).
    void OnLanded(float impactSpeed)
    {
        dip = -Mathf.Min(maxLandingDip, impactSpeed * landingDipScale);
    }

    void LateUpdate()
    {
        if (movement == null) return;
        float t = feelSpeed * Time.deltaTime;

        // --- Camara: ojo (agachado/sprint) + head-bob al andar + dip de aterrizaje ---
        if (cameraTransform != null)
        {
            float targetEye = (movement.IsCrouching ? crouchEyeHeight : standEyeHeight)
                              - (movement.IsSprinting ? sprintDip : 0f);
            eyeBase = Mathf.Lerp(eyeBase, targetEye, t);

            // Head-bob: solo al desplazarse por el suelo; entra/sale suave con bobBlend.
            bool moving = movement.IsMoving && movement.IsGrounded;
            bobBlend = Mathf.Lerp(bobBlend, moving ? 1f : 0f, t);
            if (moving)
                bobTimer += (movement.IsSprinting ? sprintBobSpeed : walkBobSpeed) * Time.deltaTime;
            float bobY = Mathf.Sin(bobTimer * 2f) * bobAmpY * bobBlend;  // sube/baja 2x por zancada
            float bobX = Mathf.Cos(bobTimer) * bobAmpX * bobBlend;       // balanceo lateral 1x

            dip = Mathf.Lerp(dip, 0f, t);   // el dip de aterrizaje vuelve a 0 suave

            cameraTransform.localPosition = new Vector3(baseX + bobX, eyeBase + bobY + dip, baseZ);
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

        // --- Punch de FOV al dashear (sensacion de velocidad) ---
        if (cam != null)
        {
            float targetFov = baseFov + (movement.IsDashing ? dashFovPunch : 0f);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, fovLerpSpeed * Time.deltaTime);
        }
    }
}
}
