using UnityEngine;

namespace ShooterDem
{
// Ataque cuerpo a cuerpo: al estar en rango, resta vida al objetivo (via IDamageable).
// Es el comportamiento del enemigo-capsula actual. Va en el prefab del Enemy junto a EnemyAI.
public class MeleeAttack : EnemyAttack
{
    [Header("Melee")]
    public int damage = 10;

    public override void Execute(Transform target)
    {
        var damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(damage * Difficulty.EnemyDamage)));

        // Si golpeamos al jugador, le decimos DESDE DONDE (indicador direccional + shake).
        var ph = target.GetComponent<PlayerHealth>();
        if (ph != null) ph.RegisterHit(transform.position);
    }
}
}
