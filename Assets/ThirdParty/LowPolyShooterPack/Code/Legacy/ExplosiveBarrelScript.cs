using UnityEngine;
using System.Collections;

public class ExplosiveBarrelScript : MonoBehaviour {

	float randomTime;
	bool routineStarted = false;

	//Used to check if the barrel 
	//has been hit and should explode 
	public bool explode = false;

	[Header("Prefabs")]
	//The explosion prefab
	public Transform explosionPrefab;
	//The destroyed barrel prefab
	public Transform destroyedBarrelPrefab;

	[Header("Customizable Options")]
	//Minimum time before the barrel explodes
	public float minTime = 0.05f;
	//Maximum time before the barrel explodes
	public float maxTime = 0.25f;

	[Header("Explosion Options")]
	//How far the explosion will reach
	public float explosionRadius = 12.5f;
	//How powerful the explosion is
	public float explosionForce = 4000.0f;
	//ASHFALL: dano de la explosion (cae con la distancia hasta 0 en el borde del radio)
	public int explosionDamage = 80;
	//ASHFALL: empuje hacia arriba (te lanza en alto -> "mini caida" al caer). Subilo para mas vuelo.
	public float explosionUpwardsModifier = 0.5f;
	//ASHFALL: knockback al JUGADOR (la fisica lo controla un instante -> vuelo + caida). Tuneable.
	public float knockbackHorizontal = 8f;   // empuje hacia atras (m/s) a quemarropa
	public float knockbackVertical = 6f;     // empuje hacia arriba (m/s) a quemarropa
	public float knockbackDuration = 0.45f;  // segundos sin control (mientras vuela/cae)

	private void Update () {
		//Generate random time based on min and max time values
		randomTime = Random.Range (minTime, maxTime);

		//If the barrel is hit
		if (explode == true) 
		{
			if (routineStarted == false) 
			{
				//Start the explode coroutine
				StartCoroutine(Explode());
				routineStarted = true;
			} 
		}
	}
	
	private IEnumerator Explode () {
		//Wait for set amount of time
		yield return new WaitForSeconds(randomTime);

		//Spawn the destroyed barrel prefab
		Instantiate (destroyedBarrelPrefab, transform.position, 
		             transform.rotation); 

		//Explosion force
		Vector3 explosionPos = transform.position;
		Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius);
		//ASHFALL: evita danar dos veces al mismo objetivo (un enemigo puede tener varios colliders)
		var ashfallDamaged = new System.Collections.Generic.HashSet<ShooterDem.IDamageable>();
		foreach (Collider hit in colliders) {
			Rigidbody rb = hit.GetComponent<Rigidbody> ();

			//Add force to nearby rigidbodies
			//ASHFALL: al JUGADOR no le aplicamos la fuerza bruta (recibe el knockback controlado mas
			//abajo). Si se la aplicaramos, se sumaria al knockback y lo mandaria fuera del mapa.
			if (rb != null && rb.GetComponent<InfimaGames.LowPolyShooterPack.Movement>() == null)
				rb.AddExplosionForce (explosionForce * 50, explosionPos, explosionRadius, explosionUpwardsModifier);

			//ASHFALL: dano por explosion a enemigos Y jugador (ambos son IDamageable), con caida por distancia.
			var ashfallDmg = hit.GetComponentInParent<ShooterDem.IDamageable>();
			if (ashfallDmg != null && ashfallDamaged.Add(ashfallDmg))
			{
				float dist = Vector3.Distance(explosionPos, hit.ClosestPoint(explosionPos));
				int dealt = Mathf.RoundToInt(explosionDamage * Mathf.Clamp01(1f - dist / explosionRadius));
				if (dealt > 0)
				{
					ashfallDmg.TakeDamage(dealt);
					//Si fue el jugador, registramos la direccion (indicador de dano + shake).
					var ashfallPlayer = ashfallDmg as ShooterDem.PlayerHealth;
					if (ashfallPlayer != null)
					{
						ashfallPlayer.RegisterHit(explosionPos);
						//ASHFALL: knockback real -> el Movement cede el control a la fisica (vuelo + caida).
						var ashfallMove = ashfallPlayer.GetComponent<InfimaGames.LowPolyShooterPack.Movement>();
						if (ashfallMove != null)
						{
							Vector3 away = ashfallPlayer.transform.position - explosionPos;
							away.y = 0f;
							away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.zero;
							float falloff = Mathf.Clamp01(1f - dist / explosionRadius);
							ashfallMove.ApplyKnockback((away * knockbackHorizontal + Vector3.up * knockbackVertical) * falloff, knockbackDuration);
						}
					}
				}
			}

			//If the barrel explosion hits other barrels with the tag "ExplosiveBarrel"
			if (hit.transform.tag == "ExplosiveBarrel") 
			{
				//Toggle the explode bool on the explosive barrel object
				hit.transform.gameObject.GetComponent<ExplosiveBarrelScript>().explode = true;
			}
				
			//If the explosion hit the tag "Target"
			if (hit.transform.tag == "Target") 
			{
				//Toggle the isHit bool on the target object
				hit.transform.gameObject.GetComponent<TargetScript>().isHit = true;
			}

			//If the explosion hit the tag "GasTank"
			if (hit.GetComponent<Collider>().tag == "GasTank") 
			{
				//If gas tank is within radius, explode it
				hit.gameObject.GetComponent<GasTankScript> ().isHit = true;
				hit.gameObject.GetComponent<GasTankScript> ().explosionTimer = 0.05f;
			}
		}

		//Raycast downwards to check the ground tag
		RaycastHit checkGround;
		if (Physics.Raycast(transform.position, Vector3.down, out checkGround, 50))
		{
			//Instantiate explosion prefab at hit position
			Instantiate (explosionPrefab, checkGround.point, 
				Quaternion.FromToRotation (Vector3.forward, checkGround.normal)); 
		}

		//Destroy the current barrel object
		Destroy (gameObject);
	}
}