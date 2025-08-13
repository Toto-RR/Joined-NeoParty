using UnityEngine;
using UnityEditor;

public class DisableSelectedColliders
{
    [MenuItem("Tools/Disable Colliders in Selection")]
    static void DisableCollidersInSelection()
    {
        int count = 0;

        foreach (GameObject obj in Selection.gameObjects)
        {
            Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
            {
                col.enabled = false;
                count++;
            }
        }

        Debug.Log($"Se han desactivado {count} colliders en la selección.");
    }
}
