using UnityEngine;

namespace ShooterDem
{
// Sacudida de camara (screen shake) por "trauma": cada impacto suma trauma (0..1) y este
// decae solo. La sacudida se calcula con ruido Perlin (suave, no tembleque aleatorio) y se
// aplica SOBRE la rotacion que dejo MouseLook este frame -> NO se acumula, porque MouseLook
// reescribe la rotacion cada frame antes de este LateUpdate.
//
// Va en la Main Camera (junto a MouseLook). Singleton: cualquiera dispara shake con
// CameraShake.Add(x) o CameraShake.AddAt(pos, x) (escalado por distancia, para explosiones).
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Intensidad (grados)")]
    public float maxPitch = 2.5f;
    public float maxYaw = 2.5f;
    public float maxRoll = 4f;

    [Header("Comportamiento")]
    public float frequency = 22f;    // velocidad del ruido (mas alto = mas nervioso)
    public float decay = 1.8f;       // cuanto baja el trauma por segundo
    public float maxDistance = 30f;  // explosiones mas lejos que esto no sacuden

    private float trauma;
    private float seedPitch, seedYaw, seedRoll;

    void Awake()
    {
        Instance = this;
        seedPitch = Random.value * 100f;
        seedYaw   = Random.value * 100f;
        seedRoll  = Random.value * 100f;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // Suma trauma directo (p. ej. al disparar o recibir dano).
    public static void Add(float amount)
    {
        if (Instance != null) Instance.trauma = Mathf.Clamp01(Instance.trauma + amount);
    }

    // Suma trauma escalado por la distancia de la camara al evento (explosiones lejanas
    // sacuden menos, las cercanas mucho).
    public static void AddAt(Vector3 worldPos, float amount)
    {
        if (Instance == null) return;
        float d = Vector3.Distance(Instance.transform.position, worldPos);
        float f = 1f - Mathf.Clamp01(d / Instance.maxDistance);
        if (f > 0f) Instance.trauma = Mathf.Clamp01(Instance.trauma + amount * f);
    }

    void LateUpdate()
    {
        if (trauma <= 0f) return;
        trauma = Mathf.Max(0f, trauma - decay * Time.deltaTime);

        float s = trauma * trauma;          // falloff cuadratico: se siente mas "punchy"
        float t = Time.time * frequency;
        float pitch = maxPitch * s * (Mathf.PerlinNoise(seedPitch, t) * 2f - 1f);
        float yaw   = maxYaw   * s * (Mathf.PerlinNoise(seedYaw,   t) * 2f - 1f);
        float roll  = maxRoll  * s * (Mathf.PerlinNoise(seedRoll,  t) * 2f - 1f);

        transform.localRotation *= Quaternion.Euler(pitch, yaw, roll);
    }
}
}
