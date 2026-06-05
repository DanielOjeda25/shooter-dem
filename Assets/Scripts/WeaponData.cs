using UnityEngine;

// "Ficha" de un arma (datos, no comportamiento). Cada arma del arsenal sera un
// asset distinto (Pistola, Escopeta, Bazooka...) sin escribir codigo nuevo: solo
// se rellenan estos valores. Weapon lee de aqui en runtime.
//
// CreateAssetMenu permite crear instancias desde: Assets > Create > Shooter > Weapon Data.
[CreateAssetMenu(fileName = "WeaponData", menuName = "Shooter/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identidad")]
    public string weaponName = "Arma";

    [Header("Disparo")]
    public float range = 100f;        // alcance del rayo en metros

    [Header("Dano (con caida por distancia)")]
    public int damage = 25;           // dano a quemarropa (hasta falloffStart)
    public int minDamage = 8;         // dano minimo (desde falloffEnd en adelante)
    public float falloffStart = 15f;  // hasta esta distancia, dano completo
    public float falloffEnd = 60f;    // desde esta distancia, solo minDamage

    [Header("Municion")]
    public int magazineSize = 12;     // balas por cargador
    public float reloadTime = 1.5f;   // segundos de recarga
}
