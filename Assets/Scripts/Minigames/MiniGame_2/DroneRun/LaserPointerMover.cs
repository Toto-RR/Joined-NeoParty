using UnityEngine;

public class LaserPointerMover : MonoBehaviour
{
    [Header("Refs (hijos de 'Matrix')")]
    public MatrixHoleSystem matrixSystem;   // arrastra el de 'Matrix'
    public Transform horizontales;          // Matrix/Horizontal
    public Transform verticales;            // Matrix/Vertical

    [Header("Movimiento")]
    public float speed = 25f;
    public bool animate = true;

    // offsets por si el pivot de las barras no está centrado en el carril
    [Header("Corrección (opcional)")]
    public Vector2 centerOffset = Vector2.zero;  // suma (x,y) al centro de la celda

    // En tu LaserPointerMover (o el script donde esté)
    [Header("Láser: color/emisión")]
    public float emissionIntensity = 4f;  // brillo HDR de la emisión
    public bool alsoTintAlbedo = true;    // opcional: teñir también el color base

    static readonly int ID_Color = Shader.PropertyToID("_Color");
    static readonly int ID_BaseColor = Shader.PropertyToID("_BaseColor");     // URP Lit
    static readonly int ID_EmissionColor = Shader.PropertyToID("_EmissionColor");

    private Color linesColor;

    float _tx; // destino X para Horizontal (en local de Matrix)
    float _ty; // destino Y para Vertical  (en local de Matrix)
    bool _hasTarget;

    /// Llamar con la celda objetivo
    public void SetToCell(Vector2Int cell, Color color)
    {
        if (!matrixSystem || !horizontales || !verticales)
        {
            Debug.LogWarning("[LaserPointerMover] Faltan referencias (Matrix/Horizontal/Vertical).");
            return;
        }

        // ❗ spawnInterval es un TIEMPO, no una velocidad. No lo metas en 'speed'.
        // speed = spawnInterval;  <-- quítalo

        linesColor = color;
        ApplyColor();

        // Centro y destinos
        Vector2 c = matrixSystem.GetCellCenter(cell.x, cell.y) + centerOffset;
        _tx = c.x;   // Horizontal mueve SOLO X
        _ty = c.y;   // Vertical   mueve SOLO Y
        _hasTarget = true;

        if (!animate)
        {
            var ph = horizontales.localPosition;
            horizontales.localPosition = new Vector3(_tx, ph.y, ph.z);

            var pv = verticales.localPosition;
            verticales.localPosition = new Vector3(pv.x, _ty, pv.z);
        }
    }

    private void ApplyColor()
    {
        // Color de emisión = color * intensidad (HDR)
        Color emissive = linesColor * emissionIntensity;

        // Horizontal
        if (horizontales)
            ApplyColorToRenderers(horizontales, linesColor, emissive);

        // Vertical
        if (verticales)
            ApplyColorToRenderers(verticales, linesColor, emissive);
    }

    void ApplyColorToRenderers(Transform root, Color albedo, Color emission)
    {
        var rends = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            // Usar .material instancia el material (está bien para pocos objetos como tus láseres)
            var m = r.material;

            // Teñir albedo (según shader: URP Lit usa _BaseColor; Standard usa _Color)
            if (alsoTintAlbedo)
            {
                if (m.HasProperty(ID_BaseColor)) m.SetColor(ID_BaseColor, albedo);
                else if (m.HasProperty(ID_Color)) m.SetColor(ID_Color, albedo);
            }

            // Emisión
            if (m.HasProperty(ID_EmissionColor))
            {
                m.EnableKeyword("_EMISSION");               // asegúrate de activar la keyword
                m.SetColor(ID_EmissionColor, emission);     // color * intensidad
            }
            else
            {
                // Si tu shader es de Shader Graph y la propiedad se llama distinto, cambia el nombre aquí.
                // Ejemplo: m.SetColor("_Emission", emission);
            }
        }
    }


    void Update()
    {
        if (!_hasTarget) return;

        if (horizontales)
        {
            var p = horizontales.localPosition;
            float nx = animate ? Mathf.MoveTowards(p.x, -_tx, speed * Time.deltaTime) : _tx;
            horizontales.localPosition = new Vector3(nx, p.y, p.z);
        }

        // Vertical: SOLO Y (en local de Matrix)
        if (verticales)
        {
            var p = verticales.localPosition;
            float ny = animate ? Mathf.MoveTowards(p.y, _ty, speed * Time.deltaTime) : _ty;
            verticales.localPosition = new Vector3(p.x, ny, p.z);
        }
    }
}
