using System.Collections.Generic;
using UnityEngine;

namespace ShooterDem
{
// Proyectil con dano en AREA (bazooka/granada). Lo lanza Weapon (fireType=Projectile).
// Vuela en linea recta; al chocar con algo (o al agotar su vida) EXPLOTA: aplica dano
// a todos los IDamageable dentro de un radio, con caida segun la distancia al centro.
// Reutiliza IDamageable (mismo contrato que el raycast) -> enemigos, jugador a futuro, etc.
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public float maxLifetime = 5f;     // si no choca con nada, explota igual (failsafe)
    public GameObject explosionPrefab; // VFX + sonido reutilizable (Explosion)
    public float explosionLifetime = 2f; // segundos antes de reciclar el VFX de explosion
    public float explosionShake = 0.6f;  // sacudida de camara (escalada por distancia)

    private int damage;
    private int minDamage;
    private float radius;
    private float knockback;
    private LayerMask mask;
    private bool exploded;
    private Rigidbody rb;
    // Reutilizado entre explosiones (Clear en vez de new) -> sin basura para el GC.
    private readonly HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Lo llama Weapon justo despues de instanciarlo.
    public void Launch(Vector3 velocity, int dmg, int minDmg, float explosionRadius, float knockbackForce, LayerMask hitMask)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();  // por si Launch llega antes que Awake

        damage = dmg;
        minDamage = minDmg;
        radius = explosionRadius;
        knockback = knockbackForce;
        mask = hitMask;
        exploded = false;

        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // evita atravesar a alta velocidad
        rb.linearVelocity = velocity;

        CancelInvoke();
        Invoke(nameof(Explode), maxLifetime);
    }

    void OnCollisionEnter(Collision collision) => Explode();

    void Explode()
    {
        if (exploded) return;
        exploded = true;
        CancelInvoke();

        // Dano en area: cada IDamageable dentro del radio recibe dano con caida lineal
        // (mas dano en el centro, menos en el borde). El HashSet evita golpear dos veces
        // al mismo objeto si tiene varios colliders (se reutiliza con Clear).
        alreadyHit.Clear();
        foreach (var col in Physics.OverlapSphere(transform.position, radius, mask))
        {
            var dmgable = col.GetComponentInParent<IDamageable>();
            if (dmgable == null || !alreadyHit.Add(dmgable)) continue;

            float t = Mathf.Clamp01(Vector3.Distance(transform.position, col.transform.position) / radius);
            dmgable.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(damage, minDamage, t))));

            if (dmgable is PlayerHealth ph) ph.RegisterHit(transform.position);   // por si te pilla tu propia bazooka

            // Empuje radial desde el centro de la explosion (menos fuerte en el borde).
            if (knockback > 0f)
            {
                var kb = col.GetComponentInParent<IKnockbackable>();
                if (kb != null)
                    kb.ApplyKnockback(col.transform.position - transform.position, knockback * (1f - t));
            }
        }

        // Efecto de explosion (VFX + sonido) pooleado (fallback a Instantiate si no hay manager).
        if (explosionPrefab != null)
            PoolManager.SpawnTimed(explosionPrefab, transform.position, Quaternion.identity, explosionLifetime);

        CameraShake.AddAt(transform.position, explosionShake);

        PoolManager.Return(gameObject);   // vuelve al pool (o se destruye)
    }
}
}
