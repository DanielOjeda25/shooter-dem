using System;
using UnityEngine;

namespace ShooterDem
{
// Vida del enemigo. Hereda toda la mecanica de Health y solo aporta lo propio:
// anunciar nacimiento/muerte por eventos ESTATICOS (un "bus" que escuchan el
// WaveSystem y, a futuro, otros sistemas) y volver al pool al morir.
// Ya NO conoce al GameManager: solo emite eventos; quien quiera, que cuente.
public class EnemyHealth : Health
{
    public static event Action<EnemyHealth> Spawned; // "ha nacido / revivido un enemigo"
    public static event Action<EnemyHealth> Killed;   // "ha muerto un enemigo"

    private EnemyPool pool;                 // de donde salio (null si fue Instantiate suelto)
    public void SetPool(EnemyPool p) => pool = p;

    // Usamos OnEnable (no Awake) para que esto tambien corra al REACTIVAR un enemigo
    // reciclado del pool. Revive (vida llena) y anuncia su nacimiento.
    void OnEnable()
    {
        ScaleAndResetHealth(Difficulty.EnemyHealth);  // vida escalada por nivel + oleada
        Spawned?.Invoke(this);
    }

    protected override void OnDeath()
    {
        Debug.Log($"{name} ha muerto.");
        Killed?.Invoke(this);

        // En vez de destruir, volvemos al pool para reutilizarnos en la proxima oleada.
        // (Si nadie nos asigno pool, nos destruimos como antes.)
        if (pool != null) pool.Return(this);
        else Destroy(gameObject);
    }
}
}
