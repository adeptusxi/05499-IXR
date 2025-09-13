using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RaycastSelect : MonoBehaviour
{
    [SerializeField] private InputActionReference triggerPress;
    [SerializeField] private InputActionReference gripPress;
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private LineRenderer rayRenderer;
    [SerializeField] private float maxRayDistance = 50f;
    
    [SerializeField] private SelectionEvaluator selectionEvaluator;

    private Transform selected;
    private Color prevColor;
    
    private void Awake()
    {
        triggerPress.action.performed += ConfirmSelection;
        gripPress.action.performed += SelectWithRay;
        
        rayRenderer.positionCount = 2;
        rayRenderer.startWidth = 0.1f;
        rayRenderer.endWidth = 0.5f;
        rayRenderer.material.color = Color.cyan;
    }

    void Update()
    {
        RenderRay();
    }

    private void OnDestroy()
    {
        triggerPress.action.performed -= ConfirmSelection;
        gripPress.action.performed -= SelectWithRay;
    }

    private void RenderRay()
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
        {
            rayRenderer.SetPosition(0, rayOrigin.position);
            rayRenderer.SetPosition(1, hit.point);
        }
        else
        {
            rayRenderer.SetPosition(0, rayOrigin.position);
            rayRenderer.SetPosition(1, rayOrigin.position + rayOrigin.forward * maxRayDistance);
        }
    }

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
