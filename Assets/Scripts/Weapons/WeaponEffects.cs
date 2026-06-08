using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShooterDem
{
// Efectos visuales del arma: fogonazo + humo al disparar, e impacto (chispas/escombros)
// y marca. Usa VFX del Particle Pack con POOLING (PrefabPool) para no generar basura (GC)
// en hordas.
//
// IMPORTANTE: el TAMANO y el COLOR de cada efecto viven en su PREFAB (variant con
// Scaling Mode = Hierarchy), NO en este codigo. Asi se ajustan en el editor con el preview
// de particulas, sin entrar a Play. Aqui solo instanciamos, reiniciamos y reciclamos.
[RequireComponent(typeof(Weapon))]
public class WeaponEffects : MonoBehaviour
{
    [Header("Disparo")]
    public GameObject muzzleFlashPrefab;   // fogonazo
    public GameObject muzzleSmokePrefab;   // humo del canon
    public Transform muzzlePoint;          // punta del arma (donde nacen)
    public float muzzleLifetime = 0.5f;
    public float muzzleSmokeLifetime = 1f;
    public float fireShake = 0.15f;        // sacudida de camara al disparar (0 = nada)

    [Header("Impacto")]
    public GameObject worldImpact;         // pared/suelo
    public GameObject fleshImpact;         // enemigo
    public GameObject impactMark;          // decal/crater (opcional; null = sin marca)
    public float impactVfxLifetime = 0.8f;
    public float impactMarkLifetime = 5f;

    private Weapon weapon;
    private PrefabPool muzzlePool, smokePool, worldPool, fleshPool, marksPool;
    private Transform poolRoot;
    // Buffer reutilizado para el rayo al suelo de la sangre (sin GC).
    private readonly RaycastHit[] downBuffer = new RaycastHit[8];

    void Awake()
    {
        weapon = GetComponent<Weapon>();
        poolRoot = new GameObject("WeaponEffectsPool").transform;

        if (muzzleFlashPrefab != null) muzzlePool = new PrefabPool(muzzleFlashPrefab, poolRoot);
        if (muzzleSmokePrefab != null) smokePool  = new PrefabPool(muzzleSmokePrefab, poolRoot);
        if (worldImpact != null) worldPool = new PrefabPool(worldImpact, poolRoot);
        if (fleshImpact != null) fleshPool = new PrefabPool(fleshImpact, poolRoot);
        if (impactMark != null)  marksPool = new PrefabPool(impactMark, poolRoot);
    }

    void OnEnable()  { weapon.Fired += HandleFired; weapon.Hit += HandleHit; }
    void OnDisable() { weapon.Fired -= HandleFired; weapon.Hit -= HandleHit; }

    void Start() { StartCoroutine(Prewarm()); }

    void HandleFired()
    {
        CameraShake.Add(fireShake);   // juice: leve sacudida al disparar
        if (muzzlePoint == null) return;
        // Fogonazo y humo nacen en la punta, en world space (no parentados, para que la
        // escala del arma no altere el tamano que fijaste en el prefab).
        SpawnWorld(muzzlePool, muzzlePoint.position, muzzlePoint.rotation, muzzleLifetime);
        SpawnWorld(smokePool,  muzzlePoint.position, muzzlePoint.rotation, muzzleSmokeLifetime);
    }

    void HandleHit(RaycastHit hit, bool hitDamageable)
    {
        if (hitDamageable)
        {
            // La sangre es un splat de SUELO: si lo dejamos en el cuerpo del enemigo
            // queda flotando. Lanzamos un rayo hacia abajo y lo posamos en el suelo
            // (saltando al propio enemigo), orientado segun la superficie.
            // 1) Marca de sangre PEGADA al cuerpo del enemigo (se mueve con el y se borra al
            //    reciclarlo del pool).
            var body = hit.collider.GetComponentInParent<BodyDecals>();
            if (body != null) body.Add(hit.point, hit.normal);

            // 2) Spray + mancha en el SUELO bajo el impacto: rayo hacia abajo, saltando al
            //    propio enemigo. El spray se posa TUMBADO en el suelo (no flota en el aire).
            int n = Physics.RaycastNonAlloc(hit.point + Vector3.up * 0.05f, Vector3.down,
                downBuffer, 8f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            Vector3 groundPos = Vector3.zero, groundNormal = Vector3.up;
            bool onGround = false;
            for (int i = 0; i < n; i++)
            {
                var dh = downBuffer[i];
                if (dh.collider.GetComponentInParent<IDamageable>() != null) continue;
                if (dh.distance < best)
                {
                    best = dh.distance;
                    groundPos = dh.point;
                    groundNormal = dh.normal;
                    onGround = true;
                }
            }
            if (onGround)
            {
                SpawnWorld(fleshPool, groundPos, Quaternion.FromToRotation(Vector3.up, groundNormal), impactVfxLifetime);
                if (BloodDecalManager.Instance != null)
                    BloodDecalManager.Instance.Spawn(groundPos, groundNormal);
            }
            return;
        }

        // Mundo (pared/suelo): efecto orientado a la normal + marca/decal opcional.
        Quaternion wrot = hit.normal != Vector3.zero
            ? Quaternion.LookRotation(hit.normal) : Quaternion.identity;
        SpawnWorld(worldPool, hit.point, wrot, impactVfxLifetime);

        // Agujero de bala PERSISTENTE en la superficie del mundo.
        if (BulletDecalManager.Instance != null)
            BulletDecalManager.Instance.Spawn(hit.point, hit.normal);

        if (marksPool != null)
        {
            Quaternion mrot = Quaternion.FromToRotation(Vector3.up, hit.normal);
            var mark = marksPool.Get(hit.point, mrot);
            mark.transform.SetParent(hit.collider.transform, true);
            StartCoroutine(ReturnAfter(marksPool, mark, impactMarkLifetime));
        }
    }

    // Saca un efecto del pool en world space, lo reinicia y lo recicla tras 'lifetime'.
    void SpawnWorld(PrefabPool pool, Vector3 pos, Quaternion rot, float lifetime)
    {
        if (pool == null) return;
        var fx = pool.Get(pos, rot);
        RestartParticles(fx);
        StartCoroutine(ReturnAfter(pool, fx, lifetime));
    }

    // Reinicia TODOS los ParticleSystem del efecto (los del pack son multi-sistema) y
    // fuerza stopAction=None para que NUNCA se autodestruyan (romperia el pool).
    void RestartParticles(GameObject go)
    {
        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.None;
        }
        foreach (var ps in systems) ps.Clear(false);
        foreach (var ps in systems) ps.Play(false);
    }

    // Calienta shaders/texturas del pack al cargar la partida: si no, el PRIMER uso de cada
    // efecto sale en cyan/magenta un instante (Unity compila el shader al renderizarlo).
    // Disparamos uno de cada, diminuto y frente a la camara, y los devolvemos al pool con
    // su escala original intacta.
    IEnumerator Prewarm()
    {
        var cam = Camera.main;
        if (cam == null) yield break;
        Vector3 p = cam.transform.position + cam.transform.forward * 2f;

        var primed = new List<PrefabPool>();
        var objs   = new List<GameObject>();
        var scales = new List<Vector3>();
        foreach (var pool in new[] { muzzlePool, smokePool, worldPool, fleshPool })
        {
            if (pool == null) continue;
            var fx = pool.Get(p, Quaternion.identity);
            scales.Add(fx.transform.localScale);          // recordar tamano real del prefab
            fx.transform.localScale *= 0.01f;             // casi invisible solo para el warmup
            RestartParticles(fx);
            primed.Add(pool);
            objs.Add(fx);
        }

        yield return null;   // 2 frames: se renderizan -> compila shaders / carga texturas
        yield return null;

        for (int i = 0; i < objs.Count; i++)
        {
            objs[i].transform.localScale = scales[i];     // restaurar antes de devolver
            primed[i].Return(objs[i]);
        }
    }

    IEnumerator ReturnAfter(PrefabPool pool, GameObject go, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        pool.Return(go);
    }
}
}
