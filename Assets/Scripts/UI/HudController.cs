using UnityEngine;
using UnityEngine.UIElements;

namespace ShooterDem
{
// Controlador del HUD en UI Toolkit. Consulta los elementos del UXML por nombre y
// los actualiza reaccionando a los EVENTOS del juego (mismo patron observador que
// el resto del proyecto): vida, municion, oleada y arma actual. Va en el GameObject
// con el UIDocument (HUD_UITK).
[RequireComponent(typeof(UIDocument))]
public class HudController : MonoBehaviour
{
    [Header("Fuentes de datos")]
    public PlayerHealth playerHealth;
    public Weapon weapon;
    public WaveSystem waveSystem;
    public WeaponManager weaponManager;

    [Header("Feedback de dano")]
    public float playerHitShake = 0.35f;   // sacudida de camara al recibir dano

    private Label healthValue, ammoValue, weaponName, waveValue;
    private CrosshairArcs crosshair;   // arcos del reticle (vida/escudo/cargador/reserva)
    private Camera cam;                // para calcular la direccion del dano (cacheada)

    void OnEnable()
    {
        if (playerHealth != null) playerHealth.Damaged += OnHealthChanged;
        if (weapon != null) weapon.AmmoChanged += RefreshAmmo;
        if (weapon != null) weapon.Fired += OnFired;
        if (waveSystem != null) waveSystem.WaveChanged += OnWaveChanged;
        if (weaponManager != null) weaponManager.WeaponSwitched += OnWeaponSwitched;
        if (playerHealth != null) playerHealth.Hit += OnPlayerHit;
    }

    void OnDisable()
    {
        if (playerHealth != null) playerHealth.Damaged -= OnHealthChanged;
        if (weapon != null) weapon.AmmoChanged -= RefreshAmmo;
        if (weapon != null) weapon.Fired -= OnFired;
        if (waveSystem != null) waveSystem.WaveChanged -= OnWaveChanged;
        if (weaponManager != null) weaponManager.WeaponSwitched -= OnWeaponSwitched;
        if (playerHealth != null) playerHealth.Hit -= OnPlayerHit;
    }

    // Al recibir dano: arco rojo apuntando al origen + sacudida de camara.
    void OnPlayerHit(Vector3 source)
    {
        if (cam == null) cam = Camera.main;
        if (cam != null && crosshair != null)
        {
            Vector3 to = source - cam.transform.position; to.y = 0f;
            Vector3 fwd = cam.transform.forward; fwd.y = 0f;
            if (to.sqrMagnitude > 0.0001f && fwd.sqrMagnitude > 0.0001f)
                crosshair.AddDamage(Vector3.SignedAngle(fwd, to, Vector3.up));  // 0=frente, +=derecha
        }
        CameraShake.Add(playerHitShake);
    }

    void OnFired()
    {
        if (crosshair != null) crosshair.Kick();   // los arcos se "abren" al disparar
    }

    void Start()
    {
        // El UIDocument construye su arbol en su OnEnable; Start corre despues, asi
        // que aqui rootVisualElement ya esta listo para consultar.
        var root = GetComponent<UIDocument>().rootVisualElement;
        healthValue = root.Q<Label>("health-value");
        ammoValue = root.Q<Label>("ammo-value");
        weaponName = root.Q<Label>("weapon-name");
        waveValue = root.Q<Label>("wave-value");

        // Reticle con arcos: lo creamos por codigo y lo anadimos como overlay.
        crosshair = new CrosshairArcs();
        root.Add(crosshair);
        crosshair.Shield = 1f;    // placeholder hasta que exista el sistema de escudo
        crosshair.Reserve = 1f;   // placeholder hasta que exista la municion de reserva

        RefreshHealth();
        RefreshAmmo();
        RefreshWave();
        if (weaponManager != null) SetWeaponName(weaponManager.CurrentWeapon);
    }

    void OnHealthChanged(int current, int max) => RefreshHealth();

    void RefreshHealth()
    {
        if (playerHealth == null) return;

        int cur = playerHealth.CurrentHealth, max = playerHealth.maxHealth;
        float pct = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;

        if (healthValue != null) healthValue.text = cur.ToString();
        if (crosshair != null) crosshair.Health = pct;   // arco de vida del reticle
    }

    void RefreshAmmo()
    {
        if (weapon == null) return;

        // El numero grande SIEMPRE muestra cifras (no texto), para que su ancho no
        // cambie y la caja no "salte".
        if (ammoValue != null)
            ammoValue.text = $"{weapon.CurrentAmmo} / {weapon.magazineSize}";

        // Arco de cargador + spinner de recarga en el reticle.
        if (crosshair != null)
        {
            crosshair.Magazine = weapon.magazineSize > 0
                ? Mathf.Clamp01((float)weapon.CurrentAmmo / weapon.magazineSize) : 0f;
            crosshair.Reloading = weapon.IsReloading;   // al recargar: circulo girando
        }
    }

    void OnWaveChanged(int wave) => RefreshWave();

    void RefreshWave()
    {
        if (waveSystem == null || waveValue == null) return;
        int w = waveSystem.CurrentWave;
        waveValue.text = w <= 0
            ? "PREPARATE"
            : (waveSystem.totalWaves > 0 ? $"OLEADA {w}/{waveSystem.totalWaves}" : $"OLEADA {w}");
    }

    void OnWeaponSwitched(WeaponData data) => SetWeaponName(data);

    void SetWeaponName(WeaponData data)
    {
        if (data == null) return;
        if (weaponName != null) weaponName.text = data.weaponName.ToUpper();
        if (crosshair != null)
        {
            crosshair.Reticle = data.crosshairStyle;          // mira por arma
            crosshair.CircleRadius = data.spreadAngle * 3f;   // el circulo refleja la dispersion
        }
    }
}
}
