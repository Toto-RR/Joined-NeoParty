using UnityEngine;
using System.Collections;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public Transform obsSpawner;
    public Transform[] carriles;

    public float spawnInterval = 2f;
    public float moveSpeed = 2f;
    public float arriveTolerance = 0.01f;

    private int obstaclesSpawned = 0;
    public GameObject metaPrefab;
    public int obstaclesToSpawnForGoal = 10;
    private bool goalSpawned = false;
    private int lastLaneIndex = -1;
    private float laneOffset = 1.25f;

    void Start()
    {
        enabled = false;
    }

    private void OnEnable()
    {
        StartCoroutine(SpawnLoop());
    }

    public IEnumerator SpawnLoop()
    {
        while (!goalSpawned)
        {
            // Espera antes de empezar el siguiente spawn
            yield return new WaitForSeconds(spawnInterval);

            // Escoger carril distinto al anterior
            int carril;
            do
            {
                carril = Random.Range(0, carriles.Length);
            } while (carril == lastLaneIndex && carriles.Length > 1);

            lastLaneIndex = carril;
            Transform targetLane = carriles[carril];

            // Mover hasta el carril
            yield return StartCoroutine(MoveToLane(targetLane));

            // Spawnear obstáculo
            SpawnObstacle(targetLane);

            // Comprobar meta
            obstaclesSpawned++;
            if (obstaclesSpawned >= obstaclesToSpawnForGoal)
            {
                StartCoroutine(SpawnGoalWithDelay());
                goalSpawned = true;
            }
        }
    }

    IEnumerator MoveToLane(Transform targetLane, bool goal = false)
    {
        while (Mathf.Abs(obsSpawner.position.x - (targetLane.position.x - laneOffset)) > arriveTolerance)
        {
            float newX;

            if (!goal) //Utiliza el offset para poner cada obstaculo en el centro del carril
                newX = Mathf.MoveTowards(obsSpawner.position.x, targetLane.position.x - laneOffset, moveSpeed * Time.deltaTime);
            else // En este caso se quiere evitar ponerlo en el centro del carril
                newX = Mathf.MoveTowards(obsSpawner.position.x, targetLane.position.x, moveSpeed * Time.deltaTime);
            
            obsSpawner.position = new Vector3(newX, obsSpawner.position.y, obsSpawner.position.z);
            yield return null; // esperar siguiente frame
        }
    }

    void SpawnObstacle(Transform lane)
    {
        Debug.Log("Obstacle spawned");

        Vector3 spawnPos = obsSpawner.position + new Vector3(0, -2f, 0);
        spawnPos.x = lane.position.x - laneOffset;
        Quaternion spawnRot = obstaclePrefab.transform.rotation;

        Instantiate(obstaclePrefab, spawnPos, spawnRot, transform);
    }

    IEnumerator SpawnGoalWithDelay()
    {
        StartCoroutine(MoveToLane(carriles[1], true));

        yield return new WaitForSeconds(1f);

        Debug.Log("GOAL spawned");

        Vector3 goalPosition = transform.position;
        goalPosition.x = carriles[1].transform.position.x;
        Instantiate(metaPrefab, goalPosition, metaPrefab.transform.rotation, transform);
    }
}
