using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public Transform[] carriles;
    public float spawnInterval = 2f;
    private float timer = 0f;

    private int obstaclesSpawned = 0;
    public GameObject metaPrefab;
    public int obstaclesToSpawnForGoal = 10;
    private bool goalSpawned = false;
    private int lastLaneIndex = -1;

    void Start()
    {
        enabled = false;
    }

    void Update()
    {
        if (goalSpawned) return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnObstacle();
            obstaclesSpawned++;
            CheckSpawnGoalByObstacleCount();
        }
    }

    void SpawnObstacle()
    {
        int carril;
        do
        {
            carril = Random.Range(0, carriles.Length);
        } while (carril == lastLaneIndex && carriles.Length > 1);

        lastLaneIndex = carril;

        Transform targetLane = carriles[carril];

        Vector3 spawnPos = transform.position + new Vector3(0, 5, 0); // posición del spawner
        spawnPos.x = targetLane.position.x - 0.5f;
        Quaternion spawnRot = obstaclePrefab.transform.rotation;

        GameObject newObstacle = Instantiate(obstaclePrefab, spawnPos, spawnRot, gameObject.transform);
        var controller = newObstacle.GetComponent<ObstacleMover>();
        if (controller != null)
        {
            //controller.SetSpeed(obstaclesSpawned);
        }
    }

    void CheckSpawnGoalByObstacleCount()
    {
        if (!goalSpawned && obstaclesSpawned >= obstaclesToSpawnForGoal)
        {
            StartGoalSpawnCountdown();
        }
    }

    void CheckSpawnGoalByTime()
    {
        if (!goalSpawned && Time.timeSinceLevelLoad >= 60f)
        {
            StartGoalSpawnCountdown();
        }
    }

    System.Collections.IEnumerator SpawnGoalWithDelay()
    {
        yield return new WaitForSeconds(1f);
        SpawnGoal();
    }

    void StartGoalSpawnCountdown()
    {
        goalSpawned = true; // Marcarlo ya para que no se sigan generando obstáculos
        StartCoroutine(SpawnGoalWithDelay());
    }

    void SpawnGoal()
    {
        goalSpawned = true;

        // Calcular la posición central de los carriles
        float minX = carriles[0].position.x;
        float maxX = carriles[carriles.Length - 1].position.x;
        float centerX = (minX + maxX) / 2f;

        Vector3 goalPosition = transform.position + new Vector3(0, 5, 0);
        goalPosition.x = centerX;
        Quaternion spawnRot = metaPrefab.transform.rotation;

        Instantiate(metaPrefab, goalPosition, spawnRot, gameObject.transform);
    }

    float CalculateSpeed(int numObstaculos)
    {
        return 3f + (numObstaculos * 0.1f); // Ajustable
    }
}
