using UnityEngine;

public class Obstacle : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Jugador impact� con un obst�culo!");
            other.gameObject.SetActive(false);
        }
    }
}
