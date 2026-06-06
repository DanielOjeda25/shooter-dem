using UnityEngine;

// Recoil procedural del arma. Escucha Weapon.Fired y aplica un "kick" que decae
// suave en LateUpdate. Va en el MISMO GameObject que Weapon (mueve su transform).
[RequireComponent(typeof(Weapon))]
public class WeaponRecoil : MonoBehaviour
{
    [Header("Recoil")]
    public float kickback = 0.06f;     // retroceso (metros, eje Z local)
    public float pitch = 0f;           // cabeceo al disparar (grados); 0 = solo retroceso
    public float returnSpeed = 10f;    // rapidez de vuelta al reposo

    private Weapon weapon;
    private Vector3 restPos;            // pose local de reposo
    private Quaternion restRot;
    // Offsets ACTUALES respecto al reposo; decaen a cero. Trabajar con offsets
    // (en vez de acumular sobre el transform vivo) evita que el recoil derive de lado.
    private Vector3 posOffset;
    private float pitchOffset;

    void Awake()
    {
        weapon = GetComponent<Weapon>();
    }

    void OnEnable()  { weapon.Fired += ApplyRecoil; }
    void OnDisable() { weapon.Fired -= ApplyRecoil; }

    void Start()
    {
        restPos = transform.localPosition;
        restRot = transform.localRotation;
    }

    void LateUpdate()
    {
        // En pausa (timeScale 0) deltaTime es 0, asi que no se mueve: correcto.
        float t = returnSpeed * Time.deltaTime;
        posOffset = Vector3.Lerp(posOffset, Vector3.zero, t);
        pitchOffset = Mathf.Lerp(pitchOffset, 0f, t);

        // Recomponemos SIEMPRE desde el reposo + offset: atras puro (Z) + cabeceo puro (X).
        transform.localPosition = restPos + posOffset;
        transform.localRotation = restRot * Quaternion.Euler(pitchOffset, 0f, 0f);
    }

    void ApplyRecoil()
    {
        posOffset.z -= kickback;
        pitchOffset -= pitch;
    }
}
