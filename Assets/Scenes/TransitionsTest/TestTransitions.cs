using UnityEngine;
using UnityEngine.InputSystem;

public class TestTransitions : MonoBehaviour
{
    public Transitions transitionType = Transitions.Doors;

    void Update()
    {
        var kb = Keyboard.current;

        if (kb == null)
        {
            Debug.Log("Teclado no detectado"); 
            return;
        }

        if (kb.leftArrowKey.wasReleasedThisFrame)
        {
            SceneChanger.Instance.ApplyTransitionAsync(4, Transitions.Doors);
        }
        if (kb.rightArrowKey.wasReleasedThisFrame)
        {
            SceneChanger.Instance.ApplyTransitionAsync(5, Transitions.Doors);
        }

        if (kb.upArrowKey.wasReleasedThisFrame)
        {
            SceneChanger.Instance.ApplyTransitionAsync(4, transitionType);
        }
        if (kb.downArrowKey.wasReleasedThisFrame)
        {
            SceneChanger.Instance.ApplyTransitionAsync(5, transitionType);
        }
    }
}
