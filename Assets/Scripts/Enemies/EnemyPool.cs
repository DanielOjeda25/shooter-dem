using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // NavMeshAgent.Warp

// Pool de enemigos: recicla GameObjects en vez de Instantiate/Destroy por oleada.
// En hordas, crear y destruir cientos de objetos dispara el recolector de basura
// (GC -> tirones de frame) y repite inicializaciones. Aqui los enemigos muertos se
// DESACTIVAN y vuelven a una cola; al pedir uno nuevo, se reutiliza si lo hay.
//
// Es una clase normal (no MonoBehaviour): la crea y posee el EnemySpawner. Asi no
// hay nada que cablear en el editor.
public class EnemyPool
{
    private readonly GameObject prefab;
    private readonly Transform parent;                 // donde "cuelgan" los inactivos
    private readonly Queue<EnemyHealth> idle = new Queue<EnemyHealth>();

    public EnemyPool(GameObject prefab, Transform parent)
    {
        this.prefab = prefab;
        this.parent = parent;
    }

    // Da un enemigo listo en (pos, rot): reutiliza uno inactivo o crea uno nuevo.
    public EnemyHealth Get(Vector3 pos, Quaternion rot)
    {
        EnemyHealth enemy;

        if (idle.Count > 0)
        {
            enemy = idle.Dequeue();

            // Colocamos ANTES de activar: con el NavMeshAgent desactivado, mover el
            // transform es seguro (con el agente activo, Unity lo ignoraria).
            enemy.transform.SetPositionAndRotation(pos, rot);
            enemy.gameObject.SetActive(true);          // dispara OnEnable: revive + "Spawned"

            // Teleporte oficial del agente a la nueva posicion (re-engancha al NavMesh).
            if (enemy.TryGetComponent(out NavMeshAgent agent) && agent.isActiveAndEnabled)
            {
                agent.Warp(pos);
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;   // limpia un "stop" heredado (murio durante un knockback)
                    agent.ResetPath();         // descarta la ruta de su vida anterior
                }
            }
        }
        else
        {
            // Pool vacio: creamos uno. Instantiate ya dispara su OnEnable ("Spawned").
            enemy = Object.Instantiate(prefab, pos, rot, parent).GetComponent<EnemyHealth>();
        }

        enemy.SetPool(this);                           // para que sepa volver aqui al morir
        return enemy;
    }

    // Devuelve un enemigo al pool: lo desactiva y lo guarda para reusar.
    public void Return(EnemyHealth enemy)
    {
        enemy.gameObject.SetActive(false);
        idle.Enqueue(enemy);
    }
}
