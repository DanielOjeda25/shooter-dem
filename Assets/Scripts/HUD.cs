using UnityEngine;
using TMPro; // TextMeshPro

// Muestra vida y municion. En vez de leer cada frame (polling + alloc de string
// cada frame), se SUSCRIBE a los eventos y solo redibuja cuando algo cambia:
// PlayerHealth.Damaged para la vida y Weapon.AmmoChanged para la municion.
public class HUD : MonoBehaviour
{
    [Header("Fuentes de datos")]
    public PlayerHealth playerHealth; // arrastra el Player
    public Weapon weapon;             // arrastra el Weapon
    public WaveSystem waveSystem;     // arrastra el EnemySpawner (lleva el WaveSystem)

    [Header("Textos (TextMeshPro)")]
    public TMP_Text healthText;       // arrastra el texto de vida
    public TMP_Text ammoText;         // arrastra el texto de municion
    public TMP_Text waveText;         // arrastra el texto de oleada

    void OnEnable()
    {
        if (playerHealth != null) playerHealth.Damaged += OnHealthChanged;
        if (weapon != null) weapon.AmmoChanged += RefreshAmmo;
        if (waveSystem != null) waveSystem.WaveChanged += OnWaveChanged;
    }

    void OnDisable()
    {
        if (playerHealth != null) playerHealth.Damaged -= OnHealthChanged;
        if (weapon != null) weapon.AmmoChanged -= RefreshAmmo;
        if (waveSystem != null) waveSystem.WaveChanged -= OnWaveChanged;
    }

    void Start()
    {
        // Pintamos el estado inicial (los eventos solo disparan al CAMBIAR).
        RefreshHealth();
        RefreshAmmo();
        RefreshWave();
    }

    // La firma encaja con Health.Damaged (int, int); leemos del componente igualmente.
    void OnHealthChanged(int current, int max) => RefreshHealth();

    void RefreshHealth()
    {
        if (playerHealth != null && healthText != null)
            healthText.text = $"VIDA  {playerHealth.CurrentHealth}/{playerHealth.maxHealth}";
    }

    void RefreshAmmo()
    {
        if (weapon != null && ammoText != null)
        {
            ammoText.text = weapon.IsReloading
                ? "RECARGANDO..."
                : $"MUNICION  {weapon.CurrentAmmo}/{weapon.magazineSize}";
        }
    }

    void OnWaveChanged(int wave) => RefreshWave();

    void RefreshWave()
    {
        if (waveSystem == null || waveText == null) return;

        int w = waveSystem.CurrentWave;
        if (w <= 0)
        {
            waveText.text = "PREPARATE...";   // antes de la primera oleada
            return;
        }
        // Modo finito muestra N/Total; infinito solo el numero.
        waveText.text = waveSystem.totalWaves > 0
            ? $"OLEADA  {w}/{waveSystem.totalWaves}"
            : $"OLEADA  {w}";
    }
}
