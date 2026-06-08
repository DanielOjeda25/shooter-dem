using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShooterDem
{
// Pool CENTRAL reutilizable por prefab. Cualquier sistema puede pedir/devolver instancias
// sin tener su propio pool (proyectiles, explosiones...). Evita el Instantiate/Destroy
// masivo en hordas (basura para el GC -> tirones).
//
// FALLBACK: si NO hay un PoolManager en la escena, los helpers estaticos hacen
// Instantiate/Destroy normal (sin pooling, pero sin romperse). Asi el codigo que lo usa
// funciona igual con o sin manager; para activar el pooling basta poner un GameObject
// vacio "PoolManager" en la escena.
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    private readonly Dictionary<GameObject, PrefabPool> poolByPrefab = new Dictionary<GameObject, PrefabPool>();
    private readonly Dictionary<GameObject, PrefabPool> poolByInstance = new Dictionary<GameObject, PrefabPool>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ---------- API estatica (con fallback si no hay manager) ----------

    // Saca una instancia del pool del prefab (o la instancia si no hay manager).
    public static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;
        if (Instance != null) return Instance.GetTracked(prefab, pos, rot);
        return Instantiate(prefab, pos, rot);   // fallback sin pool
    }

    // Devuelve una instancia a su pool (o la destruye si no vino del pool / no hay manager).
    public static void Return(GameObject instance)
    {
        if (instance == null) return;
        if (Instance != null && Instance.ReturnTracked(instance)) return;
        Destroy(instance);
    }

    // Fire-and-forget para VFX (explosiones): saca, reinicia particulas y devuelve tras 'life'.
    public static void SpawnTimed(GameObject prefab, Vector3 pos, Quaternion rot, float life)
    {
        if (prefab == null) return;
        if (Instance != null) { Instance.SpawnTimedInternal(prefab, pos, rot, life); return; }
        Instantiate(prefab, pos, rot);   // fallback: el prefab se autodestruye (AutoDestroy)
    }

    // ---------- Implementacion ----------

    GameObject GetTracked(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        var pool = PoolFor(prefab);
        var go = pool.Get(pos, rot);
        poolByInstance[go] = pool;   // recordamos su pool para poder devolverla luego
        return go;
    }

    bool ReturnTracked(GameObject instance)
    {
        if (!poolByInstance.TryGetValue(instance, out var pool)) return false;
        pool.Return(instance);
        return true;
    }

    void SpawnTimedInternal(GameObject prefab, Vector3 pos, Quaternion rot, float life)
    {
        var pool = PoolFor(prefab);
        var go = pool.Get(pos, rot);
        RestartEffects(go);
        StartCoroutine(ReturnAfter(pool, go, life));
    }

    PrefabPool PoolFor(GameObject prefab)
    {
        if (!poolByPrefab.TryGetValue(prefab, out var pool))
        {
            pool = new PrefabPool(prefab, transform);
            poolByPrefab[prefab] = pool;
        }
        return pool;
    }

    IEnumerator ReturnAfter(PrefabPool pool, GameObject go, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        pool.Return(go);   // null-safe dentro de PrefabPool
    }

    // Reinicia el efecto al sacarlo del pool: ParticleSystem y AudioSource. Ninguno de los
    // dos se redispara solo al REACTIVAR un objeto reciclado (el Awake/playOnAwake ya paso),
    // asi que hay que relanzarlos a mano. En las particulas ademas fuerza stopAction=None
    // para que NUNCA se autodestruyan (eso romperia el pool).
    static void RestartEffects(GameObject go)
    {
        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            var main = systems[i].main;
            main.stopAction = ParticleSystemStopAction.None;
        }
        for (int i = 0; i < systems.Length; i++) systems[i].Clear(false);
        for (int i = 0; i < systems.Length; i++) systems[i].Play(false);

        // playOnAwake no re-suena al reactivar desde el pool: lo reproducimos a mano.
        var sources = go.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
            if (sources[i].playOnAwake && sources[i].clip != null) sources[i].Play();
    }
}
}
