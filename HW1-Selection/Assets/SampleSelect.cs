using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SampleSelect : MonoBehaviour
{

    public InputActionReference triggerPress;
    public InputActionReference gripPress;

    public SelectionEvaluator selectionEvaluator;

    Transform selected;

    Color prevColor;


    void SelectRandom(InputAction.CallbackContext context)
    {
        if (selected)
        {
            selected.GetComponent<Renderer>().material.color = prevColor;
        }
        List<Transform> spheres = selectionEvaluator.GetSpheres();       
        selected = spheres[Random.Range(0, spheres.Count)];
        prevColor = selected.GetComponent<Renderer>().material.color;
        selected.GetComponent<Renderer>().material.color = Color.magenta;
        selectionEvaluator.SetSelection(selected);
    }

    void ConfirmSelection(InputAction.CallbackContext context)
    {
        if (!selected) {
            return;
        }
        selected.GetComponent<Renderer>().material.color = prevColor;
        selected = null;
        selectionEvaluator.ConfirmSelection();
    }

    void Awake()
    {
        gripPress.action.performed += SelectRandom;
        triggerPress.action.performed += ConfirmSelection;
    }
}
