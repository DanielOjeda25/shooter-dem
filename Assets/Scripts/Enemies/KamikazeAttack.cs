using System.Collections.Generic;
using UnityEngine;

namespace ShooterDem
{
// Ataque kamikaze: EXPLOTA -> dano en area a todo IDamageable del radio (con caida por
// distancia) + knockback, y el propio enemigo MUERE.
//
// Explota en DOS casos: (a) al alcanzar al jugador (EnemyAI llama Execute), y (b) al MORIR
// por cualquier causa, p. ej. a tiros (escucha Health.Died). Como la explosion dana a los
// IDamageable cercanos, si matas uno pegado a otros kamikazes mueren y explotan -> CADENA.
// Un flag evita explotar dos veces.
[RequireComponent(typeof(EnemyHealth))]
public class KamikazeAttack : EnemyAttack
{
    [Header("Explosion")]
    public int damage = 40;
    public float radius = 3.5f;
    public float knockback = 6f;
    public LayerMask hitMask = ~0;
    public GameObject explosionPrefab;   // VFX + sonido reutilizable (Explosion)
    public float explosionLifetime = 2f; // segundos antes de reciclar el VFX
    public float explosionShake = 0.5f;  // sacudida de camara (escalada por distancia)

    private EnemyHealth self;
    private bool hasExploded;
    private readonly HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

    void Awake()
    {
        self = GetComponent<EnemyHealth>();
    }

    // OnEnable/OnDisable (no Awake) para que la suscripcion y el reset funcionen tambien
    // al RECICLAR el kamikaze desde el pool.
    void OnEnable()
    {
        hasExploded = false;
        if (self == null) self = GetComponent<EnemyHealth>();
        if (self != null) self.Died += Explode;   // morir (a tiros, etc.) -> explota
    }

    void OnDisable()
    {
        if (self != null) self.Died -= Explode;
    }

    // Lo llama EnemyAI al alcanzar al objetivo: explota y se mata (Kill -> Died -> Explode,
    // pero el flag ya impide repetir).
    public override void Execute(Transform target)
    {
        Explode();
        if (self != null) self.Kill();
    }

    // Dano en area + knockback + VFX. Idempotente (solo la primera vez).
    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        alreadyHit.Clear();
        foreach (var col in Physics.OverlapSphere(transform.position, radius, hitMask))
        {
            var dmgable = col.GetComponentInParent<IDamageable>();
            if (dmgable == null || ReferenceEquals(dmgable, self) || !alreadyHit.Add(dmgable))
                continue;

            float t = Mathf.Clamp01(Vector3.Distance(transform.position, col.transform.position) / radius);
            dmgable.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(damage * (1f - t) * Difficulty.EnemyDamage)));  // puede matar a otro kamikaze -> cadena

            if (dmgable is PlayerHealth ph) ph.RegisterHit(transform.position);   // indicador direccional + shake

            if (knockback > 0f)
            {
                var kb = col.GetComponentInParent<IKnockbackable>();
                if (kb != null)
                    kb.ApplyKnockback(col.transform.position - transform.position, knockback * (1f - t));
            }
        }

        if (explosionPrefab != null)
            PoolManager.SpawnTimed(explosionPrefab, transform.position, Quaternion.identity, explosionLifetime);

        CameraShake.AddAt(transform.position, explosionShake);
    }
}
}
