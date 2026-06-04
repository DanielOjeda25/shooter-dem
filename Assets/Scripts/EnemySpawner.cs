using UnityEngine;

// Genera varios enemigos al empezar, en posiciones aleatorias alrededor de este objeto.
// Va en un GameObject vacio (ej. "EnemySpawner").
public class EnemySpawner : MonoBehaviour
{
    [Header("Que generar")]
    public GameObject enemyPrefab;    // arrastra aqui el prefab del Enemy

    [Header("Cuantos y donde")]
    public int count = 5;             // cuantos enemigos generar
    public float areaRadius = 20f;    // radio (en metros) alrededor del spawner
    public float spawnHeight = 1f;    // altura Y (centro de la capsula = 1)

    void Start()
    {
        for (int i = 0; i < count; i++)
            SpawnOne();
    }

    void SpawnOne()
    {
        // Random.insideUnitCircle da un punto al azar dentro de un circulo de radio 1.
        // Lo multiplicamos por el radio para repartirlos por el area.
        Vector2 circle = Random.insideUnitCircle * areaRadius;
        Vector3 pos = transform.position + new Vector3(circle.x, spawnHeight, circle.y);

        Instantiate(enemyPrefab, pos, Quaternion.identity);
    }
}
