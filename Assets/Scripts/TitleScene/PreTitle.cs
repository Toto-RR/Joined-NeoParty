using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PreTitle : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    private InputActionMap action;

    private void Start()
    {
        SoundManager.SetMusicVolume(0.5f); // Asegura que el volumen es el correcto
        SoundManager.SetFxVolume(0.5f); // Asegura que el volumen es el correcto
        SoundManager.PlayMusic(0);

        action = inputActions.FindActionMap("Tutorial", throwIfNotFound: true);
        action.Enable();

        action.FindAction("Ready", throwIfNotFound: true).performed += OnReadyPerformed;
    }

    private void OnReadyPerformed(InputAction.CallbackContext ctx)
    {
        action.FindAction("Ready", throwIfNotFound: true).performed -= OnReadyPerformed;
        action.Disable();
        SceneChanger.Instance.ApplyTransitionAsync(SceneNames.MainMenu, Transitions.Fade);
    }
}
