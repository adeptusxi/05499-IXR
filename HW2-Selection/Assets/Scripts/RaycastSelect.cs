using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RaycastSelect : MonoBehaviour
{
    [SerializeField] private InputActionReference triggerPress;
    // [SerializeField] private InputActionReference gripPress;
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private bool showRay = true;
    [SerializeField] private LineRenderer rayRenderer;
    [SerializeField] private float maxRayDistance = 50f;

    [SerializeField] private SelectionEvaluator selectionEvaluator;

    private Transform selected;
    private Color prevColor;
    
    private void Awake()
    {
        triggerPress.action.performed += ConfirmSelection;
        // gripPress.action.performed += SelectWithRay;
        
        rayRenderer.positionCount = 2;
        rayRenderer.startWidth = 0.02f;
        rayRenderer.endWidth = 0.0075f;
        rayRenderer.material.color = Color.cyan;
    }

    void Update()
    {
        Raycast();
    }

    private void OnDestroy()
    {
        triggerPress.action.performed -= ConfirmSelection;
        // gripPress.action.performed -= SelectWithRay;
    }

    // render the ray
    private void Raycast()
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
        {
            if (showRay)
            {
                rayRenderer.SetPosition(0, rayOrigin.position);
                rayRenderer.SetPosition(1, hit.point);
            }

            ResetSelected(); // clear previous selection
            
            var sphere = hit.transform;
            if (sphere != null && selectionEvaluator.GetSpheres().Contains(sphere))
            {
                prevColor = sphere.GetComponent<Renderer>().material.color;
                sphere.GetComponent<Renderer>().material.color = Color.magenta;
                selectionEvaluator.SetSelection(sphere);
                selected = sphere;
            }
        }
        else
        {
            if (showRay)
            {
                rayRenderer.SetPosition(0, rayOrigin.position);
                rayRenderer.SetPosition(1, rayOrigin.position + rayOrigin.forward * maxRayDistance);
            }

            ResetSelected();
        }
    }

    private void ResetSelected()
    {
        if (selected != null)
        {
            selected.GetComponent<Renderer>().material.color = prevColor;
            selected = null;
        }

    }

    // trigger select with controller 
    private void SelectWithRay(InputAction.CallbackContext context)
    {
        if (selected)
        {
            selected.GetComponent<Renderer>().material.color = prevColor;
        }

        List<Transform> spheres = selectionEvaluator.GetSpheres();
        if (spheres == null || spheres.Count == 0) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        Transform closestHit = null;
        float closestDist = Mathf.Infinity;
        foreach (Transform sphere in spheres)
        {
            Collider col = sphere.GetComponent<Collider>();
            if (!col) continue;

            if (col.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (hit.distance < closestDist)
                {
                    closestDist = hit.distance;
                    closestHit = sphere;
                }
            }
        }

        if (closestHit != null)
        {
            selected = closestHit;
            prevColor = selected.GetComponent<Renderer>().material.color;
            selected.GetComponent<Renderer>().material.color = Color.magenta;

            selectionEvaluator.SetSelection(selected);
        }
        else
        {
            selected = null;
        }
    }

    private void ConfirmSelection(InputAction.CallbackContext context)
    {
        if (!selected) {
            return;
        }
        selected.GetComponent<Renderer>().material.color = prevColor;
        selected = null;
        selectionEvaluator.ConfirmSelection();
    }
}
