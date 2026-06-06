using System;
using UnityEngine;

// Vida base reutilizable. Centraliza el patron currentHealth/isDead/TakeDamage/Die
// que antes estaba DUPLICADO casi identico en EnemyHealth y PlayerHealth.
// Expone eventos (patron observador) para que otros reaccionen sin que Health
// tenga que conocerlos: el HUD escucha Damaged, las reglas escuchan Died, etc.
public abstract class Health : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    public int maxHealth = 100;

    private int currentHealth;
    private bool isDead;

    // Ventanitas de solo-lectura.
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    // (vidaActual, vidaMax) cada vez que recibe dano. Lo consume el HUD.
    public event Action<int, int> Damaged;
    // Se dispara UNA vez, en el frame en que muere.
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
        if (isDead) return; // ya muerto: ignora golpes extra

        currentHealth -= amount;
        Damaged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            isDead = true;
            Died?.Invoke();
            OnDeath();
        }
    }

    // Cada subtipo decide QUE pasa al morir (destruirse, avisar a las reglas...).
    protected abstract void OnDeath();
}
