using System.Collections.Generic;
using UnityEngine;

/// Trigger que “anula” la colisión con el/los colliders del obstáculo mientras
/// el actor esté dentro. Robusto con zonas solapadas (ref-count global).
[DefaultExecutionOrder(50)]
public class SafeZone : MonoBehaviour
{
    [Tooltip("Colliders NO trigger del obstáculo a ignorar.")]
    public Collider[] obstacleColliders;

    [Tooltip("Capas de los actores (p.ej. Player).")]
    public LayerMask actorLayers = -1;

    // RefCount GLOBAL por par (actor, obstáculo)
    static readonly Dictionary<(Collider actor, Collider obstacle), int> _refCounts
        = new Dictionary<(Collider, Collider), int>();

    // Pares que esta instancia ha tocado (para cleanup al desactivar)
    readonly HashSet<(Collider actor, Collider obstacle)> _touchedByThis = new();

    bool LayerMatches(GameObject go) => ((actorLayers.value >> go.layer) & 1) == 1;

    void OnTriggerEnter(Collider other)
    {
        if (!LayerMatches(other.gameObject)) return;
        if (obstacleColliders == null || obstacleColliders.Length == 0) return;

        var actorColliders = other.GetComponentsInParent<Collider>(true);
        foreach (var ac in actorColliders)
        {
            if (!ac || ac.isTrigger) continue;

            foreach (var ob in obstacleColliders)
            {
                if (!ob) continue;

                var key = (ac, ob);
                // Incrementa refcount e ignora si era 0 -> 1
                if (_refCounts.TryGetValue(key, out int n))
                {
                    _refCounts[key] = n + 1;
                }
                else
                {
                    _refCounts[key] = 1;
                    Physics.IgnoreCollision(ac, ob, true);
                }
                _touchedByThis.Add(key);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!LayerMatches(other.gameObject)) return;
        if (obstacleColliders == null || obstacleColliders.Length == 0) return;

        var actorColliders = other.GetComponentsInParent<Collider>(true);
        foreach (var ac in actorColliders)
        {
            if (!ac || ac.isTrigger) continue;

            foreach (var ob in obstacleColliders)
            {
                if (!ob) continue;

                var key = (ac, ob);
                if (_refCounts.TryGetValue(key, out int n))
                {
                    n--;
                    if (n <= 0)
                    {
                        _refCounts.Remove(key);
                        Physics.IgnoreCollision(ac, ob, false);
                    }
                    else
                    {
                        _refCounts[key] = n;
                    }
                }
                _touchedByThis.Remove(key);
            }
        }
    }

    void OnDisable()
    {
        // Por seguridad: libera todo lo que esta instancia haya marcado
        foreach (var key in _touchedByThis)
        {
            if (_refCounts.TryGetValue(key, out int n))
            {
                n--;
                if (n <= 0)
                {
                    _refCounts.Remove(key);
                    if (key.actor && key.obstacle)
                        Physics.IgnoreCollision(key.actor, key.obstacle, false);
                }
                else
                {
                    _refCounts[key] = n;
                }
            }
        }
        _touchedByThis.Clear();
    }
}
