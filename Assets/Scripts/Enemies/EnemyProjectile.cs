using UnityEngine;

namespace ShooterDem
{
// Proyectil de un enemigo a distancia: viaja recto y al tocar al jugador le hace dano.
// ATRAVIESA a otros enemigos (sin fuego amigo) y se destruye contra el mundo o al agotar
// su vida (failsafe). Lo lanza RangedAttack.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class EnemyProjectile : MonoBehaviour
{
    public float maxLifetime = 5f;   // si no choca con nada, se destruye igual

    private int damage;
    private Vector3 velocity;
    private bool consumed;

    // Lo llama RangedAttack justo despues de instanciarlo.
    public void Launch(Vector3 vel, int dmg)
    {
        velocity = vel;
        damage = dmg;
        consumed = false;

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;     // lo movemos a mano; solo queremos los triggers
        rb.useGravity = false;
        GetComponent<Collider>().isTrigger = true;

        CancelInvoke();
        Invoke(nameof(Expire), maxLifetime);   // failsafe: vuelve al pool si no choca
    }

    void Update()
    {
        transform.position += velocity * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (consumed) return;

        // Sin fuego amigo: ignora a cualquier enemigo (y a si mismo).
        if (other.GetComponentInParent<EnemyHealth>() != null) return;

        // Algo danable que NO es enemigo (el jugador) -> dano y se consume.
        var dmgable = other.GetComponentInParent<IDamageable>();
        if (dmgable != null)
        {
            consumed = true;
            dmgable.TakeDamage(damage);

            var ph = other.GetComponentInParent<PlayerHealth>();
            if (ph != null) ph.RegisterHit(transform.position);   // indicador direccional + shake

            Despawn();
            return;
        }

        // Geometria solida del mundo (pared/suelo) -> se destruye sin dano.
        if (!other.isTrigger)
        {
            consumed = true;
            Despawn();
        }
    }

    void Expire() => Despawn();

    // Vuelve al pool (o se destruye si no vino de un pool / no hay PoolManager).
    void Despawn()
    {
        CancelInvoke();
        PoolManager.Return(gameObject);
    }
}
}
