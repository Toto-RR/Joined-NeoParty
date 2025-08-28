using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

public class MatrixHoleSystem : MonoBehaviour
{
    [Header("Área jugable (en local)")]
    public Vector2 halfExtents = new Vector2(6f, 3.5f);

    [Header("Grid")]
    [Min(1)] public Vector2 matrix = new(2, 2);

    [Header("Hueco (0..1 de la celda)")]
    [Range(0.1f, 1f)] public float fillX = 1f;
    [Range(0.1f, 1f)] public float fillY = 1f;

    [Header("Spawn")]
    public Transform parentForSpawn;     // WorldRoot/Segments
    public GameObject laserWallPrefab;
    public GameObject finishPrefab;
    public float zAtSpawn = -60f;        // aparición respecto a parentForSpawn

    [Header("Colors Mode")]
    public bool colorsMode = false;                                   // ON/OFF
    public PlayerChoices.PlayerColor currentGateColor = PlayerChoices.PlayerColor.Azul; // color activo

    public int cols { get { return Mathf.Max(1, (int)matrix.x); } }
    public int rows { get { return Mathf.Max(1, (int)matrix.y); } }

    public bool drawGizmos = true;
    static readonly int ID_GridSize = Shader.PropertyToID("_GridSize");
    static readonly int ID_CellIndex = Shader.PropertyToID("_CellIndex");
    static readonly int ID_Fill = Shader.PropertyToID("_Fill");
    static readonly int ID_Tint = Shader.PropertyToID("_Tint");
    MaterialPropertyBlock _mpb;

