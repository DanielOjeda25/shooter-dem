using System.Collections;
using UnityEngine;

// Sonidos del arma. Escucha los eventos de Weapon y reproduce los clips (2D).
// Va en el mismo GameObject que Weapon. Requiere un AudioSource (el "altavoz").
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Weapon))]
public class WeaponAudio : MonoBehaviour
{
    // Disparo / recarga / vacio son PROPIOS de cada arma: viven en weapon.data,
    // asi cada arma del arsenal suena distinto sin tocar este componente.
    // El impacto depende de la SUPERFICIE (carne vs pared), no del arma -> aqui, compartido.
    [Header("Impacto (compartido, segun superficie)")]
    public AudioClip[] concreteImpactClips; // golpe en pared/suelo
    public AudioClip[] fleshImpactClips;    // golpe en enemigo

    [Header("Casquillo (compartido, generico)")]
    public AudioClip[] shellClips;          // la vaina cae y rebota tras disparar
    public float shellDelay = 0.35f;        // tarda un poco en tocar el suelo

    private Weapon weapon;
    private AudioSource audioSource;
    private int lastImpactFrame = -1;   // para no solapar 8 sonidos en un disparo de escopeta

    void Awake()
    {
        weapon = GetComponent<Weapon>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void OnEnable()
    {
        weapon.Fired += HandleFired;
        weapon.DryFired += HandleDryFired;
        weapon.ReloadStarted += HandleReload;
        weapon.Hit += HandleHit;
    }

    void OnDisable()
    {
        weapon.Fired -= HandleFired;
        weapon.DryFired -= HandleDryFired;
        weapon.ReloadStarted -= HandleReload;
        weapon.Hit -= HandleHit;
    }

    void HandleFired()
    {
        PlayRandom(weapon.data != null ? weapon.data.fireClips : null);
        // Casquillo solo si el arma lo expulsa (la bazooka no).
        if (weapon.data != null && weapon.data.ejectsShell && shellClips != null && shellClips.Length > 0)
            StartCoroutine(PlayShellDelayed());
    }

    IEnumerator PlayShellDelayed()
    {
        yield return new WaitForSeconds(shellDelay);
        PlayRandom(shellClips);
    }

    void HandleDryFired()
    {
        var clip = weapon.data != null ? weapon.data.emptyClip : null;
        if (clip != null) audioSource.PlayOneShot(clip);
    }

    void HandleReload() => PlayRandom(weapon.data != null ? weapon.data.reloadClips : null);

    // Carne si golpeamos algo danable (un enemigo); pared si no. Un solo sonido de
    // impacto por disparo: la escopeta lanza N perdigones en el MISMO frame y no
    // queremos N sonidos solapados.
    void HandleHit(RaycastHit hit, bool hitDamageable)
    {
        if (Time.frameCount == lastImpactFrame) return;
        lastImpactFrame = Time.frameCount;
        PlayRandom(hitDamageable ? fleshImpactClips : concreteImpactClips);
    }

    // PlayOneShot permite que varios sonidos se solapen sin cortarse.
    void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }
}
