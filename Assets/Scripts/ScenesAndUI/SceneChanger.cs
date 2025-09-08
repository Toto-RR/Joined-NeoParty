using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneNames
{
    PreTitle,
    MainMenu,
    Lobby,
    GameScene_1,
    GameScene_2,
    GameScene_3,
    MinigameResult,
    EndGame,
    Credits
}

public enum Transitions
{
    Doors,
    Fade,
    FadeText,
    TV,
    Curtain,
    FadeToCredits,
    None
}

public class SceneChanger : MonoBehaviour
{
    public static SceneChanger Instance;

    [Header("Animator único que reproducirá las transiciones")]
    public Animator animator;

    [Header("Controllers disponibles (nombre debe coincidir con el enum)")]
    public List<RuntimeAnimatorController> controllers = new List<RuntimeAnimatorController>();

    [Header("Triggers")]
    public string playTrigger = "Transition";
    public string resetTrigger = "Reset";

    [Header("Hold entre close y open")]
    [Tooltip("Tiempo (en segundos, tiempo real) que se mantiene la transición cerrada antes de abrir.")]
    public float holdClosedSeconds = 0f;


    // cache para búsquedas rápidas
    private Dictionary<string, RuntimeAnimatorController> nameToController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildControllerMap();
    }

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            // Desactivar todos los hijos del objeto que tiene el componente animator
            foreach (Transform child in animator.transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void BuildControllerMap()
    {
        nameToController = new Dictionary<string, RuntimeAnimatorController>();
        foreach (var c in controllers)
        {
            if (c == null) continue;
            var key = c.name.Trim();
            if (!nameToController.ContainsKey(key))
                nameToController.Add(key, c);
        }
    }

    // --- API de cambio directo ---
    public void ChangeScene(string sceneName) => SceneManager.LoadScene(sceneName);
    public void ChangeSceneByIndex(int sceneIndex) => SceneManager.LoadScene(sceneIndex);
    public void ChangeSceneByEnum(SceneNames sceneIndex) => SceneManager.LoadScene((int)sceneIndex);

    // --- Transiciones ---
    public void ApplyTransitionAsync(int scene)
    {
        StartCoroutine(TransitionAndLoadAsyncInternal(
            () => UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene),
            Transitions.Fade,
            null));
    }

    public void ApplyTransitionAsync(int scene, Transitions transition, float? holdSeconds = null)
    {
        StartCoroutine(TransitionAndLoadAsyncInternal(
            () => UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene),
            transition,
            holdSeconds));
    }

    public void ApplyTransitionAsync(SceneNames scene, Transitions transition, float? holdSeconds = null)
    {
        StartCoroutine(TransitionAndLoadAsyncInternal(
            () => UnityEngine.SceneManagement.SceneManager.LoadSceneAsync((int)scene),
            transition,
            holdSeconds));
    }

    public void ApplyTransitionAsync(string sceneName, Transitions transition, float? holdSeconds = null)
    {
        StartCoroutine(TransitionAndLoadAsyncInternal(
            () => UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName),
            transition,
            holdSeconds));
    }


    private IEnumerator TransitionAndLoadAsyncInternal(Func<AsyncOperation> loadOpFactory, Transitions transition, float? holdSecondsOpt = null)
    {
        SoundManager.FadeOutMusic(1f, stopAfter: false);

        var controller = GetControllerByName(transition.ToString());
        if (transition == Transitions.None || animator == null || controller == null)
        {
            loadOpFactory(); // carga directa
            yield break;
        }

        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.runtimeAnimatorController = controller;
        animator.Rebind();
        animator.Update(0f);

        animator.ResetTrigger(resetTrigger);
        animator.SetTrigger(playTrigger); // CLOSE

        // Esperar a que termine la animación de CLOSE
        yield return new WaitUntil(() => HasFinished(animator, 0));

        // Empezamos a cargar la escena en segundo plano con la pantalla ya cerrada
        var op = loadOpFactory();
        op.allowSceneActivation = false;

        // Esperar a que la carga llegue al 90% (listo para activar)
        yield return new WaitUntil(() => op.progress >= 0.9f);

        // Mantener cerrado el tiempo deseado (tiempo real)
        float hold = holdSecondsOpt ?? holdClosedSeconds;
        if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

        // Activar la escena (ya con todo listo detrás del “telón”)
        bool sceneLoaded = false;
        void OnLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) => sceneLoaded = true;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnLoaded;

        op.allowSceneActivation = true;
        yield return new WaitUntil(() => sceneLoaded);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnLoaded;

        // Dar un par de frames para que la escena inicialice bien
        yield return null;
        yield return new WaitForEndOfFrame();
        yield return null;

        // Trigger de OPEN
        if (!string.IsNullOrEmpty(resetTrigger))
            animator.SetTrigger(resetTrigger);

        // Esperar a que termine la animación de OPEN
        yield return new WaitUntil(() => HasFinished(animator, 0));
    }

    private static bool HasFinished(Animator anim, int layer)
    {
        if (anim.IsInTransition(layer)) return false;
        var st = anim.GetCurrentAnimatorStateInfo(layer);
        return st.normalizedTime >= 1f;
    }

    private RuntimeAnimatorController GetControllerByName(string controllerName)
    {
        if (nameToController == null) BuildControllerMap();

        // coincidencia exacta
        if (nameToController.TryGetValue(controllerName, out var ctrl))
            return ctrl;

        // fallback: buscar por Contains/ignore case por si el asset tiene sufijos
        foreach (var kv in nameToController)
        {
            if (kv.Key.Equals(controllerName, System.StringComparison.OrdinalIgnoreCase)) return kv.Value;
            if (kv.Key.Replace(" ", "").Equals(controllerName.Replace(" ", ""), System.StringComparison.OrdinalIgnoreCase)) return kv.Value;
        }
        return null;
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Juego cerrado");
    }
}
