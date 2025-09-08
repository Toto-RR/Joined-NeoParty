using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    private bool moving = false;
    public float speed = 5f;

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
            //Debug.Log("Obstacle has hit the ground, starting movement.");
            moving = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Destroyer"))
        {
            //Debug.Log("Obstacle has hit the Destroyer, destroying.");
            Destroy(gameObject);
        }

        if (other.CompareTag("Player"))
        {
            Debug.Log("Tocó al jugador");
        }
    }
}
