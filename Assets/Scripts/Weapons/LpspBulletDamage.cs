using UnityEngine;

namespace ShooterDem
{
    /// <summary>
    /// PUENTE (Camino A): el balín del Low Poly Shooter Pack daña a NUESTROS enemigos.
    /// El proyectil del pack (script `Projectile`) solo maneja impactos por tag y no toca
    /// ningún sistema de vida. Este componente, en la colisión, busca un `IDamageable`
    /// en lo golpeado y le aplica daño. Se agrega al prefab del balín y convive con su
    /// `Projectile` (que se encarga de destruir el balín y los efectos de impacto).
    /// </summary>
    public class LpspBulletDamage : MonoBehaviour
    {
        [Tooltip("Daño que aplica el balín a un enemigo (IDamageable).")]
        public int damage = 25;

        private void OnCollisionEnter(Collision collision)
        {
            // El colisionador golpeado puede estar en un hijo; buscamos hacia el padre.
            var damageable = collision.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(damage);
        }
    }
}
