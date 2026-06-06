// Dificultad global actual de la partida. La fija el WaveSystem en cada oleada y la
// leen los enemigos al nacer/reactivarse (compatible con el pooling). Estatico para
// que cualquier enemigo lo consulte sin referencias cruzadas.
public static class Difficulty
{
    public static float healthMultiplier = 1f;  // x vida de los enemigos
    public static float speedMultiplier = 1f;    // x velocidad de los enemigos

    // Vuelve a 1 (lo llama WaveSystem al empezar, importante al reiniciar escena:
    // los estaticos NO se resetean solos entre partidas en el editor).
    public static void Reset()
    {
        healthMultiplier = 1f;
        speedMultiplier = 1f;
    }
}
