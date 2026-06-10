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

    [Header("Hitmarker")]
    public AudioClip hitmarkerClip;        // tic 2D al confirmar impacto en un enemigo
    private AudioSource hitAudio;          // fuente 2D (se crea sola en Start)
    // Anti-spam del tic por TIEMPO (antes era un flag re-armado por weapon.Fired, pero el
    // arma del pack no dispara ese evento): N impactos casi simultaneos = 1 solo tic.
    private float lastHitmarkerTime;
    const float HitmarkerSoundCooldown = 0.08f;

    private Label healthValue, ammoValue, weaponName, waveValue;
    private VisualElement staminaFill;   // barra de stamina (sprint/dash)
    private VisualElement ammoPanel;     // caja de municion (se oculta en modo desarmado)
    private CrosshairArcs crosshair;   // arcos del reticle (vida/escudo/cargador/reserva)
    private DamageVignette vignette;   // bordes rojos al recibir dano / vida baja
    private Camera cam;                // para calcular la direccion del dano (cacheada)
    private PlayerMovement playerMovement;  // para mostrar la stamina (sprint/dash)

    void OnEnable()
    {
        if (playerHealth != null) playerHealth.Damaged += OnHealthChanged;
        if (weapon != null) weapon.AmmoChanged += RefreshAmmo;
        if (weapon != null) weapon.Fired += OnFired;
        if (weapon != null) weapon.Hit += OnWeaponHit;
        if (waveSystem != null) waveSystem.WaveChanged += OnWaveChanged;
        if (weaponManager != null) weaponManager.WeaponSwitched += OnWeaponSwitched;
        if (playerHealth != null) playerHealth.Hit += OnPlayerHit;
        // Balas del pack (Camino A): bus estatico, no necesita referencia en el Inspector.
        LpspBulletDamage.HitConfirmed += OnHitConfirmed;
    }

    void OnDisable()
    {
        if (playerHealth != null) playerHealth.Damaged -= OnHealthChanged;
        if (weapon != null) weapon.AmmoChanged -= RefreshAmmo;
        if (weapon != null) weapon.Fired -= OnFired;
        if (weapon != null) weapon.Hit -= OnWeaponHit;
        if (waveSystem != null) waveSystem.WaveChanged -= OnWaveChanged;
        if (weaponManager != null) weaponManager.WeaponSwitched -= OnWeaponSwitched;
        if (playerHealth != null) playerHealth.Hit -= OnPlayerHit;
        LpspBulletDamage.HitConfirmed -= OnHitConfirmed;
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
        if (vignette != null) vignette.Pulse();   // borde rojo al recibir dano
        CameraShake.Add(playerHitShake);
    }

    void OnFired()
    {
        if (crosshair != null) crosshair.Kick();   // los arcos se "abren" al disparar
    }

    // Camino legado (Weapon por raycast): delega en el mismo confirmador.
    void OnWeaponHit(RaycastHit hit, bool hitDamageable)
    {
        if (hitDamageable) OnHitConfirmed();
    }

    // Impacto confirmado sobre un enemigo (cualquier camino: raycast legado o bala del
    // pack): X en la mira + tic 2D. La X puede repetirse sin coste; el SONIDO respeta un
    // cooldown corto (varios perdigones/balas casi simultaneos = 1 tic, no una rafaga).
    void OnHitConfirmed()
    {
        if (crosshair != null) crosshair.Hitmarker();
        if (hitmarkerClip != null && hitAudio != null
            && Time.time >= lastHitmarkerTime + HitmarkerSoundCooldown)
        {
            hitAudio.PlayOneShot(hitmarkerClip);
            lastHitmarkerTime = Time.time;
        }
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
        staminaFill = root.Q<VisualElement>("stamina-fill");
        ammoPanel = root.Q<VisualElement>("ammo-panel");
        // Estado inicial segun el modo desarmado (independiente del orden de Start).
        var unarmedMode = FindFirstObjectByType<UnarmedMode>();
        if (unarmedMode != null) SetAmmoPanelVisible(!unarmedMode.unarmed);

        // Vineta de dano: overlay DETRAS del texto del HUD (Insert(0)).
        vignette = new DamageVignette();
        root.Insert(0, vignette);

        // Overlay de feedback de combate (dano direccional + X de hitmarker). Los ARCOS
        // ya no se dibujan (la mira/municion las pone el pack; la vida, la caja SALUD).
        crosshair = new CrosshairArcs();
        root.Add(crosshair);
        // El arco superior-izquierdo (antes "escudo") muestra la STAMINA (sprint/dash).
        playerMovement = playerHealth != null ? playerHealth.GetComponent<PlayerMovement>() : null;
        crosshair.Shield = playerMovement != null ? playerMovement.Stamina01 : 1f;
        crosshair.Reserve = 1f;   // placeholder hasta que exista la municion de reserva

        // Fuente 2D para el hitmarker (la creamos si el GameObject no tiene AudioSource).
        hitAudio = GetComponent<AudioSource>();
        if (hitAudio == null) hitAudio = gameObject.AddComponent<AudioSource>();
        hitAudio.playOnAwake = false;
        hitAudio.spatialBlend = 0f;   // 2D: feedback directo para el jugador

        RefreshHealth();
        RefreshAmmo();
        RefreshWave();
        if (weaponManager != null) SetWeaponName(weaponManager.CurrentWeapon);
    }

    void Update()
    {
        // La stamina y las cargas de dash cambian continuamente -> se refrescan cada frame.
        if (crosshair != null && playerMovement != null)
            crosshair.Shield = playerMovement.Stamina01;   // arco sup-izq = stamina (sprint+dash)
        // La mira se abre segun la dispersion actual del arma (preciso parado, abierto al moverse).
        if (crosshair != null && weapon != null)
            crosshair.Bloom = weapon.CurrentSpread * 2.5f;
    }

    void OnHealthChanged(int current, int max) => RefreshHealth();

    void RefreshHealth()
    {
        if (playerHealth == null) return;

        int cur = playerHealth.CurrentHealth, max = playerHealth.maxHealth;
        float pct = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;

        if (healthValue != null) healthValue.text = cur.ToString();
        if (crosshair != null) crosshair.Health = pct;   // arco de vida del reticle
        // Tinte rojo persistente: empieza por debajo del 50% de vida, maximo al borde de morir.
        if (vignette != null) vignette.LowHealth = 1f - Mathf.Clamp01(pct / 0.5f);
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

    // Munición desde una fuente EXTERNA (arma del Low Poly Shooter Pack), via LpspHudAmmo.
    // Nuestro `Weapon` quedó solo para el viejo arsenal; el player del pack usa su propia arma.
    public void SetAmmoExternal(int current, int magazine)
    {
        if (ammoValue != null) ammoValue.text = $"{current} / {magazine}";
        if (crosshair != null)
            crosshair.Magazine = magazine > 0 ? Mathf.Clamp01((float)current / magazine) : 0f;
    }

    // Nombre del arma desde fuente externa (arma del pack).
    public void SetWeaponNameExternal(string name)
    {
        if (weaponName != null) weaponName.text = name;
    }

    // Mostrar/ocultar la caja de municion (modo desarmado: jugando con las manos).
    public void SetAmmoPanelVisible(bool visible)
    {
        if (ammoPanel != null)
            ammoPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // Stamina (0..1) desde fuente externa (el Movement del pack), via LpspHudAmmo.
    public void SetStamina01(float value)
    {
        if (staminaFill != null)
            staminaFill.style.width = Length.Percent(Mathf.Clamp01(value) * 100f);
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
