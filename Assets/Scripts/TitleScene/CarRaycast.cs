using UnityEngine;
using UnityEngine.Splines;

public class CarRaycast : MonoBehaviour
{
    public float detectionDistance = 5f;
    public LayerMask carLayer;

    void Update()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = transform.forward;

        // Dibujar el rayo en la escena
        Debug.DrawRay(origin, dir * detectionDistance, Color.red);

        // Lógica de frenado si detecta otro coche
        if (Physics.Raycast(origin, dir, detectionDistance, carLayer))
        {
            Debug.Log($"{name} ve un coche delante");
            GetComponent<SplineAnimate>().Duration = Mathf.Max(GetComponent<SplineAnimate>().Duration + Time.deltaTime * 5f, 0f); // frenar rápido
        }
    }
}
