using UnityEngine;

namespace ShooterDem
{
    /// <summary>
    /// Música adaptativa de 2 capas (estilo Serious Sam): PEACE suena sin enemigos y
    /// FIGHT cuando hay horda. Las DOS pistas se reproducen en loop TODO el tiempo en
    /// fuentes separadas; la "transición" es solo un crossfade de volúmenes (por eso
    /// nunca corta a mitad de nota). Opcional: un stinger de percusión tapa la costura
    /// al entrar en combate.
    ///
    /// ¿Cómo sabe si hay combate? Cuenta enemigos vivos con el bus estático de
    /// EnemyHealth (Spawned/Killed) — sin referencias de Inspector, sobrevive a todo.
    /// Al calmarse espera unos segundos (histéresis) antes de volver a peace, para no
    /// parpadear entre oleadas. Va en un GameObject propio (MusicManager) de la escena.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        [Header("Pistas (loops, misma tonalidad para que el crossfade empalme)")]
        public AudioClip peaceClip;     // exploración / arena limpia
        public AudioClip fightClip;     // combate / horda activa
        public AudioClip stingerClip;   // opcional: golpe de transición al entrar en combate

        [Header("Mezcla")]
        [Range(0f, 1f)] public float musicVolume = 0.6f;   // techo de la música (que no tape los SFX)
        public float crossfadeTime = 1.5f;                  // segundos del fundido
        public float calmDelay = 3f;                        // segundos sin enemigos antes de volver a peace
        [Range(0f, 1f)] public float stingerVolume = 0.9f;

        private AudioSource peaceSource, fightSource, stingerSource;
        private int alive;            // enemigos vivos (contados por el bus de EnemyHealth)
        private float calmTimer;      // cuenta atrás para volver a peace
        private bool combat;          // estado actual de la música

        void Awake()
        {
            peaceSource = CreateLayer(peaceClip);
            fightSource = CreateLayer(fightClip);
            stingerSource = gameObject.AddComponent<AudioSource>();   // one-shots, sin loop
            stingerSource.playOnAwake = false;
            stingerSource.spatialBlend = 0f;
        }

        AudioSource CreateLayer(AudioClip clip)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;   // 2D: la música no es posicional
            src.volume = 0f;         // arranca callada; el crossfade decide
            return src;
        }

        void OnEnable()
        {
            EnemyHealth.Spawned += OnEnemySpawned;
            EnemyHealth.Killed += OnEnemyKilled;
            GameManager.GameOverShown += OnGameOver;
        }

        void OnDisable()
        {
            EnemyHealth.Spawned -= OnEnemySpawned;
            EnemyHealth.Killed -= OnEnemyKilled;
            GameManager.GameOverShown -= OnGameOver;
        }

        // Fin de partida (victoria O derrota): la musica se apaga con fade. El crossfade
        // de Update usa tiempo UNSCALED, asi que funciona aunque el game over congele el
        // juego (timeScale 0). Sin esto, la pista de combate seguia sonando en la pantalla.
        private bool gameOver;
        void OnGameOver(string _) => gameOver = true;

        void Start()
        {
            // Ambas capas suenan SIEMPRE (en silencio la que no toca). Así el cambio es
            // instantáneo y la pista de combate no "arranca de cero" en cada oleada.
            if (peaceSource.clip != null) peaceSource.Play();
            if (fightSource.clip != null) fightSource.Play();
            peaceSource.volume = musicVolume;   // empezamos en paz
        }

        void OnEnemySpawned(EnemyHealth e) => alive++;
        void OnEnemyKilled(EnemyHealth e) => alive = Mathf.Max(0, alive - 1);

        void Update()
        {
            // --- Decidir el estado (con histéresis al calmarse) ---
            if (alive > 0)
            {
                calmTimer = calmDelay;        // hay horda: recarga el "respiro"
                if (!combat) EnterCombat();
            }
            else if (combat)
            {
                calmTimer -= Time.deltaTime;           // tiempo de JUEGO: en pausa se congela
                if (calmTimer <= 0f) combat = false;   // arena limpia un rato -> paz
            }

            // --- Crossfade: cada capa persigue su volumen objetivo ---
            float k = crossfadeTime > 0f ? Time.unscaledDeltaTime / crossfadeTime : 1f;
            float peaceTarget = combat ? 0f : musicVolume;
            float fightTarget = combat ? musicVolume : 0f;
            if (gameOver) { peaceTarget = 0f; fightTarget = 0f; }   // fin de partida: silencio
            peaceSource.volume = Mathf.MoveTowards(peaceSource.volume, peaceTarget, k * musicVolume);
            fightSource.volume = Mathf.MoveTowards(fightSource.volume, fightTarget, k * musicVolume);
        }

        void EnterCombat()
        {
            combat = true;
            // El stinger suena ENCIMA del crossfade y disimula la costura (truco SS).
            if (stingerClip != null) stingerSource.PlayOneShot(stingerClip, stingerVolume);
        }
    }
}
