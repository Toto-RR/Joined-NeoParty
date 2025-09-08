#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class FindMissingReferences
{
    [MenuItem("Tools/Diagnostics/Scan Scene For Missing Scripts")]
    public static void ScanScene()
    {
        int goCount = 0, compCount = 0, missingCount = 0;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            goCount++;
            var comps = go.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                compCount++;
                if (comps[i] == null)
                {
                    missingCount++;
                    Debug.LogWarning($"[Missing Script] GameObject: {GetPath(go)}", go);
                }
            }
        }
        Debug.Log($"Scan done. GameObjects: {goCount}, Components: {compCount}, Missing: {missingCount}");
    }

    private static string GetPath(GameObject obj)
    {
        var stack = new Stack<string>();
        var t = obj.transform;
        while (t != null) { stack.Push(t.name); t = t.parent; }
        return string.Join("/", stack);
    }
}
#endif
