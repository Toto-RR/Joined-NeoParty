using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RaceLeaderboardUI : MonoBehaviour
{
    [Header("Scene refs")]
    public CarSpawner spawner;             // arrástralo
    public RectTransform leftColumn;       // columna fija de rangos (VerticalLayoutGroup)
    public RectTransform rightColumn;      // columna de nombres (VerticalLayoutGroup)
    public PlayerRow rowPrefab;            // tu prefab PlayerRow (nombre + color)

    [Header("Layout")]
    public int rowHeight = 40;             // ¡igual que tu PlayerRow!
    public TMP_FontAsset rankFont;         // opcional (si no, usa fuente por defecto)
    public int rankFontSize = 24;

    [Header("Refresco y animación")]
    public float checkInterval = 0.05f;
    public float swapAnimDuration = 0.25f;

    class Entry { public CarController car; public PlayerRow row; }
    readonly List<Entry> entries = new();
    readonly List<RectTransform> rankItems = new(); // izquierda, fijos
    PlayerChoices.PlayerColor[] lastOrder = System.Array.Empty<PlayerChoices.PlayerColor>();
    float tCheck;

    // Llamar desde Minigame_3 justo tras SpawnCars():
    //   leaderboard.BuildRows(carSpawner.GetPlayers());
    public void SpawnRowsHUD()
    {
        List<CarController> cars = spawner.GetPlayers();

        // limpia columnas
        foreach (Transform c in leftColumn) Destroy(c.gameObject);
        foreach (Transform c in rightColumn) Destroy(c.gameObject);
        entries.Clear(); rankItems.Clear();

        // 1) Columna izquierda (fija): 1..N
        for (int i = 0; i < cars.Count; i++)
        {
            var rt = CreateRankItem(leftColumn, i + 1);
            rankItems.Add(rt);
        }

        // 2) Columna derecha: una PlayerRow por coche (en orden inicial)
        for (int i = 0; i < cars.Count; i++)
        {
            var e = new Entry { car = cars[i] };
            var row = Instantiate(rowPrefab, rightColumn);
            e.row = row;

            // Visual inicial (color + nombre)
            var color = PlayerChoices.GetColorRGBA(cars[i].PlayerColor);
            var name = cars[i].PlayerColor.ToString();
            var img = row.GetComponent<Image>();
            if (img) img.color = color;
            if (row.playerNameText) row.playerNameText.text = name;
            if (row.checkMark) row.checkMark.SetActive(false);

            // Alto coherente
            var le = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = rowHeight;

            row.transform.SetSiblingIndex(i);
            entries.Add(e);
        }

        // Primer refresco
        ForceRefresh();
    }

    RectTransform CreateRankItem(Transform parent, int number)
    {
        // GameObject con LayoutElement para altura fija
        var go = new GameObject($"Rank_{number}", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        var le = go.GetComponent<LayoutElement>();
        le.preferredHeight = rowHeight;

        // Texto TMP centrado
        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        var tr = (RectTransform)textGO.transform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;

        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = number.ToString();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = rankFontSize;
        if (rankFont) tmp.font = rankFont;

        return rt;
    }

    void Update()
    {
        if (entries.Count == 0) return;
        tCheck += Time.deltaTime;
        if (tCheck < checkInterval) return;
        tCheck = 0f;
        RefreshIfChanged();
    }

    public void ForceRefresh() => RefreshIfChanged(true);

    void RefreshIfChanged(bool force = false)
    {
        var order = ComputeOrderColors();
        if (force || !Same(order, lastOrder))
        {
            lastOrder = order;

            // Orden objetivo por progreso total (vueltas + progreso de la vuelta)
            var desired = entries
                .OrderByDescending(e => e.car.Laps + Mathf.Clamp01(
                    e.car.GetComponent<UnityEngine.Splines.SplineAnimate>()?.NormalizedTime ?? 0f))
                .ToList();

            // Solo reordenamos/animamos la DERECHA.
            StartCoroutine(AnimateRightColumn(desired));
        }
    }

    PlayerChoices.PlayerColor[] ComputeOrderColors()
    {
        return entries
            .OrderByDescending(e => e.car.Laps + Mathf.Clamp01(
                e.car.GetComponent<UnityEngine.Splines.SplineAnimate>()?.NormalizedTime ?? 0f))
            .Select(e => e.car.PlayerColor)
            .ToArray();
    }

    static bool Same(PlayerChoices.PlayerColor[] a, PlayerChoices.PlayerColor[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (!a[i].Equals(b[i])) return false;
        return true;
    }

    IEnumerator AnimateRightColumn(List<Entry> desired)
    {
        // 1) posiciones actuales (solo DERECHA)
        var startPos = new Dictionary<Entry, Vector2>();
        foreach (var e in entries)
            startPos[e] = ((RectTransform)e.row.transform).anchoredPosition;

        // 2) cambiamos siblingIndex en la DERECHA según orden deseado
        for (int i = 0; i < desired.Count; i++)
            desired[i].row.transform.SetSiblingIndex(i);

        // 3) forzamos layout y leemos posiciones destino (DERECHA)
        LayoutRebuilder.ForceRebuildLayoutImmediate(rightColumn);

        var targetPos = new Dictionary<Entry, Vector2>();
        foreach (var e in desired)
            targetPos[e] = ((RectTransform)e.row.transform).anchoredPosition;

        // 4) animar: ignorar layout SOLO en la derecha y hacer tween manual
        foreach (var e in entries)
        {
            var le = e.row.GetComponent<LayoutElement>() ?? e.row.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            ((RectTransform)e.row.transform).anchoredPosition = startPos[e];
        }

        float t = 0f;
        while (t < swapAnimDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / swapAnimDuration);
            float s = a * a * (3f - 2f * a); // smoothstep

            foreach (var e in entries)
            {
                var from = startPos[e];
                var to = targetPos[e];
                ((RectTransform)e.row.transform).anchoredPosition = Vector2.LerpUnclamped(from, to, s);
            }
            yield return null;
        }

        // 5) devolver control al layout en la derecha y fijar orden final
        foreach (var e in entries)
        {
            var le = e.row.GetComponent<LayoutElement>();
            if (le) le.ignoreLayout = false;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(rightColumn);

        // 6) actualizar lista a su orden final (derecha)
        entries.Clear();
        entries.AddRange(desired);
    }
}
