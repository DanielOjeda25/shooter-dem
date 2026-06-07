using UnityEngine;
using UnityEngine.UIElements;

// Menu de pausa en UI Toolkit. Se muestra/oculta segun el evento GameManager.PauseChanged
// y cablea los botones a las acciones del GameManager. Va en un GameObject con UIDocument
// (PauseMenu_UITK), con sortOrder mayor que el HUD para quedar por encima.
[RequireComponent(typeof(UIDocument))]
public class PauseMenuController : MonoBehaviour
{
    private VisualElement root;

    void OnEnable()
    {
        GameManager.PauseChanged += OnPauseChanged;
    }

    void OnDisable()
    {
        GameManager.PauseChanged -= OnPauseChanged;
    }

    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        Bind("btn-resume", () => GameManager.Instance?.Resume());
        Bind("btn-restart", () => GameManager.Instance?.RestartGame());
        Bind("btn-quit", () => GameManager.Instance?.QuitGame());

        Hide();   // oculto al empezar (solo aparece al pausar)
    }

    void Bind(string name, System.Action action)
    {
        var btn = root.Q<Button>(name);
        if (btn != null) btn.clicked += action;
    }

    void OnPauseChanged(bool paused)
    {
        if (root == null) return;
        if (paused) Show(); else Hide();
    }

    void Show() { root.style.display = DisplayStyle.Flex; }
    void Hide() { root.style.display = DisplayStyle.None; }
}
