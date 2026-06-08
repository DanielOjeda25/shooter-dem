using System;
using System.Collections;
using UnityEngine;

namespace ShooterDem
{
// Cerebro de las hordas. Genera oleadas crecientes via EnemySpawner y avanza a la
// siguiente cuando se limpia la actual. Cuenta enemigos vivos con los eventos
// EnemyHealth.Spawned/Killed (mismo patron observador del resto del proyecto).
//
// PACING (clave para hordas): no suelta toda la oleada de golpe. Mantiene un TOPE de
// enemigos vivos a la vez (maxAliveAtOnce) y va metiendo el resto por TANDAS segun
// caen otros. Asi la oleada puede ser enorme sin reventar el rendimiento.
//
// Modo HIBRIDO: totalWaves = 0 -> infinitas (survival, sin victoria);
//               totalWaves = N -> finitas, victoria al limpiar la oleada N.
public class WaveSystem : MonoBehaviour
{
    [Header("Oleadas")]
    public int totalWaves = 0;            // 0 = infinitas; N = finitas con victoria
    public int baseEnemies = 5;           // enemigos en la oleada 1
    public int enemiesGrowthPerWave = 3;  // +N enemigos por cada oleada siguiente

    [Header("Pacing / hordas")]
    public int maxAliveAtOnce = 20;       // tope de enemigos vivos a la vez
    public int spawnBatchSize = 5;        // cuantos intenta meter por tick
    public float spawnInterval = 0.5f;    // segundos entre tandas de spawn
    public int maxSpawnFailTicks = 5;     // ticks fallando seguidos antes de rendirse (anti-cuelgue)

    [Header("Descanso entre oleadas (decrece con la dificultad)")]
    public float timeBetweenWaves = 5f;        // descanso tras la oleada 1
    public float restReductionPerWave = 0.5f;  // cuanto se acorta por oleada
    public float minTimeBetweenWaves = 1.5f;   // suelo del descanso
    public float firstWaveDelay = 2f;          // margen antes de la primera oleada

    [Header("Escalado de dificultad (por oleada)")]
    public float healthGrowthPerWave = 0.15f;  // +15% vida de enemigos por oleada
    public float speedGrowthPerWave = 0.05f;   // +5% velocidad por oleada
    public float maxSpeedMultiplier = 2f;      // tope de velocidad (que no sea imposible)

    [Header("Generador")]
    public EnemySpawner spawner;          // quien instancia los enemigos

    private int currentWave;              // oleada actual (1-based)
    private int enemiesAlive;             // vivos ahora mismo

    public int CurrentWave => currentWave;
    public event Action<int> WaveChanged; // nuevo numero de oleada (para el HUD)

    void OnEnable()
    {
        EnemyHealth.Spawned += OnEnemySpawned;
        EnemyHealth.Killed += OnEnemyKilled;
    }

    void OnDisable()
    {
        EnemyHealth.Spawned -= OnEnemySpawned;
        EnemyHealth.Killed -= OnEnemyKilled;
    }

    void Start()
    {
        if (spawner == null)
        {
            Debug.LogError("WaveSystem: falta asignar el EnemySpawner.");
            return;
        }
        Difficulty.ResetWaves();   // reinicia el escalado por oleada (el nivel se mantiene)
        StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        yield return new WaitForSeconds(firstWaveDelay);

        while (true)
        {
            currentWave++;
            WaveChanged?.Invoke(currentWave);

            // Escalado por oleada: enemigos mas duros y rapidos (ENCIMA del nivel de
            // dificultad). Cada enemigo lo lee al activarse, incluso del pool.
            Difficulty.waveHealth = 1f + (currentWave - 1) * healthGrowthPerWave;
            Difficulty.waveSpeed = Mathf.Min(maxSpeedMultiplier, 1f + (currentWave - 1) * speedGrowthPerWave);

            int remaining = EnemiesForWave(currentWave);   // cuota total de la oleada
            Debug.Log($"=== OLEADA {currentWave} — {remaining} enemigos (max {maxAliveAtOnce} vivos) ===");

            // Spawn por tandas respetando el tope de vivos: solo metemos lo que cabe
            // bajo el tope; cuando mueren enemigos se abre hueco y entran mas.
            // Descontamos por lo REALMENTE generado (algunos pueden omitirse por sitio).
            int failTicks = 0;
            while (remaining > 0)
            {
                int batch = ToSpawnThisTick(remaining, enemiesAlive);
                if (batch > 0)
                {
                    int spawned = spawner.SpawnEnemies(batch);
                    remaining -= spawned;

                    // Si pedimos spawnear pero no salio ninguno, contamos fallo.
                    // Tras varios seguidos, abandonamos el resto (evita bucle infinito
                    // si no hay sitio valido) y seguimos con los ya generados.
                    if (spawned == 0)
                    {
                        if (++failTicks >= maxSpawnFailTicks)
                        {
                            Debug.LogWarning($"WaveSystem: no se pudo generar el resto de la oleada ({remaining} omitidos).");
                            break;
                        }
                    }
                    else failTicks = 0;
                }
                yield return new WaitForSeconds(spawnInterval);
            }

            // Ya estan todos generados: esperamos a que caiga el ultimo.
            yield return new WaitUntil(() => enemiesAlive <= 0);

            // Modo finito: si era la ultima oleada, victoria y fin.
            if (totalWaves > 0 && currentWave >= totalWaves)
            {
                Debug.Log("=== Todas las oleadas superadas ===");
                if (GameManager.Instance != null)
                    GameManager.Instance.TriggerVictory();
                yield break;
            }

            yield return new WaitForSeconds(RestTime(currentWave));
        }
    }

    // Cuota total de enemigos de una oleada (crece con la oleada y con la dificultad).
    int EnemiesForWave(int wave) =>
        Mathf.Max(1, Mathf.RoundToInt((baseEnemies + (wave - 1) * enemiesGrowthPerWave) * Difficulty.SpawnCount));

    // Cuantos meter en este tick: el hueco que queda bajo el tope, limitado por el
    // tamano de tanda y por lo que aun falta por generar. Nunca negativo.
    int ToSpawnThisTick(int remaining, int alive)
    {
        int room = Mathf.Min(maxAliveAtOnce - alive, spawnBatchSize);
        return Mathf.Clamp(room, 0, remaining);
    }

    // Descanso entre oleadas: se acorta con la dificultad hasta un minimo (mas presion).
    float RestTime(int wave) => Mathf.Max(minTimeBetweenWaves, timeBetweenWaves - (wave - 1) * restReductionPerWave);

    void OnEnemySpawned(EnemyHealth enemy) => enemiesAlive++;
    void OnEnemyKilled(EnemyHealth enemy) => enemiesAlive--;
}
}
