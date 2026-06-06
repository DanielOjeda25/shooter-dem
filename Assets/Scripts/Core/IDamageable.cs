// Contrato para cualquier cosa que pueda recibir dano: enemigos, el jugador y,
// a futuro, destructibles (barriles, props). Desacopla a quien INFLIGE dano
// (Weapon, EnemyAI) de los tipos concretos: solo les importa "esto se puede danar".
public interface IDamageable
{
    void TakeDamage(int amount);
}
