using UnityEngine;

namespace ShooterDem
{
// Reproductor de un clip "fire-and-forget" que SOBREVIVE a quien lo lanzo. Caso tipico:
// el sonido de MUERTE de un enemigo que vuelve al pool en el mismo instante (su propio
// AudioSource se cortaria). Se saca del PoolManager, suena en su posicion (3D) y vuelve
// SOLO al pool cuando el clip termina. Mismo patron que la explosion, pero con clip variable.
[RequireComponent(typeof(AudioSource))]
public class PooledSfx : MonoBehaviour
{
    private AudioSource src;

    void Awake() => src = GetComponent<AudioSource>();

    // Lanza 'clip' en 'pos' usando una instancia pooleada de 'prefab' (que lleva un PooledSfx).
    // Estatico: el que lo llama (p. ej. EnemyAudio) puede estar a punto de desactivarse.
    public static void Play(GameObject prefab, AudioClip clip, Vector3 pos, float volume = 1f)
    {
        if (prefab == null || clip == null) return;
        var go = PoolManager.Spawn(prefab, pos, Quaternion.identity);
        var sfx = go != null ? go.GetComponent<PooledSfx>() : null;
        if (sfx != null) sfx.PlayNow(clip, volume);
    }

    void PlayNow(AudioClip clip, float volume)
    {
        src.clip = clip;
        src.volume = volume;
        src.Play();
        CancelInvoke();                                  // por si se reutiliza del pool antes de tiempo
        Invoke(nameof(ReturnToPool), clip.length + 0.1f);
    }

    void ReturnToPool() => PoolManager.Return(gameObject);
}
}
