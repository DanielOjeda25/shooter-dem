using System.Collections.Generic;
using UnityEngine;

// Pool GENERICO de GameObjects a partir de un prefab. Igual idea que EnemyPool pero
// sin logica de enemigo: sirve para efectos (chispas, marcas) o cualquier cosa que
// se cree y destruya mucho. Reutiliza objetos inactivos en vez de Instantiate/Destroy.
//
// Es una clase normal (no MonoBehaviour): la crea y posee quien la necesite.
public class PrefabPool
{
    private readonly GameObject prefab;
    private readonly Transform parent;                 // donde "cuelgan" los inactivos
    private readonly Queue<GameObject> idle = new Queue<GameObject>();

    public PrefabPool(GameObject prefab, Transform parent)
    {
        this.prefab = prefab;
        this.parent = parent;
    }

    // Da un objeto listo en (pos, rot): reutiliza uno inactivo o crea uno nuevo.
    public GameObject Get(Vector3 pos, Quaternion rot)
    {
        GameObject go;

        if (idle.Count > 0)
        {
            go = idle.Dequeue();
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, rot);
            go.SetActive(true);
        }
        else
        {
            go = Object.Instantiate(prefab, pos, rot, parent);
        }

        return go;
    }

    // Devuelve un objeto al pool: lo desactiva, lo re-cuelga del contenedor y lo encola.
    // Tolerante a objetos ya destruidos (p. ej. si su padre fue destruido): los descarta.
    public void Return(GameObject go)
    {
        if (go == null) return;                        // (Unity: destruido == null)
        go.transform.SetParent(parent, false);
        go.SetActive(false);
        idle.Enqueue(go);
    }
}
