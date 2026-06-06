using System.Collections.Generic;
using UnityEngine;

// Proyectil con dano en AREA (bazooka/granada). Lo lanza Weapon (fireType=Projectile).
// Vuela en linea recta; al chocar con algo (o al agotar su vida) EXPLOTA: aplica dano
// a todos los IDamageable dentro de un radio, con caida segun la distancia al centro.
// Reutiliza IDamageable (mismo contrato que el raycast) -> enemigos, jugador a futuro, etc.
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public float maxLifetime = 5f;     // si no choca con nada, explota igual (failsafe)
    public AudioClip explosionClip;    // sonido al explotar (lo pone el prefab)

    private int damage;
    private int minDamage;
    private float radius;
    private float knockback;
    private LayerMask mask;
    private bool exploded;
    private Rigidbody rb;

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

        Invoke(nameof(Explode), maxLifetime);
    }

    void OnCollisionEnter(Collision collision) => Explode();

    void Explode()
    {
        if (exploded) return;
        exploded = true;
        CancelInvoke();

        // Dano en area: cada IDamageable dentro del radio recibe dano con caida lineal
        // (mas dano en el centro de la explosion, menos en el borde). Usamos un HashSet
        // para no golpear dos veces al mismo objeto si tiene varios colliders.
        var alreadyHit = new HashSet<IDamageable>();
        foreach (var col in Physics.OverlapSphere(transform.position, radius, mask))
        {
            var dmgable = col.GetComponentInParent<IDamageable>();
            if (dmgable == null || !alreadyHit.Add(dmgable)) continue;

            float t = Mathf.Clamp01(Vector3.Distance(transform.position, col.transform.position) / radius);
            dmgable.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(damage, minDamage, t))));

            // Empuje radial desde el centro de la explosion (menos fuerte en el borde).
            if (knockback > 0f)
            {
                var kb = col.GetComponentInParent<IKnockbackable>();
                if (kb != null)
                    kb.ApplyKnockback(col.transform.position - transform.position, knockback * (1f - t));
            }
        }

        // Sonido de explosion en el punto (PlayClipAtPoint crea un altavoz temporal
        // que sobrevive al Destroy de este proyectil).
        if (explosionClip != null)
            AudioSource.PlayClipAtPoint(explosionClip, transform.position);

        // De momento se destruye; el efecto visual de explosion vendra luego.
        Destroy(gameObject);
    }
}
