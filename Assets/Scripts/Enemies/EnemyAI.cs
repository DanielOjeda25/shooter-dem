using UnityEngine;
using UnityEngine.AI; // NavMeshAgent vive aqui

namespace ShooterDem
{
// IA basica: el enemigo persigue al jugador usando el NavMesh.
// Va en el GameObject "Enemy" (necesita un NavMeshAgent).
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, IKnockbackable
{
    [Header("Objetivo")]
    public Transform target; // arrastra aqui el "Player"

    [Header("Ataque")]
    public float attackRange = 2f;     // a que distancia puede atacar
    public float attackCooldown = 1f;  // segundos entre ataques (el EFECTO lo hace EnemyAttack)
    // KITING (enemigos a distancia): si > 0, mantiene esta distancia del objetivo
    // (se aleja si esta mas cerca, se acerca si esta mas lejos). 0 = perseguir siempre (melee).
    public float keepDistance = 0f;

    [Header("Rendimiento")]
    // Recalcular la ruta cada frame por enemigo no escala a hordas. Lo hacemos
    // cada repathInterval segundos, escalonado entre enemigos para no sincronizar.
    public float repathInterval = 0.2f;

    [Header("Knockback")]
    // Corto a proposito: empuje visible pero recupera la persecucion casi al instante.
    // Si fuera largo, disparar rapido encadenaria el aturdimiento y "congelaria" al enemigo.
    public float knockbackDuration = 0.08f;  // duracion del empujon + mini-aturdimiento
    // Resistencia al empuje POR TIPO de enemigo: 1 = normal; <1 pesado/tanque (se mueve
    // menos); >1 ligero (sale mas disparado). Se multiplica por la fuerza del arma.
    public float knockbackMultiplier = 1f;

    private NavMeshAgent agent;
    private EnemyAttack attack;            // estrategia de ataque (melee, a distancia, kamikaze...)
    private float lastAttackTime;          // cuando ataco por ultima vez
    private float repathTimer;             // cuenta atras para el proximo SetDestination
    private float knockbackTimer;          // tiempo restante de empujon (0 = normal)
    private Vector3 knockbackVel;          // velocidad de empuje actual (decae)
    private float baseSpeed;               // velocidad original del agente (sin escalar)

    void Awake()
    {
        // Cacheamos componentes una vez.
        agent = GetComponent<NavMeshAgent>();
        attack = GetComponent<EnemyAttack>();   // la estrategia de ataque del prefab
        baseSpeed = agent.speed;                // guardamos la velocidad base del prefab
    }

    // OnEnable (no Start): asi tambien se reinicializa al REACTIVAR un enemigo
    // reciclado del pool (Start solo corre una vez en la vida del objeto).
    void OnEnable()
    {
        // Si nadie nos asigno un target, usamos el localizador del jugador (O(1),
        // sin escanear la escena).
        if (target == null && PlayerHealth.Current != null)
            target = PlayerHealth.Current.transform;

        // Escalonamos el primer repath para repartir la carga entre enemigos.
        repathTimer = Random.value * repathInterval;
        knockbackTimer = 0f;            // por si se reutiliza desde el pool
        knockbackVel = Vector3.zero;    // sin empuje residual de la vida anterior

        // Velocidad escalada por la dificultad (nivel + oleada).
        if (agent != null) agent.speed = baseSpeed * Difficulty.EnemySpeed;

        // Enemigos a distancia: la rotacion la llevamos nosotros (mirar al jugador), no
        // el agente (que miraria a su destino de movimiento).
        if (agent != null && keepDistance > 0f) agent.updateRotation = false;
    }

    // IKnockbackable: el arma/explosion nos empuja al impactar.
    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (agent == null || !agent.isOnNavMesh) return;
        direction.y = 0f;                       // empuje horizontal, no hacia arriba
        if (direction.sqrMagnitude < 0.0001f) return;

        knockbackVel = direction.normalized * force * knockbackMultiplier;  // pesado resiste, ligero vuela
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
            UpdateDestination();
        }

        // En rango + sin cooldown -> delegamos el efecto a la estrategia de ataque.
        // sqrMagnitude (sin sqrt) para no pagar una raiz por enemigo cada frame.
        Vector3 toTarget = target.position - transform.position;
        if (toTarget.sqrMagnitude <= attackRange * attackRange
            && Time.time >= lastAttackTime + attackCooldown * Difficulty.AttackCooldown)
        {
            lastAttackTime = Time.time;
            attack?.Execute(target);
        }

        // Enemigos a distancia: mirar al jugador (para que el arma apunte de frente).
        if (keepDistance > 0f)
        {
            Vector3 flat = toTarget; flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(flat), 360f * Time.deltaTime);
        }
    }

    // Decide a donde ir. Por defecto persigue. Con keepDistance > 0 hace KITING:
    // retrocede si esta muy cerca, se acerca si esta lejos, se planta en la banda.
    void UpdateDestination()
    {
        if (keepDistance <= 0f) { agent.SetDestination(target.position); return; }

        Vector3 flat = target.position - transform.position; flat.y = 0f;
        float dist = flat.magnitude;
        if (dist < keepDistance * 0.85f && dist > 0.01f)
            agent.SetDestination(transform.position - flat.normalized * 3f);  // retrocede
        else if (dist > keepDistance * 1.15f)
            agent.SetDestination(target.position);                            // se acerca
        else
            agent.SetDestination(transform.position);                         // se planta y dispara
    }
}
}
