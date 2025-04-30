using UnityEngine;
using Unity.Cinemachine;
using System;
using System.Collections;

public class CameraTransitionManager : MonoBehaviour
{
    public CinemachineCamera tutorialCam;
    public CinemachineCamera gameplayCam;
    public float priorityTutorial = 5f;
    public float priorityGameplay = 20f;
    public CinemachineBrain brain;

    public void SwitchToGameplay(Action onComplete)
    {
        StartCoroutine(SwitchCoroutine(onComplete));
    }

    private IEnumerator SwitchCoroutine(Action onComplete)
    {
        // Cambiar prioridades para forzar el blending
        tutorialCam.Priority = (int)priorityTutorial;
        gameplayCam.Priority = (int)priorityGameplay;

        // Esperar un frame para que arranque el blending
        yield return null;

        do
        {
            // Esperar un frame para que arranque el blending
            yield return null;
            Debug.Log("Esperando el blending...");
        } 
        while (brain.IsBlending);

        // ¡Blend terminado!
        onComplete?.Invoke();
    }

    public void DisableCameras()
    {
        if (tutorialCam != null)
        {
            tutorialCam.Priority = 0;
            tutorialCam.enabled = false;
        }
        if (gameplayCam != null)
        {
            gameplayCam.Priority = 0;
            gameplayCam.enabled = false;
        }
        if (brain != null)
        {
            brain.enabled = false;
            brain.gameObject.SetActive(false);
        }
        
    }
}
