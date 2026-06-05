using System;
using UnityEngine;

// Vida del enemigo. Hereda toda la mecanica de Health y solo aporta lo propio:
// anunciar nacimiento/muerte por eventos ESTATICOS (un "bus" que escucha el
// GameManager hoy y el sistema de oleadas en v2.0) y destruirse al morir.
// Ya NO conoce al GameManager: solo emite eventos; quien quiera, que cuente.
public class EnemyHealth : Health
{
    public static event Action<EnemyHealth> Spawned; // "ha nacido un enemigo"
    public static event Action<EnemyHealth> Killed;   // "ha muerto un enemigo"

    protected override void Awake()
    {
        base.Awake();            // inicializa la vida
        Spawned?.Invoke(this);
    }

    protected override void OnDeath()
    {
        Debug.Log($"{name} ha muerto.");
        Killed?.Invoke(this);
        Destroy(gameObject);     // de momento desaparece; efectos de muerte vendran luego
    }
}
