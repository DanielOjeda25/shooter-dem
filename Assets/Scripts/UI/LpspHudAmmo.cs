using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace ShooterDem
{
    /// <summary>
    /// PUENTE (Camino A): muestra la munición del arma equipada del Low Poly Shooter Pack
    /// en NUESTRO <see cref="HudController"/>. Nuestro HUD leía nuestro `Weapon` (retirado del
    /// player), así que esto cierra el hueco: cada frame toma el arma equipada del Character
    /// del pack (vía su GameModeService) y la vuelca al HUD. Va en el player del pack.
    /// </summary>
    public class LpspHudAmmo : MonoBehaviour
    {
        private HudController hud;
        private CharacterBehaviour character;
        private WeaponBehaviour lastWeapon;
        private Movement movement;   // para la stamina (sprint/dash)

        void Start()
        {
            hud = FindFirstObjectByType<HudController>();
            TryGetCharacter();
        }

        void TryGetCharacter()
        {
            if (ServiceLocator.Current == null) return;
            var gms = ServiceLocator.Current.Get<IGameModeService>();
            character = gms != null ? gms.GetPlayerCharacter() : null;
        }

        void Update()
        {
            if (hud == null) return;
            if (character == null) { TryGetCharacter(); if (character == null) return; }

            // Stamina (sprint/dash) -> barra del HUD.
            if (movement == null) movement = character.GetComponent<Movement>();
            if (movement != null) hud.SetStamina01(movement.Stamina01);

            var inventory = character.GetInventory();
            var weapon = inventory != null ? inventory.GetEquipped() : null;
            if (weapon == null) return;

            // Munición: current / capacidad del cargador.
            hud.SetAmmoExternal(weapon.GetAmmunitionCurrent(), weapon.GetAmmunitionTotal());

            // Nombre del arma: solo cuando cambia.
            if (weapon != lastWeapon)
            {
                lastWeapon = weapon;
                hud.SetWeaponNameExternal(FriendlyName(weapon.name));
            }
        }

        // Nombre lindo a partir del nombre del prefab del arma.
        private static string FriendlyName(string raw)
        {
            string n = raw.Replace("P_LPSP_WEP_", "").Replace("(Clone)", "").Trim();
            n = System.Text.RegularExpressions.Regex.Replace(n, "_\\d+$", ""); // quita "_03"
            if (n.Contains("Handgun")) return "Pistola";
            if (n.StartsWith("AR") || n.Contains("Rifle")) return "Rifle";
            return n;
        }
    }
}
