using UnityEngine;

/// <summary>
/// Clase que maneja la meta de la carrera y env�a el trigger para detener movimiento y animaci�n
/// </summary>
public class EndRun : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Jugador ha alcanzado la meta!");
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.FinishReached();
            }
        }
    }
}
