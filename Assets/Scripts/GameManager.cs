using UnityEngine;
using UnityEngine.SceneManagement;  // para recargar la escena (reiniciar)
using UnityEngine.InputSystem;      // tecla Escape (Input System nuevo)
using TMPro; // TextMeshPro

// El "arbitro" del juego: lleva la cuenta de enemigos vivos y decide victoria/derrota.
// Va en un GameObject vacio "GameManager".
public class GameManager : MonoBehaviour
{
    // Singleton: una referencia estatica para que cualquier script (EnemyHealth,
    // PlayerHealth) pueda avisar al GameManager sin tener que arrastrarlo en el Inspector.
    public static GameManager Instance { get; private set; }

    [Header("Game Over (UI)")]
    public GameObject gameOverPanel;  // panel que se enciende al terminar (empieza apagado)
    public TMP_Text gameOverText;     // texto grande del mensaje

    [Header("Pausa (UI)")]
    public GameObject pausePanel;     // panel de pausa (empieza apagado)

    private int enemiesAlive;
    private bool gameOver;
    private bool isPaused;

    void Awake()
    {
        // Guardamos la referencia global. Awake corre antes que cualquier Start,
        // asi que cuando los enemigos se registren, Instance ya existe.
        Instance = this;
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

    // Cada enemigo se apunta al nacer.
    public void RegisterEnemy()
    {
        enemiesAlive++;
    }

    // Cada enemigo avisa al morir.
    public void EnemyKilled()
    {
        enemiesAlive--;
        Debug.Log($"Enemigos restantes: {enemiesAlive}");

        if (enemiesAlive <= 0)
            Win();
    }

    // El jugador avisa al morir.
    public void PlayerDied()
    {
        Lose();
    }

    void Win()
    {
        if (gameOver) return; // evita repetir
        gameOver = true;
        Debug.Log("=== VICTORIA: todos los enemigos eliminados ===");
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
