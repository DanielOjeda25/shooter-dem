using System.Collections;          // necesario para las corrutinas (IEnumerator)
using UnityEngine;
using UnityEngine.InputSystem; // Nuevo Input System de Unity 6

// Disparo por raycast. Va en el GameObject "Weapon" (hijo de la camara).
// Al hacer clic, lanza un rayo desde el centro de la camara hacia delante
// y nos dice que objeto golpea y donde.
// RequireComponent: si este script esta en un objeto sin AudioSource, Unity
// lo anade automaticamente. Asi siempre tenemos "altavoz" para los sonidos.
[RequireComponent(typeof(AudioSource))]
public class Weapon : MonoBehaviour
{
    [Header("Disparo")]
    public float range = 100f;        // alcance del rayo en metros
    public int damage = 25;           // dano por disparo
    public Camera fpsCamera;          // desde donde sale el tiro (la Main Camera)

    [Header("Que se puede golpear")]
    public LayerMask hitMask = ~0;    // ~0 = todas las capas (de momento, todo)

    [Header("Efecto de impacto")]
    public GameObject impactPrefab;   // marca que aparece donde pega el tiro
    public float impactLifetime = 5f; // segundos antes de que la marca se borre sola

    [Header("Municion")]
    public int magazineSize = 12;     // balas por cargador
    public float reloadTime = 1.5f;   // segundos que tarda la recarga

    [Header("Efectos")]
    // Fogonazo en la punta del arma. Lo arrastramos desde el Inspector
    // (el GameObject "MuzzleFlash", hijo del arma). Puede quedar null sin romper nada.
    public ParticleSystem muzzleFlash;

    // Chispas en el punto de impacto. Es un prefab (Assets/Prefabs/ImpactSparks):
    // instanciamos una copia en cada golpe y se autodestruye.
    public GameObject impactSparks;

    [Header("Sonido")]
    // Arrays = varias variantes; elegimos una al azar para que no suene repetitivo.
    public AudioClip[] fireClips;            // disparo (fire1, fire2)
    public AudioClip emptyClip;             // clic sin municion (empty)
    public AudioClip reloadClip;            // recarga (reload)
    public AudioClip[] concreteImpactClips; // golpe en pared/suelo (concrete1..4)
    public AudioClip[] fleshImpactClips;    // golpe en enemigo (flesh1..5)

    private AudioSource audioSource;        // el "altavoz" del arma (sonidos 2D)

    [Header("Recoil")]
    public float recoilKickback = 0.05f;    // cuanto retrocede el arma (metros, eje Z local)
    public float recoilPitch = 5f;          // cuanto sube el morro al disparar (grados)
    public float recoilReturnSpeed = 8f;    // que tan rapido vuelve a su sitio

    private Vector3 weaponRestPos;          // posicion local de reposo (sin disparar)
    private Quaternion weaponRestRot;       // rotacion local de reposo
    // Offsets ACTUALES respecto al reposo; decaen a cero cada frame. Trabajar con
    // offsets (en vez de acumular sobre el transform vivo) evita que el recoil
    // "derive" de lado con disparos rapidos: siempre es atras puro + cabeceo puro.
    private Vector3 recoilPosOffset;        // desplazamiento (z negativo = atras)
    private float recoilPitchOffset;        // cabeceo en grados (negativo = morro arriba)

    private int currentAmmo;          // balas que quedan ahora mismo
    private bool isReloading;         // true mientras esta recargando

    // "Ventanitas" de solo-lectura para el HUD.
    public int CurrentAmmo => currentAmmo;
    public bool IsReloading => isReloading;

    void Awake()
    {
        // Si no arrastramos la camara en el Inspector, usamos la principal.
        if (fpsCamera == null)
            fpsCamera = Camera.main;

        // Guardamos el AudioSource de este objeto (garantizado por RequireComponent).
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;   // que no suene solo al arrancar
    }

    void Start()
    {
        // Empezamos con el cargador lleno.
        currentAmmo = magazineSize;

        // Memorizamos la pose de reposo del arma para volver a ella tras el recoil.
        weaponRestPos = transform.localPosition;
        weaponRestRot = transform.localRotation;
    }

    void LateUpdate()
    {
        // Cada frame, los offsets del recoil decaen suavemente hacia cero.
        // En pausa (timeScale 0) deltaTime es 0, asi que no se mueve: correcto.
        float t = recoilReturnSpeed * Time.deltaTime;
        recoilPosOffset = Vector3.Lerp(recoilPosOffset, Vector3.zero, t);
        recoilPitchOffset = Mathf.Lerp(recoilPitchOffset, 0f, t);

        // Recomponemos el transform SIEMPRE desde la pose de reposo + el offset.
        // Asi el desplazamiento es atras puro (Z) y el giro es cabeceo puro (X).
        transform.localPosition = weaponRestPos + recoilPosOffset;
        transform.localRotation = weaponRestRot * Quaternion.Euler(recoilPitchOffset, 0f, 0f);
    }

