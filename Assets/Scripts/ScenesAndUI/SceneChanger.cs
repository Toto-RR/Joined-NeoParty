using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneNames
{
    MainMenu,
    Lobby,
    GameScene,
    PostMinigame,
    PostGame,
    Settings,
    Credits
}

public enum Transitions
{
    Doors,
    Fade,
    FadeText,
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
    public void ApplyTransition(int sceneIndex, Transitions transition, float transitionTime = 0.8f)
    {
        if (transitionTime <= 0f) transitionTime = this.transitionTime;

        if (System.Enum.IsDefined(typeof(SceneNames), sceneIndex))
            StartCoroutine(TransitionAndLoad((SceneNames)sceneIndex, transition)); // <-- NO ASÍNCRONA
        else
            Debug.LogError($"Índice de escena inválido: {sceneIndex}");
    }


    public void ApplyTransitionAsync(int scene, Transitions transition, float minTransitionTime = 0.8f)
    {
        StartCoroutine(TransitionAndLoadAsync((SceneNames)scene, transition));
    }
    public void ApplyTransitionAsync(SceneNames scene, Transitions transition)
    {
        StartCoroutine(TransitionAndLoadAsync(scene, transition));
    }

    private IEnumerator TransitionAndLoadAsync(SceneNames scene, Transitions transition)
    {
        // --- CERRAR ---
        var controller = GetControllerByName(transition.ToString());
        if (transition == Transitions.None || animator == null || controller == null)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene((int)scene);
            yield break;
        }

        // Asegúrate de que el overlay siempre anima aunque cambie de cámara/escena
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        animator.runtimeAnimatorController = controller;
        animator.Rebind();
        animator.Update(0f);

        animator.ResetTrigger(resetTrigger);   // limpia estado anterior
        animator.SetTrigger(playTrigger);      // "Transition" -> anim de CERRAR

        // Espera a que ACABE el cierre de verdad
        yield return new WaitUntil(() => HasFinished(animator, 0));

        // --- CARGA ASYNC (oculta tras cierre estático) ---
        var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync((int)scene);
        op.allowSceneActivation = false;

        yield return new WaitUntil(() => op.progress >= 0.9f);

        // --- ACTIVAR ESCENA (pico de trabajo) ---
        bool sceneLoaded = false;
        void OnLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) => sceneLoaded = true;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnLoaded;

        op.allowSceneActivation = true;

        // Espera a que Unity confirme que la escena ya está “puesta”
        yield return new WaitUntil(() => sceneLoaded);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnLoaded;

        // --- SETTLE FRAMES (evita tirón en la apertura) ---
        // Espera un par de frames para que CPU/GPU/GC se estabilicen
        yield return null;              // frame 1
        yield return new WaitForEndOfFrame(); // frame 1 fin (garantiza dibujado)
        yield return null;              // frame 2

        // --- ABRIR ---
        // Si tu “Reset” abre, úsalo; si no, crea un trigger Open distinto.
        if (!string.IsNullOrEmpty(resetTrigger))
            animator.SetTrigger(resetTrigger); // "Reset" -> anim de ABRIR

        // (Opcional) si quieres desactivar el overlay tras abrir, espera a que termine
        yield return new WaitUntil(() => HasFinished(animator, 0));
    }

    private IEnumerator TransitionAndLoad(SceneNames scene, Transitions transition)
    {
        // --- CERRAR ---
        var controller = GetControllerByName(transition.ToString());
        if (transition == Transitions.None || animator == null || controller == null)
        {
            SceneManager.LoadScene((int)scene); // carga síncrona directa
            yield break;
        }

        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        animator.runtimeAnimatorController = controller;
        animator.Rebind();
        animator.Update(0f);

        animator.ResetTrigger(resetTrigger);
        animator.SetTrigger(playTrigger);      // cerrar

        // Espera a que termine el cierre
        yield return new WaitUntil(() => HasFinished(animator, 0));

        // --- CARGA SÍNCRONA ---
        SceneManager.LoadScene((int)scene);    // bloqueo: carga inmediata

        // --- SETTLE FRAMES ---
        yield return null;                      // frame 1
        yield return new WaitForEndOfFrame();   // final frame 1
        yield return null;                      // frame 2

        // --- ABRIR ---
        if (!string.IsNullOrEmpty(resetTrigger))
            animator.SetTrigger(resetTrigger);  // abrir

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
