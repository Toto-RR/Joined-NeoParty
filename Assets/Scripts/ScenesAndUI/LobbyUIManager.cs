using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LobbyUIManager : MonoBehaviour
{
    [Header("General Animator")]
    public Animator generalAnimator;

    [Header("Layouts")]
    public GameObject blueLayout;
    public GameObject orangeLayout;
    public GameObject greenLayout;
    public GameObject yellowLayout;

    [Header("Animators parameters")]
    public string nextParameter = "Next";
    public string backParameter = "Back";
    private string pendingNextColor;

    private Dictionary<string, GameObject> layouts;

    private void Awake()
    {
        layouts = new Dictionary<string, GameObject>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "azul", blueLayout },
            { "naranja", orangeLayout },
            { "verde", greenLayout },
            { "amarillo", yellowLayout }
        };
    }

    public void SetLayoutStep(string color, int step)
    {
        if (layouts.TryGetValue(color.ToLower(), out var layout))
            layout.GetComponent<Animator>().SetInteger("step", step);
    }

    public void SetGeneralStep(int step)
    {
        generalAnimator.gameObject.SetActive(true);
        generalAnimator.SetInteger("step", step);
    }


}
