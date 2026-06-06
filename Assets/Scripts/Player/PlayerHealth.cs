using System;
using UnityEngine;

// Vida del jugador. Hereda Health; al morir avisa por un evento estatico que
// escucha el GameManager para disparar la derrota. El HUD lee CurrentHealth/maxHealth
// (de la base) y puede suscribirse a Damaged.
public class PlayerHealth : Health
{
    public static event Action PlayerDied; // "el jugador ha muerto"

    // Localizador: el unico jugador vivo se publica aqui. Evita que cada enemigo
    // haga FindAnyObjectByType (O(n) de escena) para encontrarlo. O(1) y robusto
    // a recargas de escena (el nuevo Player se republica en su OnEnable).
    public static PlayerHealth Current { get; private set; }

    void OnEnable()  { Current = this; }
    void OnDisable() { if (Current == this) Current = null; }

    protected override void OnDeath()
    {
        Debug.Log("Has muerto.");
        PlayerDied?.Invoke();
    }
}
