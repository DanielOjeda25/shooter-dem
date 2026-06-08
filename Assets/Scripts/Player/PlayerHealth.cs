using System;
using UnityEngine;

namespace ShooterDem
{
// Vida del jugador. Hereda Health; al morir avisa por un evento estatico que
// escucha el GameManager para disparar la derrota. El HUD lee CurrentHealth/maxHealth
// (de la base) y puede suscribirse a Damaged.
public class PlayerHealth : Health
{
    public static event Action PlayerDied; // "el jugador ha muerto"

    // "Me han golpeado desde esta posicion del mundo" (para el indicador direccional de
    // dano + screen shake). Lo invocan los atacantes al danar al jugador, ya que
    // IDamageable.TakeDamage no lleva el origen.
    public event Action<Vector3> Hit;
    public void RegisterHit(Vector3 sourceWorldPos) => Hit?.Invoke(sourceWorldPos);

    // Localizador: el unico jugador vivo se publica aqui. Evita que cada enemigo
    // haga FindAnyObjectByType (O(n) de escena) para encontrarlo. O(1) y robusto
    // a recargas de escena (el nuevo Player se republica en su OnEnable).
    public static PlayerHealth Current { get; private set; }

    private PlayerMovement movement;

    protected override void Awake()
    {
        base.Awake();
        movement = GetComponent<PlayerMovement>();
    }

    void OnEnable()  { Current = this; }
    void OnDisable() { if (Current == this) Current = null; }

    // i-frames mientras dashea: esquivar de verdad evita el dano.
    void Update()
    {
        Invulnerable = movement != null && movement.IsDashing;
    }

    protected override void OnDeath()
    {
        Debug.Log("Has muerto.");
        PlayerDied?.Invoke();
    }
}
}
