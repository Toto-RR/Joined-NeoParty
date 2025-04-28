using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public static SceneChanger Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // evita duplicados si tienes mas de uno en la escena
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // opcional, si quieres que persista entre escenas
    }

    // Cambia de escena usando el nombre
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Cambia de escena usando el índice (opcional)
    public void ChangeSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    // Cierra el juego
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Juego cerrado");
    }
}
