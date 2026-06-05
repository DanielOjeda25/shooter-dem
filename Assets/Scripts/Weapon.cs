using System;
using System.Collections;          // corrutinas (IEnumerator)
using UnityEngine;
using UnityEngine.InputSystem;     // Nuevo Input System de Unity 6

// Responsabilidad UNICA: leer el input de disparo/recarga, lanzar el raycast,
// aplicar dano y gestionar la municion. Los NUMEROS del arma (dano, falloff,
// cargador...) viven en un WeaponData (ScriptableObject), de modo que un ARSENAL
// se hace creando "fichas" distintas SIN escribir codigo nuevo. NO sabe de sonido,
// particulas ni recoil: emite EVENTOS y los componentes satelite reaccionan.
public class Weapon : MonoBehaviour
{
    [Header("Datos del arma")]
    public WeaponData data;           // ficha con dano/falloff/cargador/etc.

    [Header("Referencias / capas")]
    public Camera fpsCamera;          // desde donde sale el tiro (la Main Camera)
    public LayerMask hitMask = ~0;    // que capas puede golpear el rayo

    private int currentAmmo;          // balas que quedan ahora mismo
    private bool isReloading;         // true mientras esta recargando

    // "Ventanitas" de solo-lectura para el HUD.
    public int CurrentAmmo => currentAmmo;
    public bool IsReloading => isReloading;
    // El HUD lee esto; lo exponemos desde los datos para no tener que cambiar su codigo.
    public int magazineSize => data != null ? data.magazineSize : 0;

    // Eventos para los componentes de efectos (patron observador).
    public event Action Fired;                  // disparo efectivo (gasta bala)
    public event Action DryFired;               // clic sin municion
    public event Action ReloadStarted;          // empieza la recarga
    public event Action<RaycastHit, bool> Hit;  // (info del golpe, golpeoAlgoDanable)
    public event Action AmmoChanged;            // municion o estado de recarga cambio (HUD)

    void Awake()
    {
        // Si no arrastramos la camara en el Inspector, usamos la principal.
        if (fpsCamera == null)
            fpsCamera = Camera.main;

        if (data == null)
            Debug.LogError("Weapon: falta asignar un WeaponData en 'data'.", this);
    }

    void Start()
    {
        // Empezamos con el cargador lleno.
        currentAmmo = magazineSize;
        AmmoChanged?.Invoke();   // valor inicial para el HUD
    }

    void Update()
    {
        // Si el juego esta congelado (pausa o game over), no disparamos ni recargamos.
        if (Time.timeScale == 0f) return;
        // Mientras recarga, o sin datos de arma, ignoramos input.
        if (isReloading || data == null) return;

        var kb = Keyboard.current;
        // Recargar con R (solo si no esta ya lleno).
        if (kb != null && kb.rKey.wasPressedThisFrame && currentAmmo < magazineSize)
        {
            StartCoroutine(Reload());
            return;
        }

        var mouse = Mouse.current;
        if (mouse == null) return;

        // wasPressedThisFrame = un disparo por clic (semiautomatico).
        if (mouse.leftButton.wasPressedThisFrame)
            Shoot();
    }

    void Shoot()
    {
        // Sin balas: avisa y emite DryFired (el sonido lo pone WeaponAudio).
        if (currentAmmo <= 0)
        {
            Debug.Log("Click! Sin municion (pulsa R para recargar)");
            DryFired?.Invoke();
            return;
        }

        // Gastamos una bala y avisamos del disparo (muzzle, sonido, recoil reaccionan).
        currentAmmo--;
        Debug.Log($"Disparo. Balas: {currentAmmo}/{magazineSize}");
        Fired?.Invoke();
        AmmoChanged?.Invoke();

        // Rayo desde el centro de la camara hacia delante.
        Vector3 origin = fpsCamera.transform.position;
        Vector3 direction = fpsCamera.transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, data.range, hitMask))
        {
            Debug.Log($"Impacto en: {hit.collider.name} (a {hit.distance:F1} m)");

            // Si lo golpeado se puede danar (IDamageable), le aplicamos dano con
            // caida por distancia: a quemarropa pega data.damage, lejos baja a minDamage.
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(DamageForDistance(hit.distance));

            Debug.DrawLine(origin, hit.point, Color.red, 1f); // depuracion en Scene

            // Avisamos del impacto. El bool indica si golpeamos algo danable
            // (lo usa WeaponAudio para elegir sonido de carne vs pared).
            Hit?.Invoke(hit, damageable != null);
        }
        else
        {
            Debug.Log("Fallo (no golpeo nada)");
            Debug.DrawRay(origin, direction * data.range, Color.green, 1f);
        }
    }

    // Dano segun la distancia al impacto: completo hasta falloffStart, baja
    // linealmente hasta minDamage en falloffEnd, y se mantiene en minDamage mas alla.
    int DamageForDistance(float distance)
    {
        if (distance <= data.falloffStart) return data.damage;
        if (distance >= data.falloffEnd) return data.minDamage;

        float t = Mathf.InverseLerp(data.falloffStart, data.falloffEnd, distance);
        return Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(data.damage, data.minDamage, t)));
    }

    // Corrutina: espera reloadTime sin congelar el frame y rellena el cargador.
    IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Recargando...");
        ReloadStarted?.Invoke();
        AmmoChanged?.Invoke();   // el HUD muestra "RECARGANDO..."

        yield return new WaitForSeconds(data.reloadTime);

        currentAmmo = magazineSize;
        isReloading = false;
        Debug.Log($"Recargado. Balas: {currentAmmo}/{magazineSize}");
        AmmoChanged?.Invoke();   // el HUD vuelve a mostrar el numero
    }
}
