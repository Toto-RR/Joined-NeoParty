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

    [Header("Tiempo de transición (en segundos)")]
    public float transitionTime = 1f;

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
    public void ApplyTransitionAsync(int scene, Transitions transition)
    {
        StartCoroutine(TransitionAndLoadAsyncInternal(
            () => UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene),
            transition));
    }

    public void ApplyTransitionAsync(SceneNames scene, Transitions transition)
    {
        StartCoroutine(TransitionAndLoadAsyncInternal(
            () => UnityEngine.SceneManagement.SceneManager.LoadSceneAsync((int)scene),
            transition));
    }

    public void ApplyTransitionAsync(string sceneName, Transitions transition)
    {
        StartCoroutine(TransitionAndLoadAsyncInternal(
            () => UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName),
            transition));
    }

    private IEnumerator TransitionAndLoadAsyncInternal(Func<AsyncOperation> loadOpFactory, Transitions transition)
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
        animator.SetTrigger(playTrigger);

        yield return new WaitUntil(() => HasFinished(animator, 0));

        var op = loadOpFactory();
        op.allowSceneActivation = false;

        yield return new WaitUntil(() => op.progress >= 0.9f);

        bool sceneLoaded = false;
        void OnLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) => sceneLoaded = true;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnLoaded;

        op.allowSceneActivation = true;
        yield return new WaitUntil(() => sceneLoaded);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnLoaded;

        yield return null;
        yield return new WaitForEndOfFrame();
        yield return null;

        if (!string.IsNullOrEmpty(resetTrigger))
            animator.SetTrigger(resetTrigger);

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
