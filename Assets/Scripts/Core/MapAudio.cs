using UnityEngine;

namespace ShooterDem
{
    /// <summary>
    /// "Ficha" de audio de un MAPA (data-driven, como WeaponData/EnemyData): la música de
    /// paz/combate y el ambiente de fondo que suenan en ese mapa. Se arrastra al campo
    /// `mapAudio` del MusicManager; cambiar la música de un mapa = editar este asset (o
    /// soltar otro distinto). Cero código.
    ///
    /// Convención de clips: cada tema es un par `ambience-N-peace` / `ambience-N-fight`
    /// en la MISMA tonalidad/tempo (para que el crossfade peace<->fight empalme).
    /// Create: Assets > Create > Shooter > Map Audio.
    /// </summary>
    [CreateAssetMenu(fileName = "MapAudio", menuName = "Shooter/Map Audio")]
    public class MapAudio : ScriptableObject
    {
        [Header("Música adaptativa (peace y fight del MISMO tema)")]
        public AudioClip peace;     // exploración / arena limpia
        public AudioClip fight;     // combate / horda activa
        public AudioClip stinger;   // opcional: golpe de percusión al entrar en combate

        [Header("Ambiente de fondo (viento, ceniza... suena SIEMPRE en el mapa)")]
        public AudioClip ambientLoop;
        [Range(0f, 1f)] public float ambientVolume = 0.5f;
    }
}