    void Update()
    {
        // Si el juego esta congelado (pausa o game over), no disparamos ni recargamos.
        // (Evita que un clic en un boton del menu dispare el arma.)
        if (Time.timeScale == 0f) return;

        // Mientras recarga, ignoramos disparo y recarga (no se puede hacer nada).
        if (isReloading) return;

        var kb = Keyboard.current;
        // Recargar con R (solo si no esta ya lleno).
        if (kb != null && kb.rKey.wasPressedThisFrame && currentAmmo < magazineSize)
        {
            StartCoroutine(Reload());
            return;
        }

        var mouse = Mouse.current;
        if (mouse == null) return;

        // wasPressedThisFrame = se dispara UNA vez por clic (no mientras mantienes)
        if (mouse.leftButton.wasPressedThisFrame)
            Shoot();
    }

    void Shoot()
    {
        // Sin balas: no dispara, avisa y sugiere recargar.
        if (currentAmmo <= 0)
        {
            Debug.Log("Click! Sin municion (pulsa R para recargar)");
            if (emptyClip != null)
                audioSource.PlayOneShot(emptyClip);   // clic en seco
            return;
        }

        // Gastamos una bala.
        currentAmmo--;
        Debug.Log($"Disparo. Balas: {currentAmmo}/{magazineSize}");

        // Sonido de disparo: una variante al azar.
        PlayRandom2D(fireClips);

        // Recoil: damos el "kick" hacia atras y arriba (LateUpdate lo devuelve).
        ApplyRecoil();

        // Fogonazo: si hay Particle System asignado, lanzamos una rafaga.
        // Play() reinicia y reproduce el efecto desde cero en cada disparo.
        if (muzzleFlash != null)
            muzzleFlash.Play();

        // 1) Origen y direccion del rayo: centro de la camara, hacia delante
        Vector3 origin = fpsCamera.transform.position;
        Vector3 direction = fpsCamera.transform.forward;

        // 2) Lanzar el rayo. Si golpea algo dentro del alcance, hit guarda la info.
        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask))
        {
            // hit.collider = objeto golpeado, hit.point = punto exacto del impacto
            Debug.Log($"Impacto en: {hit.collider.name} (a {hit.distance:F1} m)");

            // Si lo golpeado se puede danar (implementa IDamageable: enemigos,
            // futuros destructibles...), le aplicamos dano. No nos importa su tipo.
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(damage);

            // Linea roja visible en la vista Scene durante 1 segundo (para depurar)
            Debug.DrawLine(origin, hit.point, Color.red, 1f);

            // Sonido de impacto en 2D (volumen pleno, se oye siempre, no depende
            // de la distancia). Si golpeamos algo danable (un enemigo) usamos
            // sonidos de carne; si no, de pared.
            PlayRandom2D(damageable != null ? fleshImpactClips : concreteImpactClips);

            // Chispas: instanciamos el prefab en el punto del golpe. Como las
            // particulas salen en esfera (todas direcciones), no hace falta
            // orientarlo: usamos Quaternion.identity. Se autodestruye en 1s.
            if (impactSparks != null)
            {
                GameObject sparks = Instantiate(impactSparks, hit.point, Quaternion.identity);
                Destroy(sparks, 1f);
            }

            // Marca de impacto: si hay prefab asignado, lo creamos en el punto del golpe.
            if (impactPrefab != null)
            {
                // hit.normal = direccion "hacia afuera" de la superficie golpeada.
                // El disco (cilindro) tiene su cara plana en el eje Y, asi que
                // alineamos su Y con la normal para que quede tumbado sobre la superficie.
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                GameObject mark = Instantiate(impactPrefab, hit.point, rotation);

                // Pegamos la marca al objeto golpeado: si ese objeto se mueve o se
                // destruye (un enemigo al morir), la marca le acompana en vez de
                // quedar flotando. El "true" conserva posicion/rotacion/tamano en el
                // mundo pese a la escala del padre (p. ej. el suelo esta escalado x5).
                mark.transform.SetParent(hit.collider.transform, true);

                // La borramos sola pasados unos segundos para no llenar la escena.
                Destroy(mark, impactLifetime);
            }
        }
        else
        {
            // No golpeo nada: linea verde hacia el alcance maximo
            Debug.Log("Fallo (no golpeo nada)");
            Debug.DrawRay(origin, direction * range, Color.green, 1f);
        }
    }

    // Corrutina: se ejecuta "a trozos" en el tiempo. El yield pausa aqui
    // sin congelar el juego y reanuda cuando pasan los segundos indicados.
    IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Recargando...");

        if (reloadClip != null)
            audioSource.PlayOneShot(reloadClip);     // suena al empezar la recarga

        yield return new WaitForSeconds(reloadTime); // espera sin bloquear el frame

        currentAmmo = magazineSize;                  // cargador lleno otra vez
        isReloading = false;
        Debug.Log($"Recargado. Balas: {currentAmmo}/{magazineSize}");
    }

    // Reproduce una variante al azar del array por el altavoz del arma (sonido 2D).
    // PlayOneShot permite que varios disparos se solapen sin cortarse.
    void PlayRandom2D(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    // Aplica el "golpe" del recoil sumando a los offsets: atras (-Z) y morro
    // arriba (-X grados). LateUpdate los hace decaer a cero (vuelta suave).
    void ApplyRecoil()
    {
        recoilPosOffset.z -= recoilKickback;
        recoilPitchOffset -= recoilPitch;
    }
}