    void Awake()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
    }

    // --- API pública ---

    public void SetGrid(int newCols, int newRows)
    {
        newCols = Mathf.Max(1, newCols);
        newRows = Mathf.Max(1, newRows);
        matrix = new Vector2(newCols, newRows);
    }

    public void SetFill01(float x01, float y01)
    {
        fillX = Mathf.Clamp01(x01);
        fillY = Mathf.Clamp01(y01);
    }

    public Vector2 GetCellSize()
    {
        float cellW = (halfExtents.x * 2f) / Mathf.Max(1, cols);
        float cellH = (halfExtents.y * 2f) / Mathf.Max(1, rows);
        return new Vector2(cellW, cellH);
    }

    public Vector2Int GetRandomCell()
    {
        int cx = Random.Range(0, cols);
        int cy = Random.Range(0, rows);
        Debug.Log($"MatrixHoleSystem: elijo celda ({cx},{cy}) de ({cols},{rows})");
        return new Vector2Int(cx, cy);
    }

    public Vector2 GetCellCenter(int cx, int cy)
    {
        float cellW = (halfExtents.x * 2f) / cols;
        float cellH = (halfExtents.y * 2f) / rows;

        float minX = -halfExtents.x + cellW * 0.5f;
        float minY = -halfExtents.y + cellH * 0.5f;

        float x = minX + cx * cellW;
        float y = minY + cy * cellH;

        return new Vector2(x, y);
    }

    public IEnumerator StartSpawn(Vector2Int indexCell)
    {
        yield return new WaitForSeconds(0.2f);
        SpawnLaserWall(indexCell, new Vector2(fillX, fillY), null);
    }

    /// <summary>
    /// API directa para spawnear indicando celda, fill y velocidad del muro (opcional).
    /// </summary>
    public GameObject SpawnAt(Vector2Int cell, Vector2 fill01, float? wallSpeed = null)
    {
        return SpawnLaserWall(cell, fill01, wallSpeed);
    }

    // --- Internos ---

    GameObject SpawnLaserWall(Vector2Int indexCell, Vector2 fill01, float? wallSpeed)
    {
        if (!laserWallPrefab || !parentForSpawn) return null;

        var go = Instantiate(laserWallPrefab, parentForSpawn);
        go.transform.localPosition = new Vector3(0, 0, zAtSpawn);
        go.transform.localScale = new Vector3(halfExtents.x * 2f, halfExtents.y * 2f, 1f);

        // Shader por MaterialPropertyBlock (instancia-safe)
        var meshRender = go.GetComponent<MeshRenderer>();
        if (meshRender != null)
        {
            _mpb.Clear();
            _mpb.SetVector(ID_GridSize, new Vector4(cols, rows, 0, 0));
            _mpb.SetVector(ID_CellIndex, new Vector4(indexCell.x, indexCell.y, 0, 0));
            _mpb.SetVector(ID_Fill, new Vector4(fill01.x, fill01.y, 0, 0));
            meshRender.SetPropertyBlock(_mpb);
        }

        // Collider seguro
        var wall = go.GetComponent<ILaserWall>();
        if (wall != null)
        {
            Vector2 cellSize = GetCellSize();
            Vector2 holeSize = new(cellSize.x * fill01.x, cellSize.y * fill01.y);
            wall.Setup(GetCellCenter(indexCell.x, indexCell.y), holeSize);
        }

        // Velocidad
        var laserWall = go.GetComponent<LaserWall>();
        if (laserWall && wallSpeed.HasValue) laserWall.speed = wallSpeed.Value;
        if (colorsMode)
        {
            // Oculta el MeshRenderer de la pared grande
            var wallMr = go.GetComponent<MeshRenderer>();
            if (wallMr) wallMr.enabled = false;

            // Activa el Renderer de la SafeZone
            var safeZone = go.GetComponentInChildren<SafeZone>(true);
            if (safeZone)
            {
                // 1) Tintar el renderer
                var safeZoneRenderer = safeZone.GetComponent<MeshRenderer>()
                                       ?? safeZone.GetComponentInChildren<MeshRenderer>(true);

                if (safeZoneRenderer)
                {
                    _mpb.Clear();
                    _mpb.SetColor(ID_Tint, PlayerChoices.GetColorRGBA(currentGateColor));
                    safeZoneRenderer.SetPropertyBlock(_mpb);
                    safeZoneRenderer.enabled = true;

                    // 2) Alinear el render AL collider calculado por Build()
                    var bc = safeZone.GetComponent<BoxCollider>(); // ya lo configuró Build()
                    if (bc)
                    {
                        safeZoneRenderer.transform.localPosition = bc.center;
                        var ls = safeZoneRenderer.transform.localScale;
                        safeZoneRenderer.transform.localScale = new Vector3(bc.size.x, bc.size.y, ls.z);

                        // Ajusta la safe zone a TODO el tamaño del hueco
                        bc.center = Vector3.zero;
                        bc.size = new(1, 1, 1);
                    }
                }

                if (laserWall)
                {
                    laserWall.colorsMode = true;
                    laserWall.gateColor = currentGateColor;
                }
            }
            else
            {
                if (laserWall) laserWall.colorsMode = false;
            }


            // 3) Marca info en el LaserWall (para que el dron lo lea al colisionar)
            if (laserWall)
            {
                laserWall.colorsMode = true;
                laserWall.gateColor = currentGateColor;
            }
        }
        else
        {
            if (laserWall) laserWall.colorsMode = false;
        }

        return go;
    }

    public void SpawnFinish()
    {
        var go = Instantiate(finishPrefab, parentForSpawn);
        go.transform.localPosition = new Vector3(0, 0, zAtSpawn);
        go.transform.localScale = new Vector3(halfExtents.x * 2f, halfExtents.y * 2f, 1f);
    }

    // Gizmos
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.blue;
        var p = transform.position;
        var r = new Vector3(halfExtents.x * 2f, halfExtents.y * 2f, 0f);
        Gizmos.DrawWireCube(p, r);

        float cellW = (halfExtents.x * 2f) / Mathf.Max(1, cols);
        float cellH = (halfExtents.y * 2f) / Mathf.Max(1, rows);
        for (int i = 0; i <= cols; i++)
        {
            float x = transform.position.x - halfExtents.x + i * cellW;
            Gizmos.DrawLine(new Vector3(x, p.y - halfExtents.y, p.z), new Vector3(x, p.y + halfExtents.y, p.z));
        }
        for (int j = 0; j <= rows; j++)
        {
            float y = transform.position.y - halfExtents.y + j * cellH;
            Gizmos.DrawLine(new Vector3(p.x - halfExtents.x, y, p.z), new Vector3(p.x + halfExtents.x, y, p.z));
        }

        Gizmos.color = Color.yellow;
        for (int cx = 0; cx < Mathf.Max(1, cols); cx++)
            for (int cy = 0; cy < Mathf.Max(1, rows); cy++)
            {
                Vector2 c = GetCellCenter(cx, cy);
                Gizmos.DrawSphere(transform.TransformPoint(new Vector3(c.x, c.y, 0f)), 0.12f);
            }
    }
}
