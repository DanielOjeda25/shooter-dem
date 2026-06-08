using UnityEngine;

namespace ShooterDem
{
// Audio del enemigo (data-driven en el prefab), con su propio AudioSource 3D: en una horda
// oyes DE DONDE viene cada enemigo. Reacciona a eventos de la IA y de la vida (no al reves:
// ellos solo emiten). Reutilizable por cualquier tipo de enemigo (melee, kamikaze, ranged, tanque).
[RequireComponent(typeof(AudioSource))]
public class EnemyAudio : MonoBehaviour
{
    [Header("Clips (arrays = variantes elegidas al azar)")]
    public AudioClip idleLoop;        // bucle mientras vive (3D: solo se oyen los cercanos)
    public AudioClip[] alertClips;    // al DETECTAR al jugador (evento EnemyAI.Aggroed)
    public AudioClip[] attackClips;   // al ATACAR (lo llama EnemyAI tras Execute; ranged = disparo)
    public AudioClip[] hurtClips;     // al recibir dano NO letal (Health.Damaged)
    public AudioClip[] deathClips;    // al MORIR (suena en un objeto pooleado que sobrevive)

    [Header("Muerte (audio que sobrevive al reciclaje)")]
    public GameObject deathSfxPrefab; // prefab con PooledSfx (SfxOneShot); null si no hay deathClips
    public float deathVolume = 1f;

    private AudioSource source;
    private EnemyAI ai;
    private Health health;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        ai = GetComponent<EnemyAI>();
        health = GetComponent<Health>();
    }

    // OnEnable/OnDisable (no Awake): asi tambien suscribe/reinicia al RECICLAR del pool.
    void OnEnable()
    {
        if (idleLoop != null)
        {
            source.clip = idleLoop;
            source.loop = true;
            source.Play();              // arranca el ambiente al aparecer (o revivir del pool)
        }
        if (ai != null) ai.Aggroed += OnAggro;
        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }
    }

    void OnDisable()
    {
        if (ai != null) ai.Aggroed -= OnAggro;
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }
        source.Stop();                  // corta el loop al volver al pool / morir
    }

    void OnAggro() => PlayRandom(alertClips);

    // La llama EnemyAI justo despues de Execute (sirve a cualquier tipo de ataque).
    public void PlayAttack() => PlayRandom(attackClips);

    // Quejido SOLO en golpe no letal (en el letal suena la muerte, no el quejido).
    void OnDamaged(int current, int max)
    {
        if (current > 0) PlayRandom(hurtClips);
    }

    // Muerte: el enemigo vuelve al pool (su AudioSource se cortaria), asi que el clip se
    // reproduce en un objeto INDEPENDIENTE pooleado que sobrevive (PooledSfx).
    void OnDied()
    {
        if (deathClips == null || deathClips.Length == 0) return;
        var clip = deathClips[Random.Range(0, deathClips.Length)];
        PooledSfx.Play(deathSfxPrefab, clip, transform.position, deathVolume);
    }

    // PlayOneShot mezcla el efecto SOBRE el loop sin cortarlo.
    void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        var clip = clips[Random.Range(0, clips.Length)];
        if (clip != null) source.PlayOneShot(clip);
    }
}
}
