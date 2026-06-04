using UnityEngine;
using TMPro; // TextMeshPro

// Muestra vida y municion en pantalla. Va en un GameObject del Canvas (o en el Canvas).
// Lee los datos de PlayerHealth y Weapon cada frame y los escribe en los textos.
public class HUD : MonoBehaviour
{
    [Header("Fuentes de datos")]
    public PlayerHealth playerHealth; // arrastra el Player
    public Weapon weapon;             // arrastra el Weapon

    [Header("Textos (TextMeshPro)")]
    public TMP_Text healthText;       // arrastra el texto de vida
    public TMP_Text ammoText;         // arrastra el texto de municion

    void Update()
    {
        if (playerHealth != null && healthText != null)
            healthText.text = $"VIDA  {playerHealth.CurrentHealth}/{playerHealth.maxHealth}";

        if (weapon != null && ammoText != null)
        {
            // Mientras recarga, mostramos un aviso en vez del numero.
            ammoText.text = weapon.IsReloading
                ? "RECARGANDO..."
                : $"MUNICION  {weapon.CurrentAmmo}/{weapon.magazineSize}";
        }
    }
}
