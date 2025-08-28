using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawnerFlying : MonoBehaviour
{
    public FrustumVolumeSpawner pointProvider;
    public List<GameObject> enemyPrefabs;
    public float spawnInterval = 1.0f;
    public int maxAlive = 18;
    public int triesPerSpawn = 20;

    private readonly List<GameObject> alive = new();
    private Coroutine loop;

    public void Begin()
    {
        StopSpawning();
        alive.RemoveAll(go => go == null);
        loop = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (loop != null) StopCoroutine(loop);
        loop = null;
    }

    private IEnumerator SpawnLoop()
    {
        var wfs = new WaitForSeconds(spawnInterval);
        while (true)
        {
            yield return wfs;
            alive.RemoveAll(go => go == null);
            if (alive.Count >= maxAlive) continue;

            if (pointProvider != null &&
                pointProvider.TryGetVisiblePoint(out var pos, triesPerSpawn))
            {
                var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
                var rot = Random.rotationUniform;
                var go = Instantiate(prefab, pos, rot);
                alive.Add(go);
            }
        }
    }
}
