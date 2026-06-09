// Helper minimo agregado para ASHFALL.
// El "Free Sample" del Low Poly Shooter Pack referencia Log.kill(...) pero NO incluye la
// clase Log del pack completo. Reimplementacion minima (solo logging) para que compile.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Logging helper del pack (reimplementado: el free sample no lo trae).
    /// </summary>
    public static class Log
    {
        /// <summary>Loguea un error (uso original: avisar de un fallo grave de servicio).</summary>
        public static void kill(string message) => Debug.LogError(message);
    }
}
