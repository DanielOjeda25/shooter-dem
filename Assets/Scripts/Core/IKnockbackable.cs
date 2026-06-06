using UnityEngine;

// Contrato para cualquier cosa que pueda ser EMPUJADA al recibir un impacto
// (enemigos hoy; props/jugador a futuro). Igual idea que IDamageable: desacopla a
// quien empuja (Weapon, Projectile) del tipo concreto que recibe el empujon.
public interface IKnockbackable
{
    // direction: hacia donde empujar (se normaliza dentro). force: intensidad.
    void ApplyKnockback(Vector3 direction, float force);
}
