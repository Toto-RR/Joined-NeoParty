using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GamepadDebugger : MonoBehaviour
{
    public enum Test
    {
        Gamepad,
        Arduino
    }

    public InputActionAsset inputActions;
    public RectTransform pointer;       // Se moverá con el stick derecho (giroscopio)
    public Image[] buttonIndicators;    // Indicadores visuales para botones
    public Slider triggerSlider;  // Nuevo: Slider para visualizar el trigger

    private InputAction joystick;
    private InputAction trigger;
    private InputAction[] buttons;      // Array con las acciones individuales de botones

    public float pointerRange = 100f;
    public float triggerHeight = 150f;

    public Test deviceTesting = Test.Gamepad;

    void Awake()
    {
        var gameplayMap = inputActions.FindActionMap("Arduino");

        joystick = gameplayMap.FindAction("Joystick");
        trigger = gameplayMap.FindAction("Trigger");

        // Asume que tienes 4 acciones separadas llamadas "Button0", "Button1", etc.
        buttons = new InputAction[buttonIndicators.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i] = gameplayMap.FindAction("Button" + (i+1));
        }

        gameplayMap.Enable();
    }

    private Vector2 currentPointer;
    public float smoothSpeed = 10f;

    void Update()
    {
        // Mover el puntero con el stick derecho (joystick simulado con giroscopio)
        Vector2 rawInput = joystick.ReadValue<Vector2>();
        currentPointer = Vector2.Lerp(currentPointer, rawInput, Time.deltaTime * smoothSpeed);
        pointer.anchoredPosition = currentPointer * pointerRange;

        // RAW INPUT (NOT RECOMMENDED)
        //Vector2 joy = joystick.ReadValue<Vector2>();
        //pointer.anchoredPosition = joy * pointerRange;


        // Escalar la barra según el gatillo
        float rawTrigger = trigger.ReadValue<float>();
        float normalizedTrigger = (rawTrigger + 1f) / 2f; // Convierte -1, 0.5, 1
        triggerSlider.value = normalizedTrigger;
        Debug.Log("Trigger value: " + rawTrigger + ", Normalized: " + normalizedTrigger);

        // Mostrar estado de los botones
        for (int i = 0; i < buttons.Length; i++)
        {
            bool pressed = buttons[i].ReadValue<float>() > 0.5f;
            if(i == 0) buttonIndicators[i].color = pressed ? Color.blue : Color.gray;
            else if(i == 1) buttonIndicators[i].color = pressed ? Color.magenta : Color.gray;
            else if(i == 2) buttonIndicators[i].color = pressed ? Color.green : Color.gray;
            else if(i == 3) buttonIndicators[i].color = pressed ? Color.yellow : Color.gray;
        }
    }
}
