using UnityEngine;
using UnityEngine.AI; // NavMeshAgent vive aqui

// IA basica: el enemigo persigue al jugador usando el NavMesh.
// Va en el GameObject "Enemy" (necesita un NavMeshAgent).
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, IKnockbackable
{
    [Header("Objetivo")]
    public Transform target; // arrastra aqui el "Player"

    [Header("Ataque")]
    public float attackRange = 2f;     // a que distancia puede golpear
    public int attackDamage = 10;      // dano por golpe
    public float attackCooldown = 1f;  // segundos entre golpes

    [Header("Rendimiento")]
    // Recalcular la ruta cada frame por enemigo no escala a hordas. Lo hacemos
    // cada repathInterval segundos, escalonado entre enemigos para no sincronizar.
    public float repathInterval = 0.2f;

    [Header("Knockback")]
    // Corto a proposito: empuje visible pero recupera la persecucion casi al instante.
    // Si fuera largo, disparar rapido encadenaria el aturdimiento y "congelaria" al enemigo.
    public float knockbackDuration = 0.08f;  // duracion del empujon + mini-aturdimiento

    private NavMeshAgent agent;
    private IDamageable targetDamageable;  // a quien golpeamos (el jugador, vía interfaz)
    private float lastAttackTime;          // cuando golpeo por ultima vez
    private float repathTimer;             // cuenta atras para el proximo SetDestination
    private float knockbackTimer;          // tiempo restante de empujon (0 = normal)
    private Vector3 knockbackVel;          // velocidad de empuje actual (decae)

    void Awake()
    {
        // Cacheamos el agente una vez.
        agent = GetComponent<NavMeshAgent>();
    }

    // OnEnable (no Start): asi tambien se reinicializa al REACTIVAR un enemigo
    // reciclado del pool (Start solo corre una vez en la vida del objeto).
    void OnEnable()
    {
        // Si nadie nos asigno un target, usamos el localizador del jugador (O(1),
        // sin escanear la escena).
        if (target == null && PlayerHealth.Current != null)
            target = PlayerHealth.Current.transform;

        // Guardamos su "danable" para poder restarle vida al atacar.
        if (target != null)
            targetDamageable = target.GetComponent<IDamageable>();

        // Escalonamos el primer repath para repartir la carga entre enemigos.
        repathTimer = Random.value * repathInterval;
        knockbackTimer = 0f;   // por si se reutiliza desde el pool
    }

    // IKnockbackable: el arma/explosion nos empuja al impactar.
    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (agent == null || !agent.isOnNavMesh) return;
        direction.y = 0f;                       // empuje horizontal, no hacia arriba
        if (direction.sqrMagnitude < 0.0001f) return;

        knockbackVel = direction.normalized * force;
        knockbackTimer = knockbackDuration;
        agent.isStopped = true;                 // deja de perseguir mientras dura
    }

    void Update()
    {
        if (target == null) return;

        // Mientras dura el knockback: empuje + mini-aturdimiento (ni persigue ni ataca).
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            agent.Move(knockbackVel * Time.deltaTime);                 // respeta el NavMesh
            knockbackVel = Vector3.Lerp(knockbackVel, Vector3.zero, Time.deltaTime / knockbackDuration);
            if (knockbackTimer <= 0f && agent.isOnNavMesh)
                agent.isStopped = false;                              // reanuda la persecucion
            return;
        }

        // Repath con throttle: solo recalculamos la ruta cada repathInterval seg
        // (no cada frame). El NavMeshAgent sigue moviendose suave entre recalculos.
        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;
            agent.SetDestination(target.position);
        }

        // El chequeo de ataque es barato: lo dejamos cada frame.
        // Si estamos lo bastante cerca, atacamos (con un cooldown entre golpes).
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            targetDamageable?.TakeDamage(attackDamage);
        }
    }
}
