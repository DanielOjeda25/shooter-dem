using System;
using UnityEngine;

namespace ShooterDem
{
// Vida base reutilizable. Centraliza el patron currentHealth/isDead/TakeDamage/Die
// que antes estaba DUPLICADO casi identico en EnemyHealth y PlayerHealth.
// Expone el evento Damaged (patron observador) que consume el HUD. La muerte la
// gestiona cada subtipo en OnDeath (enemigos -> pool/eventos; jugador -> derrota).
public abstract class Health : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    public int maxHealth = 100;

    private int currentHealth;
    private bool isDead;

    // Ventanitas de solo-lectura.
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    // i-frames: si esta activo, ignora el dano (p. ej. durante el dash del jugador).
    public bool Invulnerable { get; set; }

    // (vidaActual, vidaMax) cada vez que recibe dano. Lo consume el HUD.
    public event Action<int, int> Damaged;

    // Se dispara al morir, ANTES de OnDeath (p. ej. el kamikaze explota al morir por balas).
    public event Action Died;

    // virtual: los subtipos pueden ampliar Awake (p. ej. anunciar su nacimiento)
    // pero deben llamar a base.Awake() para inicializar la vida.
    protected virtual void Awake()
    {
        ResetState();
    }

    // Reinicia vida y estado de muerte. Se usa al nacer y, sobre todo, al REUTILIZAR
    // un objeto del pool (Awake solo corre la primera vez en la vida del GameObject;
    // al reactivar uno reciclado hay que "revivirlo" a mano).
    protected void ResetState()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    // Como ResetState pero escalando la vida (para el escalado de dificultad por oleada).
    protected void ScaleAndResetHealth(float multiplier)
    {
        currentHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * multiplier));
        isDead = false;
    }

    public void TakeDamage(int amount)
    {
        if (isDead || Invulnerable) return; // muerto o invulnerable: ignora el golpe

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;   // nunca mostrar vida negativa
        Damaged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    // Mata al objeto al instante, sin pasar por TakeDamage (lo usa el kamikaze al explotar).
    public void Kill() => Die();

    // Camino UNICO de muerte: marca, avisa (Died) y delega el QUE pasa en OnDeath.
    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Died?.Invoke();
        OnDeath();
    }

    // Cada subtipo decide QUE pasa al morir (destruirse, avisar a las reglas...).
    protected abstract void OnDeath();
}
}
