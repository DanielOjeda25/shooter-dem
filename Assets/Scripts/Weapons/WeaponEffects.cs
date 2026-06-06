using System.Collections;
using UnityEngine;

// Efectos visuales del arma: fogonazo al disparar y chispas + marca en el impacto.
// Escucha los eventos de Weapon. Va en el mismo GameObject que Weapon.
//
// Chispas y marcas usan POOLING (PrefabPool): en hordas, disparar sin parar haria
// Instantiate/Destroy constante -> basura (GC) y tirones. Aqui se reciclan.
[RequireComponent(typeof(Weapon))]
public class WeaponEffects : MonoBehaviour
{
    [Header("Disparo")]
    public ParticleSystem muzzleFlash;   // fogonazo en la punta (hijo del arma)

    [Header("Impacto")]
    public GameObject impactSparks;      // prefab de chispas (esfera de particulas)
    public GameObject impactMark;        // prefab de marca/decal
    public float sparksLifetime = 1f;    // segundos antes de reciclar las chispas
    public float impactLifetime = 5f;    // segundos antes de reciclar la marca

    private Weapon weapon;
    private PrefabPool sparksPool;
    private PrefabPool marksPool;
    private Transform poolRoot;          // contenedor de los efectos reciclados

    void Awake()
    {
        weapon = GetComponent<Weapon>();

        // Contenedor para que los efectos inactivos no ensucien la jerarquia.
        poolRoot = new GameObject("ImpactEffectsPool").transform;

        if (impactSparks != null) sparksPool = new PrefabPool(impactSparks, poolRoot);
        if (impactMark != null)   marksPool  = new PrefabPool(impactMark, poolRoot);
    }

    void OnEnable()  { weapon.Fired += HandleFired; weapon.Hit += HandleHit; }
    void OnDisable() { weapon.Fired -= HandleFired; weapon.Hit -= HandleHit; }

    void HandleFired()
    {
        if (muzzleFlash != null)
            muzzleFlash.Play();
    }

    void HandleHit(RaycastHit hit, bool hitDamageable)
    {
        // Chispas: las pedimos al pool, reiniciamos su particula y las reciclamos luego.
        if (sparksPool != null)
        {
            GameObject sparks = sparksPool.Get(hit.point, Quaternion.identity);
            RestartParticles(sparks);
            StartCoroutine(ReturnAfter(sparksPool, sparks, sparksLifetime));
        }

        // Marca/decal SOLO en geometria del mundo (paredes/suelo), no en enemigos:
        // un enemigo es danable y vuelve al pool, asi que pegarle la marca haria que
        // reapareciera sobre el al reciclarse. En el mundo la pegamos al objeto golpeado.
        if (!hitDamageable && marksPool != null)
        {
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
            GameObject mark = marksPool.Get(hit.point, rot);
            mark.transform.SetParent(hit.collider.transform, true);
            StartCoroutine(ReturnAfter(marksPool, mark, impactLifetime));
        }
    }

    // Reinicia el sistema de particulas al reutilizarlo. Ademas fuerza stopAction=None
    // para que NUNCA se autodestruya (eso romperia el pool al dejar refs muertas).
    void RestartParticles(GameObject go)
    {
        var ps = go.GetComponentInChildren<ParticleSystem>();
        if (ps == null) return;

        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.None;
        ps.Clear(true);
        ps.Play(true);
    }

    // Espera y devuelve el objeto al pool (en vez de Destroy).
    IEnumerator ReturnAfter(PrefabPool pool, GameObject go, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        pool.Return(go);
    }
}
