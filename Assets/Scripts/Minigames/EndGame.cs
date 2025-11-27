using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class EndGame : MonoBehaviour
{
    private readonly List<PlayerChoices.PlayerData> winners = new();

    [Header("Base Player")]
    public GameObject basePlayer;

    [Header("Spawn Points for Winners")]
    public List<GameObject> stands = new();

    private CinemachineSplineDolly splineDolly;
    public bool debugMode = false;

    private readonly List<CinemachineCamera> cinemachineCameras = new();
    private CameraTransitionManager camTransition;
    private Camera mainCamera;

    // Jugadores instanciados agrupados por stand
    private readonly List<List<GameObject>> playersPerStand = new();

    [Header("Animaciones")]
    [Tooltip("Nombre del parámetro float en el Animator de los jugadores")]
    public string animParam = "AnimIndex";

    // Color para stands con más de un jugador (empate)
    public Color TieTextColor = new Color(1f, 0.3f, 0.7f);

    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogError("Main Camera not found in the scene.");

        camTransition = GetComponent<CameraTransitionManager>();
        if (!camTransition)
            Debug.LogError("CameraTransitionManager not found on this GameObject.");

        splineDolly = GetComponentInChildren<CinemachineSplineDolly>(true);
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (debugMode)
        {
            PlayerChoices.Instance.ResetPlayers();

            var colors = new[]
            {
                PlayerChoices.PlayerColor.Azul,
                PlayerChoices.PlayerColor.Naranja,
                PlayerChoices.PlayerColor.Verde,
                PlayerChoices.PlayerColor.Amarillo
            };

            for (int i = 0; i < colors.Length; i++)
            {
                InputDevice dev = (i == 0 && Keyboard.current != null)
                    ? (InputDevice)Keyboard.current
                    : InputSystem.AddDevice<Gamepad>(); // fake gamepad

                PlayerChoices.AddPlayer(colors[i], dev);
            }

            foreach (var player in PlayerChoices.GetActivePlayers())
            {
                player.wins = Random.Range(0,5); // victorias aleatorias
            }
        }
#endif

        GetWinners();
        SpawnWinners();

        camTransition.brain = mainCamera.GetComponent<CinemachineBrain>();

        SoundManager.PlayMusic(7); // Endgame3
        SoundManager.FadeInMusic(1f);

        // Primera transición: espera a que acabe el recorrido inicial
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        yield return new WaitForSeconds(6.5f);

        camTransition.tutorialCam = GetComponentInChildren<CinemachineCamera>(true); // cámara inicial
        camTransition.gameplayCam = cinemachineCameras[0]; // primer stand

        bool done = false;
        camTransition.SwitchToGameplay(() => { done = true; });
        yield return new WaitUntil(() => done);

        // Secuencia de stands
        yield return ShowStandCamerasSequential();
    }

    private IEnumerator ShowStandCamerasSequential()
    {
        // ya estamos en la cámara del primer stand
        for (int i = 0; i < cinemachineCameras.Count; i++)
        {
            // Acción en el stand actual
            yield return PlayStandActionAndWaitForIdle(i);

            // Avanzar a la siguiente cámara si hay más
            if (i + 1 < cinemachineCameras.Count)
            {
                camTransition.tutorialCam = camTransition.gameplayCam;
                camTransition.gameplayCam = cinemachineCameras[i + 1];

                bool done = false;
                camTransition.SwitchToGameplay(() => { done = true; });
                yield return new WaitUntil(() => done);
            }
        }

        Debug.Log("[EndGame] Secuencia de stands finalizada.");
        SceneChanger.Instance.ApplyTransitionAsync(SceneNames.Credits, Transitions.FadeToCredits, 3f);
    }

    private IEnumerator PlayStandActionAndWaitForIdle(int standIndex)
    {
        if (standIndex < 0 || standIndex >= playersPerStand.Count) yield break;

        int animValue = Random.Range(1, 5); // 1..4 (baile)

        // Lanzamos animación de baile en todos
        foreach (var go in playersPerStand[standIndex])
        {
            var anim = go.GetComponentInChildren<Animator>(true);
            if (anim != null)
                anim.SetInteger(animParam, animValue);
        }

        // Esperamos hasta que TODOS vuelvan a Idle
        yield return new WaitUntil(() => AllBackToIdle(standIndex));

        Debug.Log($"[EndGame] Stand {standIndex}: todos los jugadores han vuelto a Idle -> avanzar cámara");
    }

    private bool AllBackToIdle(int standIndex)
    {
        foreach (var go in playersPerStand[standIndex])
        {
            var anim = go.GetComponentInChildren<Animator>(true);
            if (anim == null) continue;

            var st = anim.GetCurrentAnimatorStateInfo(0);
            if (!st.IsName("HumanoidIdle")) // cambia "Idle" si tu estado idle se llama distinto
                return false;
        }
        return true;
    }

    public void GetWinners()
    {
        var orderedPlayers = PlayerChoices.GetActivePlayers()
            .OrderByDescending(p => p.wins)
            .ToList();

        winners.Clear();
        winners.AddRange(orderedPlayers);

        Debug.Log(string.Join(", ", winners.Select(p => $"{p.Color}: {p.wins}")));
    }

    public void SpawnWinners()
    {
        var groups = winners
            .GroupBy(p => p.wins)
            .OrderByDescending(g => g.Key)
            .Select(g => g.ToList())
            .ToList();

        cinemachineCameras.Clear();
        playersPerStand.Clear();

        int usedStands = Mathf.Min(groups.Count, stands.Count);

        for (int s = 0; s < usedStands; s++)
        {
            var stand = stands[s];
            var group = groups[s];

            ConfigureStandVisuals(stand, group, s);

            var vcam = stand.GetComponentInChildren<CinemachineCamera>(true);
            if (vcam != null) cinemachineCameras.Add(vcam);

            var spawnPoint = stand.transform.Find("Spawn");
            if (spawnPoint == null)
            {
                Debug.LogError($"[EndGame] El stand '{stand.name}' no tiene hijo 'Spawn'.");
                playersPerStand.Add(new List<GameObject>());
                continue;
            }

            var offsets = GetSpawnOffsets(group.Count, 1f);
            var list = new List<GameObject>();
            for (int i = 0; i < group.Count; i++)
            {
                var go = ConfigureAndInstantiatePlayer(basePlayer, group[i].Color, spawnPoint, offsets[i]);
                list.Add(go);
            }
            playersPerStand.Add(list);
        }

        for (int s = usedStands; s < stands.Count; s++)
            if (stands[s] != null) stands[s].SetActive(false);
    }

    private void ConfigureStandVisuals(GameObject stand, List<PlayerChoices.PlayerData> group, int standIndex)
    {
        var text = stand.GetComponentInChildren<TextMeshPro>(true);

        // Texto = "nº Puesto"
        if (text)
            text.text = $"{standIndex + 1}º Puesto";

        bool tie = group.Count > 1;
        Color col = tie ? TieTextColor : PlayerChoices.GetColorRGBA(group[0].Color);

        if (text) text.color = col;

        ColorTaggedLightsInStand(stand, col);
        ColorAllParticlesInStand(stand, col);
    }

    private void ColorAllParticlesInStand(GameObject stand, Color col)
    {
        var psAll = stand.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in psAll)
        {
            var main = ps.main;
            main.startColor = col;
        }
    }

    private void ColorTaggedLightsInStand(GameObject stand, Color col)
    {
        var lights = stand.GetComponentsInChildren<Light>(true)
                          .Where(l => l.CompareTag("PodiumLight")); // solo luces con tag
        foreach (var lig in lights)
        {
            lig.color = col;
        }
    }

    private GameObject ConfigureAndInstantiatePlayer(GameObject prefab, PlayerChoices.PlayerColor color, Transform spawnPoint, Vector3 localOffset)
    {
        var instance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
        instance.transform.localPosition += localOffset;

        var src = CharacterCatalog.Instance.Get(PlayerChoices.GetPlayerSkin(color));
        var srcSMR = src.GetComponentInChildren<SkinnedMeshRenderer>(true);
        var dstSMR = instance.GetComponentInChildren<SkinnedMeshRenderer>(true);

        if (srcSMR && dstSMR)
        {
            dstSMR.sharedMesh = srcSMR.sharedMesh;
            dstSMR.SetSharedMaterials(new List<Material>() { PlayerChoices.GetMaterialByColor(color) });
        }

        return instance;
    }

    private Vector3[] GetSpawnOffsets(int count, float spacing)
    {
        var arr = new Vector3[count];

        if (count == 1)
        {
            arr[0] = Vector3.zero;
        }
        else
        {
            float start = -(count - 1) * 0.5f * spacing;
            for (int i = 0; i < count; i++)
            {
                arr[i] = new Vector3(start + i * spacing, 0f, 0f);
            }
        }

        return arr;
    }
}
