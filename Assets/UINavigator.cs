using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class UINavigator : MonoBehaviour
{
    public InputActionReference navigateUp;
    public InputActionReference navigateDown;

    private void OnEnable()
    {
        navigateUp.action.performed += OnNavigateUp;
        navigateDown.action.performed += OnNavigateDown;
    }

    private void OnDisable()
    {
        navigateUp.action.performed -= OnNavigateUp;
        navigateDown.action.performed -= OnNavigateDown;
    }

    private void OnNavigateUp(InputAction.CallbackContext ctx)
    {
        SimulateNavigation(Vector2.up);
    }

    private void OnNavigateDown(InputAction.CallbackContext ctx)
    {
        SimulateNavigation(Vector2.down);
    }

    private void SimulateNavigation(Vector2 direction)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem.currentSelectedGameObject == null)
            return;

        var axisEventData = new AxisEventData(eventSystem)
        {
            moveVector = direction,
            moveDir = direction.y > 0 ? MoveDirection.Up : MoveDirection.Down
        };

        ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, axisEventData, ExecuteEvents.moveHandler);
    }
}
