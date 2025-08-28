using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallSpawner : MonoBehaviour
{
    // =======================
    //  GAME LOOP / DIFICULTAD
    // =======================
    [Header("Grid inicial")]
    [SerializeField] int startCols = 2;
    [SerializeField] int startRows = 2;

    [Header("Hueco (relleno relativo de la celda)")]
    [Range(0.2f, 1f)][SerializeField] float maxFill = 1.0f;
    [Range(0.2f, 1f)][SerializeField] float minFill = 0.5f;
    [Tooltip("Reducción del tamaño del hueco por ola")]
    [SerializeField] float fillDecayPerWave = 0.03f;  // reduce tamaño del hueco por ola

    [Header("Velocidad de las paredes")]
    [Tooltip("Velocidad inicial de las paredes")]
    [SerializeField] float startWallSpeed = 20f;       // coincide por defecto con tu LaserWall
    [Tooltip("Incremento de velocidad por ola")]
    [SerializeField] float speedPerWave = 3.5f;        // +velocidad por ola

    [Header("Cadencia")]
    [Tooltip("Tiempo inicial entre paredes")]
    [SerializeField] float spawnInterval = 1.20f;     // tiempo inicial entre paredes
    [Tooltip("Número de paredes por ola")]
    [SerializeField] int wallsPerWave = 6;
    [Tooltip("Descanso entre olas")]
    [SerializeField] float restBetweenWaves = 1.2f;

    [Header("Evolución de grid")]
    [Tooltip("Cada N olas, aumenta el tamaño de la grid")]
    [SerializeField] int gridIncreaseEveryNWaves = 3;  // cada N olas crece la grid
    [Tooltip("Si está activo, alterna entre aumentar cols y rows. Si no, aumenta ambos a la vez.")]
    [SerializeField] bool alternateColsRows = true;    // alterna cols/rows al crecer

    [Header("Patrones activos")]
    [Tooltip("Si no hay ninguno activo, siempre usará Random")]
    [SerializeField] bool useRandom = true;
    [Tooltip("Barrido por filas")]
    [SerializeField] bool useSweepRows = true;
    [Tooltip("Barrido por columnas")]
    [SerializeField] bool useSweepCols = true;
    [Tooltip("Patrón ajedrez")]
    [SerializeField] bool useChess = true;
    [Tooltip("Patrón cruz")]
    [SerializeField] bool useCross = true;
    [Tooltip("Secuencia fija de posiciones")]
    [SerializeField] bool useFixedSequence = true;

    [Header("Colors Mode")]
    [SerializeField] int switchToColorsModeAtWave = 4;

    [Header("Inicio")]
    public bool startLoopAutomaticallyAfterCountdown = true;

    [Header("Duración del minijuego")]
    [SerializeField] float gameDuration = 90f; // segundos

    int _wave = 0;
    System.Random _rng = new System.Random();

    private MatrixHoleSystem matrixSystem;
    private LaserPointerMover laserPointerMover;
    public void Setup(MatrixHoleSystem matrix, LaserPointerMover laserPointer)
    {
        matrixSystem = matrix;
        laserPointerMover = laserPointer;
    }

    public void StartGame()
    {
        StopGameLoop();
        StartCoroutine(GameLoop());
    }

    public void StopGameLoop()
    {
        StopAllCoroutines(); // corta coroutines locales de este componente (incluye GameLoop/Countdown si estuvieran)
    }

    IEnumerator GameLoop()
    {
        if (!matrixSystem)
        {
            Debug.LogError("WallSpawner: Falta MatrixHoleSystem.");
            yield break;
        }

        // Estado inicial
        _wave = 0;
        matrixSystem.SetGrid(startCols, startRows);
        matrixSystem.SetFill01(maxFill, maxFill);
        float elapsed = 0f;

        while (elapsed < gameDuration)
        {
            _wave++;

            // --- Dificultad ---
            float speed = startWallSpeed + (_wave - 1) * speedPerWave;
            float targetFill = Mathf.Clamp(maxFill - (_wave - 1) * fillDecayPerWave, minFill, maxFill);
            matrixSystem.SetFill01(targetFill, targetFill);

            // --- Grid dinámica ---
            if (gridIncreaseEveryNWaves > 0 && _wave % gridIncreaseEveryNWaves == 0)
            {
                if (alternateColsRows)
                {
                    bool incCols = ((_wave / gridIncreaseEveryNWaves) % 2) == 1;
                    int newCols = matrixSystem.cols + (incCols ? 1 : 0);
                    int newRows = matrixSystem.rows + (incCols ? 0 : 1);
                    matrixSystem.SetGrid(newCols, newRows);
                }
                else
                {
                    matrixSystem.SetGrid(matrixSystem.cols + 1, matrixSystem.rows + 1);
                }
            }

            // --- Selección de patrón y generación de celdas ---
            Pattern pattern = PickPattern();
            List<Vector2Int> cells = GenerateCells(pattern, matrixSystem.cols, matrixSystem.rows, wallsPerWave);

            foreach (var cell in cells)
            {
                // Decide ColorsMode y color UNA sola vez para esta pared
                bool isColors = (_wave >= switchToColorsModeAtWave);
                matrixSystem.colorsMode = isColors;

                Color lineColor = Color.red;
                if (isColors)
                {
                    var activos = PlayerChoices.GetActivePlayers();
                    if (activos.Count > 0)
                        matrixSystem.currentGateColor = activos[_rng.Next(activos.Count)].Color;
                    lineColor = PlayerChoices.GetColorRGBA(matrixSystem.currentGateColor);
                }

                // Alinea láser y tiñe con el MISMO color de la puerta
                laserPointerMover.SetToCell(cell, lineColor);

                // Spawnea pared con velocidad de esta ola
                GameObject wallGo = matrixSystem.SpawnAt(cell, new Vector2(targetFill, targetFill), speed);

                // Espera a que la pared se destruya antes de instanciar la siguiente
                while (wallGo != null)
                    yield return null;
            }

            yield return new WaitForSeconds(restBetweenWaves);
            elapsed += (wallsPerWave * spawnInterval) + restBetweenWaves;
        }

        matrixSystem.SpawnFinish();
    }

    enum Pattern { Random, SweepRows, SweepCols, Chess, Cross, FixedSequence }

    Pattern PickPattern()
    {
        var options = new List<Pattern>();
        if (useRandom) options.Add(Pattern.Random);
        if (useSweepRows) options.Add(Pattern.SweepRows);
        if (useSweepCols) options.Add(Pattern.SweepCols);
        if (useChess) options.Add(Pattern.Chess);
        if (useCross) options.Add(Pattern.Cross);
        if (useFixedSequence) options.Add(Pattern.FixedSequence);

        if (options.Count == 0) return Pattern.Random;
        return options[_rng.Next(options.Count)];
    }

    List<Vector2Int> GenerateCells(Pattern p, int cols, int rows, int count)
    {
        switch (p)
        {
            case Pattern.SweepRows: return Gen_SweepRows(cols, rows, count);
            case Pattern.SweepCols: return Gen_SweepCols(cols, rows, count);
            case Pattern.Chess: return Gen_Chess(cols, rows, count);
            case Pattern.Cross: return Gen_Cross(cols, rows, count);
            case Pattern.FixedSequence: return Gen_FixedSequence(cols, rows, count);
            case Pattern.Random:
            default: return Gen_Random(cols, rows, count);
        }
    }

    // ------- Patrones -------

    List<Vector2Int> Gen_Random(int cols, int rows, int count)
    {
        var list = new List<Vector2Int>(count);
        for (int i = 0; i < count; i++)
            list.Add(new Vector2Int(_rng.Next(cols), _rng.Next(rows)));
        return list;
    }

    List<Vector2Int> Gen_SweepRows(int cols, int rows, int count)
    {
        var list = new List<Vector2Int>(count);
        int i = 0;
        for (int y = 0; y < rows && i < count; y++)
            for (int x = 0; x < cols && i < count; x++, i++)
                list.Add(new Vector2Int(x, y));
        return list;
    }

    List<Vector2Int> Gen_SweepCols(int cols, int rows, int count)
    {
        var list = new List<Vector2Int>(count);
        int i = 0;
        for (int x = 0; x < cols && i < count; x++)
            for (int y = 0; y < rows && i < count; y++, i++)
                list.Add(new Vector2Int(x, y));
        return list;
    }

    List<Vector2Int> Gen_Chess(int cols, int rows, int count)
    {
        var temp = new List<Vector2Int>();
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < cols; x++)
                if (((x + y) & 1) == 0) temp.Add(new Vector2Int(x, y));

        // Completa aleatorios si faltan
        while (temp.Count < count) temp.Add(new Vector2Int(_rng.Next(cols), _rng.Next(rows)));
        return temp.GetRange(0, Mathf.Min(count, temp.Count));
    }

    List<Vector2Int> Gen_Cross(int cols, int rows, int count)
    {
        var list = new List<Vector2Int>();
        int midX = cols / 2;
        int midY = rows / 2;
        for (int x = 0; x < cols; x++) list.Add(new Vector2Int(x, midY));
        for (int y = 0; y < rows; y++) if (y != midY) list.Add(new Vector2Int(midX, y));

        if (list.Count >= count) return list.GetRange(0, count);
        while (list.Count < count) list.Add(new Vector2Int(_rng.Next(cols), _rng.Next(rows)));
        return list;
    }

    List<Vector2Int> Gen_FixedSequence(int cols, int rows, int count)
    {
        var seq = new List<Vector2Int>()
        {
            new Vector2Int(0,0),
            new Vector2Int(cols-1,0),
            new Vector2Int(0,rows-1),
            new Vector2Int(cols-1,rows-1),
            new Vector2Int(cols/2, rows/2),
        };

        var list = new List<Vector2Int>(count);
        int i = 0;
        while (list.Count < count)
        {
            var s = seq[i % seq.Count];
            list.Add(new Vector2Int(
                Mathf.Clamp(s.x, 0, cols - 1),
                Mathf.Clamp(s.y, 0, rows - 1)
            ));
            i++;
        }
        return list;
    }

}
