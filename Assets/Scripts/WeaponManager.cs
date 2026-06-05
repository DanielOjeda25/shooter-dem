using System;
using UnityEngine;
using UnityEngine.InputSystem; // Nuevo Input System

// Inventario de armas: mantiene el arsenal (lista de WeaponData) y permite cambiar
// de arma con las teclas 1..9 o la rueda del raton. Reutiliza UN solo componente
// Weapon intercambiando su 'data', y recuerda la MUNICION de cada arma por separado.
// Va en el mismo GameObject que Weapon (o en el Player; solo necesita la referencia).
public class WeaponManager : MonoBehaviour
{
    [Header("Arsenal")]
    public Weapon weapon;             // el arma fisica (su data se intercambia)
    public WeaponData[] weapons;      // fichas disponibles (slot 1, 2, 3...)

    private int[] ammo;               // municion actual por arma (paralelo a weapons)
    private int index;                // arma equipada ahora

    public int CurrentIndex => index;
    public WeaponData CurrentWeapon =>
        (weapons != null && index >= 0 && index < weapons.Length) ? weapons[index] : null;
    public event Action<WeaponData> WeaponSwitched;  // para el HUD a futuro

    void Awake()
    {
        // Avisamos al arma de que la gestionamos nosotros (no se auto-inicializa).
        if (weapon != null) weapon.externallyManaged = true;
    }

    void Start()
    {
        if (weapon == null || weapons == null || weapons.Length == 0)
        {
            Debug.LogError("WeaponManager: asigna 'weapon' y al menos un WeaponData en 'weapons'.");
            enabled = false;
            return;
        }

        // Cada arma empieza con su cargador lleno.
        ammo = new int[weapons.Length];
        for (int i = 0; i < weapons.Length; i++)
            ammo[i] = weapons[i] != null ? weapons[i].magazineSize : 0;

        index = 0;
        ApplyEquip();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;   // en pausa/game over no se cambia de arma

        // Teclas 1..9 -> slot directo.
        var kb = Keyboard.current;
        if (kb != null)
        {
            int n = Mathf.Min(weapons.Length, 9);
            for (int i = 0; i < n; i++)
                if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                    Equip(i);
        }

        // Rueda del raton -> siguiente / anterior (circular).
        var mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0f) Equip((index + 1) % weapons.Length);
            else if (scroll < 0f) Equip((index - 1 + weapons.Length) % weapons.Length);
        }
    }

    // Cambia al arma del slot i (si existe y no es la actual).
    public void Equip(int i)
    {
        if (i == index || i < 0 || i >= weapons.Length || weapons[i] == null) return;

        ammo[index] = weapon.CurrentAmmo;   // guardamos la municion del arma que dejamos
        index = i;
        ApplyEquip();
    }

    void ApplyEquip()
    {
        weapon.Equip(weapons[index], ammo[index]);
        WeaponSwitched?.Invoke(weapons[index]);
        Debug.Log($"Arma equipada: {weapons[index].weaponName} ({ammo[index]}/{weapons[index].magazineSize})");
    }
}
