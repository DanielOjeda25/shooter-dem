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
            // Punto/normal del impacto para orientar el efecto.
            var contact = collision.GetContact(0);
            Vector3 pos = contact.point;
            Vector3 normal = contact.normal;

            // El colisionador golpeado puede estar en un hijo; buscamos hacia el padre.
            var damageable = collision.collider.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                // Superficie (muro/suelo): agujero de bala persistente (pool con tope).
                if (BulletDecalManager.Instance != null)
                    BulletDecalManager.Instance.Spawn(pos, normal);
                return;
            }

            damageable.TakeDamage(damage);
            HitConfirmed?.Invoke();   // avisa al HUD: hitmarker (X + tic)

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

            // 3) Mancha de sangre persistente en el SUELO bajo el impacto.
            // OJO: el rayo hacia abajo sale del punto de impacto (el pecho del enemigo),
            // asi que hay que IGNORAR los colliders del PROPIO enemigo (si no, la mancha
            // quedaba pegada al cuerpo y "flotando" en el aire al morir el bicho —
            // especialmente en el tanque, que es alto y ancho). Tambien exigimos que la
            // superficie sea horizontal (normal hacia arriba): es una mancha de SUELO.
            if (spawnFloorDecal && BloodDecalManager.Instance != null)
            {
                Transform enemyRoot = (damageable as Component)?.transform.root;
                var hits = Physics.RaycastAll(pos, Vector3.down, 6f);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var h in hits)
                {
                    if (enemyRoot != null && h.collider.transform.root == enemyRoot)
                        continue;                  // su propio cuerpo: seguir bajando
                    if (h.normal.y < 0.5f) break;  // pared/objeto vertical: no es suelo
                    BloodDecalManager.Instance.Spawn(h.point, h.normal);
                    break;
                }
            }
        }
    }
}
