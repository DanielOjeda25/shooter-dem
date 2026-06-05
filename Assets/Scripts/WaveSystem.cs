using System;
using System.Collections;
using UnityEngine;

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

    [Header("Descanso entre oleadas (decrece con la dificultad)")]
    public float timeBetweenWaves = 5f;        // descanso tras la oleada 1
    public float restReductionPerWave = 0.5f;  // cuanto se acorta por oleada
    public float minTimeBetweenWaves = 1.5f;   // suelo del descanso
    public float firstWaveDelay = 2f;          // margen antes de la primera oleada

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
        StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        yield return new WaitForSeconds(firstWaveDelay);

        while (true)
        {
            currentWave++;
            WaveChanged?.Invoke(currentWave);

            int remaining = EnemiesForWave(currentWave);   // cuota total de la oleada
            Debug.Log($"=== OLEADA {currentWave} — {remaining} enemigos (max {maxAliveAtOnce} vivos) ===");

            // Spawn por tandas respetando el tope de vivos: solo metemos lo que cabe
            // bajo el tope; cuando mueren enemigos se abre hueco y entran mas.
            while (remaining > 0)
            {
                int batch = ToSpawnThisTick(remaining, enemiesAlive);
                if (batch > 0)
                {
                    spawner.SpawnEnemies(batch);
                    remaining -= batch;
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

    // Cuota total de enemigos de una oleada (crece linealmente con la oleada).
    int EnemiesForWave(int wave) => Mathf.Max(1, baseEnemies + (wave - 1) * enemiesGrowthPerWave);

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
