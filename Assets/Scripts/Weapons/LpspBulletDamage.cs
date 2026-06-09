using UnityEngine;

namespace ShooterDem
{
    /// <summary>
    /// PUENTE (Camino A): el balín del Low Poly Shooter Pack daña a NUESTROS enemigos
    /// y genera el FEEDBACK de impacto en carne (sangre + sonido + mancha en el suelo).
    /// El proyectil del pack (script `Projectile`) solo maneja impactos en superficies por
    /// tag; este componente añade la parte de gore al golpear un `IDamageable` (enemigo).
    /// Se agrega al prefab del balín y convive con su `Projectile`.
    /// </summary>
    public class LpspBulletDamage : MonoBehaviour
    {
        // Bus estatico (como EnemyHealth.Killed): "una bala daño a un enemigo".
        // Lo escucha el HUD para el hitmarker (X + tic) sin cablear nada en el Inspector.
        public static event System.Action HitConfirmed;

        [Header("Daño")]
        public int damage = 25;

        [Header("Feedback de impacto en carne")]
        [Tooltip("Spray de sangre (p.ej. FleshImpact).")]
        public GameObject fleshImpactPrefab;
        public float fleshImpactLifetime = 1.2f;
        [Tooltip("Sonidos de carne (se elige uno al azar): flesh1-5.")]
        public AudioClip[] fleshSounds;
        [Tooltip("Prefab con PooledSfx para reproducir el sonido (SfxOneShot).")]
        public GameObject sfxPrefab;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Tooltip("Mancha de sangre persistente en el suelo bajo el impacto.")]
        public bool spawnFloorDecal = true;

        private void OnCollisionEnter(Collision collision)
        {
            // El colisionador golpeado puede estar en un hijo; buscamos hacia el padre.
            var damageable = collision.collider.GetComponentInParent<IDamageable>();
            if (damageable == null)
                return;   // no es enemigo: las superficies las maneja el Projectile del pack

            damageable.TakeDamage(damage);
            HitConfirmed?.Invoke();   // avisa al HUD: hitmarker (X + tic)

            // Punto/normal del impacto para orientar el efecto.
            var contact = collision.GetContact(0);
            Vector3 pos = contact.point;
            Vector3 normal = contact.normal;
            Quaternion rot = normal.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(normal)
                : Quaternion.identity;

            // 1) Spray de sangre.
            if (fleshImpactPrefab != null)
            {
                var fx = Instantiate(fleshImpactPrefab, pos, rot);
                Destroy(fx, fleshImpactLifetime);
            }

            // 2) Sonido de carne (pooleado, 3D desde el punto de impacto).
            if (fleshSounds != null && fleshSounds.Length > 0 && sfxPrefab != null)
            {
                var clip = fleshSounds[Random.Range(0, fleshSounds.Length)];
                if (clip != null) PooledSfx.Play(sfxPrefab, clip, pos, sfxVolume);
            }

            // 3) Mancha de sangre persistente en el suelo bajo el impacto.
            if (spawnFloorDecal && BloodDecalManager.Instance != null
                && Physics.Raycast(pos, Vector3.down, out var ground, 5f))
            {
                BloodDecalManager.Instance.Spawn(ground.point, ground.normal);
            }
        }
    }
}
