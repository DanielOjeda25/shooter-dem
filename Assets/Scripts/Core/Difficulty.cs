using UnityEngine;

namespace ShooterDem
{
public enum DifficultyLevel { Easy = 0, Medium = 1, Hard = 2 }

// Dificultad global de la partida. Combina un PRESET por nivel (Facil/Medio/Dificil,
// elegido en el menu de pausa y guardado en PlayerPrefs) con el escalado por OLEADA
// (sube vida/velocidad segun avanzan las oleadas). Enemigos y spawner leen los valores
// EFECTIVOS (preset x oleada). Estatico para que cualquiera lo consulte sin referencias.
public static class Difficulty
{
    public static DifficultyLevel Level { get; private set; } = DifficultyLevel.Medium;

    // Preset del nivel actual (lo fija SetLevel).
    static float lvlSpeed = 1.2f, lvlHealth = 1f, lvlDamage = 1f, lvlAttackCd = 0.9f, lvlSpawn = 1f;

    // Escalado por oleada (lo fija el WaveSystem, ENCIMA del preset).
    public static float waveSpeed = 1f;
    public static float waveHealth = 1f;

    // Valores EFECTIVOS que consumen enemigos / spawner.
    public static float EnemySpeed     => lvlSpeed  * waveSpeed;
    public static float EnemyHealth    => lvlHealth * waveHealth;
    public static float EnemyDamage    => lvlDamage;
    public static float AttackCooldown => lvlAttackCd;   // <1 = atacan mas rapido
    public static float SpawnCount     => lvlSpawn;

    const string PrefKey = "difficultyLevel";

    // Carga el nivel guardado (llamar al arrancar la partida).
    public static void Load() =>
        SetLevel((DifficultyLevel)PlayerPrefs.GetInt(PrefKey, (int)DifficultyLevel.Medium), false);

    public static void SetLevel(DifficultyLevel level, bool save = true)
    {
        Level = level;
        switch (level)
        {
            case DifficultyLevel.Easy:
                lvlSpeed = 0.9f; lvlHealth = 0.8f; lvlDamage = 0.6f; lvlAttackCd = 1.2f; lvlSpawn = 0.8f; break;
            case DifficultyLevel.Hard:
                lvlSpeed = 1.5f; lvlHealth = 1.3f; lvlDamage = 1.4f; lvlAttackCd = 0.7f; lvlSpawn = 1.25f; break;
            default: // Medium
                lvlSpeed = 1.2f; lvlHealth = 1f; lvlDamage = 1f; lvlAttackCd = 0.9f; lvlSpawn = 1f; break;
        }
        if (save) { PlayerPrefs.SetInt(PrefKey, (int)level); PlayerPrefs.Save(); }
    }

    // Reinicia SOLO el escalado por oleada (el preset de nivel se mantiene entre oleadas).
    public static void ResetWaves() { waveSpeed = 1f; waveHealth = 1f; }
}
}
