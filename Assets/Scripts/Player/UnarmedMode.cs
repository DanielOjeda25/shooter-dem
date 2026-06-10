using UnityEngine;
using UnityEngine.InputSystem;

namespace ShooterDem
{
    /// <summary>
    /// MODO DESARMADO (transición a los brazos propios): bloquea las acciones de ARMA
    /// del pack (disparar, apuntar, recargar, inspeccionar, cambiar arma) dejando intactas
    /// las de movimiento (mover, mirar, saltar, correr). Reversible: destildar `unarmed`
    /// (o desactivar este componente) re-habilita todo.
    /// La retícula del pack se quita aparte (CanvasSpawner desactivado en el player).
    /// Va en el GameObject raíz del player.
    /// </summary>
    public class UnarmedMode : MonoBehaviour
    {
        [Tooltip("ON = sin disparo/apuntar/recarga (jugando con las manos).")]
        public bool unarmed = true;

        // Acciones del IA_Player del pack que pertenecen al ARMA.
        private static readonly string[] WeaponActions =
            { "Fire", "Aim", "Reload", "Inspect", "Holster", "Inventory Next", "Inventory Next Wheel" };

        private PlayerInput playerInput;

        void Start()   // Start: después de que PlayerInput inicialice sus acciones
        {
            playerInput = GetComponentInChildren<PlayerInput>(true);
            Apply();
        }

        void OnValidate() { if (Application.isPlaying && playerInput != null) Apply(); }

        public void Apply()
        {
            if (playerInput != null)
            {
                foreach (var name in WeaponActions)
                {
                    var action = playerInput.actions.FindAction(name);
                    if (action == null) continue;
                    if (unarmed) action.Disable();
                    else action.Enable();
                }
            }

            // HUD: sin arma no hay caja de municion.
            var hud = FindFirstObjectByType<HudController>();
            if (hud != null) hud.SetAmmoPanelVisible(!unarmed);
        }
    }
}
