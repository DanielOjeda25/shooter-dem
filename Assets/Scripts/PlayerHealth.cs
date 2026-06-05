using System;
using UnityEngine;

// Vida del jugador. Hereda Health; al morir avisa por un evento estatico que
// escucha el GameManager para disparar la derrota. El HUD lee CurrentHealth/maxHealth
// (de la base) y puede suscribirse a Damaged.
public class PlayerHealth : Health
{
    public static event Action PlayerDied; // "el jugador ha muerto"

    protected override void OnDeath()
    {
        Debug.Log("Has muerto.");
        PlayerDied?.Invoke();
    }
}
