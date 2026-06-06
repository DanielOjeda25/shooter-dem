using UnityEngine;

// Como dispara un arma. Lo lee Weapon para elegir la logica de disparo.
public enum FireType
{
    Single,      // un raycast recto (pistola, rifle)
    Shotgun,     // varios perdigones (raycasts) en un cono de dispersion
    Projectile   // lanza un proyectil que explota (dano en area) al impactar
}

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

    [Header("Forma de disparo")]
    public FireType fireType = FireType.Single;

    [Header("Disparo")]
    public float range = 100f;        // alcance del rayo en metros

    [Header("Dano (con caida por distancia)")]
    public int damage = 25;           // dano a quemarropa (hasta falloffStart)
    public int minDamage = 8;         // dano minimo (desde falloffEnd en adelante)
    public float falloffStart = 15f;  // hasta esta distancia, dano completo
    public float falloffEnd = 60f;    // desde esta distancia, solo minDamage

    [Header("Empuje al impactar (knockback)")]
    public float knockback = 8f;      // fuerza del empujon al enemigo (0 = nada)

    [Header("Escopeta (si fireType = Shotgun)")]
    public int pellets = 8;           // nº de perdigones por disparo
    public float spreadAngle = 6f;    // dispersion del cono, en grados

    [Header("Proyectil (si fireType = Projectile)")]
    public GameObject projectilePrefab; // debe tener Projectile + Rigidbody + Collider
    public float projectileSpeed = 35f; // velocidad de lanzamiento
    public float explosionRadius = 4f;  // radio de dano en area al impactar

    [Header("Municion")]
    public int magazineSize = 12;     // balas por cargador
    public float reloadTime = 1.5f;   // segundos de recarga

    [Header("Sonido (propio de cada arma)")]
    // Arrays = variantes; se elige una al azar para que no suene repetitivo.
    public AudioClip[] fireClips;     // disparo
    public AudioClip[] reloadClips;   // recarga
    public AudioClip emptyClip;       // clic sin municion
    // ¿Suelta casquillo al disparar? Pistola/escopeta sí; bazooka/lanzas no.
    public bool ejectsShell = true;
}
