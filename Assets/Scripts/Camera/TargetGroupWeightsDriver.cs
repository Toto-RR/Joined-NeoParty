using Unity.Cinemachine;
using UnityEngine;

public class TargetGroupWeightsDriver : MonoBehaviour
{
    [Header("Asignar el Target Group y los dos objetivos (índices en el array)")]
    public CinemachineTargetGroup group;
    [Min(0)] public int indexA = 0;
    [Min(0)] public int indexB = 1;

    [Header("Parámetro único a animar (0 -> 1)")]
    [Range(0f, 1f)] public float t = 0f;

    void LateUpdate()
    {
        if (!group) return;

        // En CM3, Targets es un array de structs: hay que leer -> modificar -> re-asignar
        var targets = group.Targets;
        if (targets == null || targets.Count == 0) return;

        if (indexA < targets.Count) targets[indexA].Weight = 1f - t;
        if (indexB < targets.Count) targets[indexB].Weight = t;

        group.Targets = targets; // re-asignar para aplicar cambios
    }
}
