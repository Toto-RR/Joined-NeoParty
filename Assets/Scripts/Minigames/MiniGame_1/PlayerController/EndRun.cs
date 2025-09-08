using UnityEngine;

/// <summary>
/// Clase que maneja la meta de la carrera y envía el trigger para detener movimiento y animación
/// </summary>
public class EndRun : MonoBehaviour
{
    private bool moving = false;
    public float speed = 30f;

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    void Update()
    {
        if (moving)
        {
            transform.Translate(Vector3.right * speed * Time.deltaTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            moving = true;
        }
    }

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

        if (other.CompareTag("Destroyer"))
        {
            Destroy(gameObject);
        }
    }
}
