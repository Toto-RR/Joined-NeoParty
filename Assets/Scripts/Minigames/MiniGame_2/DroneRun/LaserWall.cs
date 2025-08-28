using System.Data;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI.Table;

public interface ILaserWall
{
    void Setup(Vector2 cellIndexCenter, Vector2 cellSize);
}

public class LaserWall : MonoBehaviour, ILaserWall
{
    [Header("Movimiento local en +Z")]
    public float speed = 20f;
    public float despawnZ = 25f;   // cuando z local >= despawnZ -> destruir

    public bool colorsMode = false;
    public PlayerChoices.PlayerColor gateColor;
    Rigidbody _rb;

    Vector2 center;      // centro del hueco
    Vector2 size;        // (ancho, alto) del hueco

    void Awake()
    {
        // Garantiza rigidbody para que los triggers funcionen
        _rb = GetComponent<Rigidbody>();
        if (!_rb) _rb = gameObject.AddComponent<Rigidbody>();
        _rb.isKinematic = true;     // se mueve por transform
        _rb.useGravity = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void Setup(Vector2 cellIndexCenter, Vector2 cellSize)
    {
        center = cellIndexCenter;
        size = cellSize;

        Build();
    }

    void Build()
    {
        var sZ = GetComponentInChildren<SafeZone>(true);
        if (!sZ) return;

        var tr = sZ.transform;

        // El hijo SIN escala ni offset (que no se multiplique nada)
        tr.localPosition = Vector3.zero;
        tr.localRotation = Quaternion.identity;
        tr.localScale = Vector3.one;

        // Asegura BoxCollider trigger en el hijo
        var bc = tr.GetComponent<BoxCollider>();
        if (!bc) bc = tr.gameObject.AddComponent<BoxCollider>();
        bc.isTrigger = true;

        // Compensa cualquier escala del padre (por si en algún caso lo escalas)
        var ls = transform.lossyScale;
        var inv = new Vector3(
            ls.x == 0f ? 1f : 1f / ls.x,
            ls.y == 0f ? 1f : 1f / ls.y,
            ls.z == 0f ? 1f : 1f / ls.z
        );

        // El center/size que quieres en MUNDO, pásalo a espacio LOCAL del hijo dividiendo por la escala acumulada del padre
        bc.center = Vector3.Scale(new Vector3(center.x, center.y, 0f), inv);
        bc.size = Vector3.Scale(new Vector3(size.x, size.y, 1f), inv);
    }

    void Update()
    {
        transform.localPosition += new Vector3(0f, 0f, speed * Time.deltaTime);

        //if (transform.localPosition.z >= despawnZ)
            //Destroy(gameObject); // o vuelve al pool
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Destroyer"))
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Destroyer"))
        {
            Destroy(gameObject);
        }
    }

}
