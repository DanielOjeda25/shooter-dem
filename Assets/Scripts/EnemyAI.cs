using UnityEngine;
using UnityEngine.AI; // NavMeshAgent vive aqui

// IA basica: el enemigo persigue al jugador usando el NavMesh.
// Va en el GameObject "Enemy" (necesita un NavMeshAgent).
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target; // arrastra aqui el "Player"

    [Header("Ataque")]
    public float attackRange = 2f;     // a que distancia puede golpear
    public int attackDamage = 10;      // dano por golpe
    public float attackCooldown = 1f;  // segundos entre golpes

    private NavMeshAgent agent;
    private IDamageable targetDamageable;  // a quien golpeamos (el jugador, vía interfaz)
    private float lastAttackTime;          // cuando golpeo por ultima vez

    void Awake()
    {
        // Cacheamos el agente una vez.
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        // Si nadie nos asigno un target (p. ej. enemigos generados desde un prefab),
        // buscamos al jugador en la escena por su componente PlayerHealth.
        if (target == null)
        {
            PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
            if (player != null)
                target = player.transform;
        }

        // Guardamos su "danable" para poder restarle vida al atacar.
        if (target != null)
            targetDamageable = target.GetComponent<IDamageable>();
    }

    void Update()
    {
        if (target == null) return;

        // Cada frame, le decimos al agente "ve hacia el jugador".
        // El NavMeshAgent calcula solo la ruta sobre el NavMesh.
        agent.SetDestination(target.position);

        // Si estamos lo bastante cerca, atacamos (con un cooldown entre golpes).
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            targetDamageable?.TakeDamage(attackDamage);
        }
    }
}
