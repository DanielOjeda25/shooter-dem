using UnityEngine;
using UnityEngine.SceneManagement;  // para recargar la escena (reiniciar)
using UnityEngine.InputSystem;      // tecla Escape (Input System nuevo)
using TMPro; // TextMeshPro

// El "arbitro" del juego: gestiona derrota, pausa y la UI de fin. La VICTORIA y el
// recuento de enemigos los lleva el WaveSystem (que llama a TriggerVictory()).
// Va en un GameObject vacio "GameManager".
public class GameManager : MonoBehaviour
{
    // Singleton: referencia global para utilidades y para que el WaveSystem dispare
    // la victoria sin arrastrarlo en el Inspector.
    public static GameManager Instance { get; private set; }

    [Header("Game Over (UI)")]
    public GameObject gameOverPanel;  // panel que se enciende al terminar (empieza apagado)
    public TMP_Text gameOverText;     // texto grande del mensaje

    [Header("Pausa (UI)")]
    public GameObject pausePanel;     // panel de pausa (empieza apagado)

    private bool gameOver;
    private bool isPaused;

    void Awake()
    {
        // Guardamos la referencia global (la usan los botones de UI / utilidades).
        Instance = this;
    }

    // Solo nos importa la muerte del jugador (derrota). Los enemigos los cuenta el WaveSystem.
    void OnEnable()
    {
        PlayerHealth.PlayerDied += HandlePlayerDied;
    }

    void OnDisable()
    {
        PlayerHealth.PlayerDied -= HandlePlayerDied;
    }

    void Update()
    {
        // Si ya termino la partida, Escape no hace nada (el juego ya esta congelado).
        if (gameOver) return;

        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    // ---------- Reacciones a eventos / API publica ----------

    void HandlePlayerDied()
    {
        Lose();
    }

    // Lo llama el WaveSystem cuando se completa la ultima oleada (modo finito).
    public void TriggerVictory()
    {
        if (gameOver) return; // evita repetir
        gameOver = true;
        Debug.Log("=== VICTORIA: todas las oleadas superadas ===");
        ShowGameOver("GANASTE");
    }

    void Lose()
    {
        if (gameOver) return;
        gameOver = true;
        Debug.Log("=== DERROTA: has muerto ===");
        ShowGameOver("PERDISTE");
    }

    // Parte visual comun: congela el juego, suelta el cursor y muestra el panel.
    void ShowGameOver(string message)
    {
        Time.timeScale = 0f;                      // 0 = todo se detiene (enemigos, fisica)
        Cursor.lockState = CursorLockMode.None;   // soltamos el cursor
        Cursor.visible = true;

        if (gameOverText != null)
            gameOverText.text = message;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);        // encendemos el panel
    }

    // ---------- Pausa ----------

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    // Llamado por la tecla Escape y por el boton "Reanudar".
    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;                       // el juego vuelve a correr
        Cursor.lockState = CursorLockMode.Locked;  // re-enganchamos el cursor (FPS)
        Cursor.visible = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    // ---------- Botones ----------

    // Reinicia: recarga la escena actual desde cero.
    public void RestartGame()
    {
        Time.timeScale = 1f; // IMPORTANTE: restaurar antes de recargar, si no la escena nueva nace congelada
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Sale del juego. Solo funciona en una build; en el editor solo avisa.
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego... (solo cierra en una build, no en el editor)");
        Application.Quit();
    }
}
