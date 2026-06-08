using UnityEngine;

namespace ShooterDem
{
// Ataque a distancia: al estar en rango (lo gestiona EnemyAI) lanza un PROYECTIL hacia el
// objetivo. El proyectil es esquivable (viaja por el aire). Va en el prefab del enemigo
// ranged junto a EnemyAI (con keepDistance > 0) y EnemyHealth.
public class RangedAttack : EnemyAttack
{
    [Header("Disparo")]
    public GameObject projectilePrefab;   // EnemyProjectile
    public Transform muzzlePoint;         // punta del arma (origen de la bala); si null, se calcula
    public float projectileSpeed = 16f;
    public int damage = 8;
    public float muzzleHeight = 1.2f;     // altura de salida si NO hay muzzlePoint
    public float muzzleForward = 0.7f;    // adelante, para no nacer dentro del propio collider

    public override void Execute(Transform target)
    {
        if (projectilePrefab == null || target == null) return;

        // Origen: la punta del arma si esta asignada; si no, un punto calculado al frente.
        Vector3 origin = muzzlePoint != null
            ? muzzlePoint.position
            : transform.position + Vector3.up * muzzleHeight;

        Vector3 aim = target.position + Vector3.up * 1.0f;   // apunta al torso, no a los pies
        Vector3 dir = (aim - origin).normalized;
        if (muzzlePoint == null) origin += dir * muzzleForward;

        var go = PoolManager.Spawn(projectilePrefab, origin, Quaternion.LookRotation(dir));
        if (go == null) return;
        var proj = go.GetComponent<EnemyProjectile>();
        if (proj != null)
            proj.Launch(dir * projectileSpeed, Mathf.Max(1, Mathf.RoundToInt(damage * Difficulty.EnemyDamage)));
    }
}
}
