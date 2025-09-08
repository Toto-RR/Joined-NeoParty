using UnityEngine;

public class Obstacle : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Jugador impactó con un obstáculo!");
            other.gameObject.SetActive(false);
        }
    }
}
