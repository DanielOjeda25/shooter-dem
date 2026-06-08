using UnityEngine;
using UnityEngine.UIElements;

namespace ShooterDem
{
// Menu de pausa en UI Toolkit. Se muestra/oculta segun GameManager.PauseChanged y cablea
// los botones. Tiene un submenu "OPCIONES" con la DIFICULTAD (Facil/Medio/Dificil), que
// marca el nivel activo. Va en un GameObject con UIDocument (PauseMenu_UITK).
[RequireComponent(typeof(UIDocument))]
public class PauseMenuController : MonoBehaviour
{
    private VisualElement root, mainMenu, optionsMenu;
    private Button diffEasy, diffMedium, diffHard;

    void OnEnable()  { GameManager.PauseChanged += OnPauseChanged; }
    void OnDisable() { GameManager.PauseChanged -= OnPauseChanged; }

    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        mainMenu = root.Q<VisualElement>("pause-menu");
        optionsMenu = root.Q<VisualElement>("options-menu");

        Bind("btn-resume", () => GameManager.Instance?.Resume());
        Bind("btn-restart", () => GameManager.Instance?.RestartGame());
        Bind("btn-quit", () => GameManager.Instance?.QuitGame());
        Bind("btn-options", ShowOptions);
        Bind("btn-options-back", ShowMain);

        diffEasy = root.Q<Button>("btn-diff-easy");
        diffMedium = root.Q<Button>("btn-diff-medium");
        diffHard = root.Q<Button>("btn-diff-hard");
        if (diffEasy != null) diffEasy.clicked += () => SetDifficulty(DifficultyLevel.Easy);
        if (diffMedium != null) diffMedium.clicked += () => SetDifficulty(DifficultyLevel.Medium);
        if (diffHard != null) diffHard.clicked += () => SetDifficulty(DifficultyLevel.Hard);

        ShowMain();   // empieza en el menu principal (oculta opciones)
        Hide();       // oculto del todo hasta pausar
    }

    void Bind(string name, System.Action action)
    {
        var btn = root.Q<Button>(name);
        if (btn != null) btn.clicked += action;
    }

    void SetDifficulty(DifficultyLevel level)
    {
        Difficulty.SetLevel(level);   // aplica + guarda en PlayerPrefs
        RefreshDiffHighlight();
    }

    void RefreshDiffHighlight()
    {
        SetSelected(diffEasy,   Difficulty.Level == DifficultyLevel.Easy);
        SetSelected(diffMedium, Difficulty.Level == DifficultyLevel.Medium);
        SetSelected(diffHard,   Difficulty.Level == DifficultyLevel.Hard);
    }

    static void SetSelected(Button b, bool on)
    {
        if (b == null) return;
        if (on) b.AddToClassList("selected");
        else b.RemoveFromClassList("selected");
    }

    void OnPauseChanged(bool paused)
    {
        if (root == null) return;
        if (paused) Show(); else Hide();
    }

    void Show() { root.style.display = DisplayStyle.Flex; ShowMain(); }   // siempre abre en el principal
    void Hide() { root.style.display = DisplayStyle.None; }

    void ShowMain()
    {
        if (mainMenu != null) mainMenu.style.display = DisplayStyle.Flex;
        if (optionsMenu != null) optionsMenu.style.display = DisplayStyle.None;
    }

    void ShowOptions()
    {
        if (mainMenu != null) mainMenu.style.display = DisplayStyle.None;
        if (optionsMenu != null) optionsMenu.style.display = DisplayStyle.Flex;
        RefreshDiffHighlight();   // marca el nivel activo al abrir
    }
}
}
