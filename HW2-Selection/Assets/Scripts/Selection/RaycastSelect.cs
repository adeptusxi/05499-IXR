using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.UI;

// implements simple raycast selection 
public class RaycastSelect : MonoBehaviour
{
    [SerializeField] protected SelectionEvaluator selectionEvaluator;
    [SerializeField] private GameObject evaluationUI;
    [SerializeField] protected Camera cam;
    
    [Header("Controls")]
    [SerializeField] private InputActionReference triggerPress;
    [SerializeField] private InputActionReference buttonPress;
    [SerializeField] private Transform rayOriginObject; // for non-controller raycast (e.g. from head), use this 
    [SerializeField] private OVRControllerHelper rightController;
    [SerializeField] private OVRControllerHelper leftController;
    [SerializeField] private RaycastType raycastOrigin;
    
    [Header("Haptics")]
    [SerializeField] private float hapticsAmplitude = 0.17f;
    [SerializeField] private float hapticsDuration = 0.023f;
    
    [Header("Display")]
    [SerializeField] private bool showRay = true;
    [SerializeField] private LineRenderer rayRenderer;
    [SerializeField] private float maxRayDistance = 100;
    [SerializeField] private bool showCursor = false;
    [SerializeField] private Image headCursor; // object on screen-space camera canvas, fixed to center of user's vision

    private Transform selected;         // currently selected sphere. null if current raycast is no-hit
    private Transform lastHitSphere;    // sphere last hit by raycast. null if last raycast was a no-hit 
    private Color originalColor;        // original color of currently selected sphere 

    private enum RaycastType
    {
        LeftController, 
        RightController, 
        Custom
    }
    
    protected virtual void Awake()
    {
        triggerPress.action.performed += ConfirmSelection;
        buttonPress.action.performed += ConfirmSelection;
        
        rayRenderer.positionCount = 2;
        rayRenderer.startWidth = 0.004f;
        rayRenderer.endWidth = 0.004f;
    }

    protected virtual void Start()
    {
        if (!showCursor && headCursor != null)
            headCursor.enabled = false;
        
        if (!showRay && rayRenderer != null) 
            rayRenderer.enabled = false;
    }

    protected void Update()
    {
        if (!evaluationUI.activeSelf) // don't raycast if trial hasn't started 
            Raycast();
    }

    protected void OnDestroy()
    {
        triggerPress.action.performed -= ConfirmSelection;
        buttonPress.action.performed -= ConfirmSelection;
    }

    private void Raycast()
    {
        Transform rayOrigin;
        switch (raycastOrigin)
        {
            case RaycastType.LeftController:
                rayOrigin = leftController.GetPointerRayTransform();
                break;
            case RaycastType.RightController:
                rayOrigin = rightController.GetPointerRayTransform();
                break;
            default:
                rayOrigin = rayOriginObject;
                break;
        }

        Ray ray = GetRay(rayOrigin);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
        {
            // ray hit something in scene 
            OnRaycastHit(hit, ray);
            
            if (showRay)
            {
                rayRenderer.SetPosition(0, rayOrigin.position);
                rayRenderer.SetPosition(1, hit.point);
            }

            ResetSelected(); // clear previous selection 
            
            var sphere = hit.transform;
            if (sphere != null && selectionEvaluator.GetSpheres().Contains(sphere))
            {
                // ray hit a sphere 
                SetSelected(sphere);

                OnRaycastHitSphere(hit, ray, sphere);
                if (selected != sphere)
                {
                    selected = sphere;
                    OnRaycastHitDifferentSphere(hit, ray, sphere);
                }
                else
                {
                    OnRaycastHitNewSphere(hit, ray, sphere);
                }
            }
            else
            {
                OnRaycastMissSphere(ray);
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
            lastHitSphere = null;

            OnRaycastMiss(ray);
            OnRaycastMissSphere(ray);
        }
    }

    protected virtual Ray GetRay(Transform rayOrigin)
    {
        return new Ray(rayOrigin.position, rayOrigin.forward);
    }

    // if applicable, does controller haptics 
    // does not do anything if in UI, not using controller raycast, or the hit is the same as previous hit 
    protected void DoHaptics(Transform sphereHit)
    {
        if ((raycastOrigin == RaycastType.RightController || raycastOrigin == RaycastType.LeftController)
            && lastHitSphere != null && lastHitSphere != sphereHit && !evaluationUI.activeSelf)
        {
            if (raycastOrigin == RaycastType.RightController)
            {
                HapticsManager.SendHaptics(XRNode.RightHand, hapticsAmplitude, hapticsDuration);
            }
            if (raycastOrigin == RaycastType.LeftController)
            {
                HapticsManager.SendHaptics(XRNode.LeftHand, hapticsAmplitude, hapticsDuration);
            }
        }
    }

    protected void SetSelected(Transform sphere)
    {
        DoHaptics(sphere);
        originalColor = sphere.GetComponent<Renderer>().material.color;
        sphere.GetComponent<Renderer>().material.color = Color.magenta;
        selectionEvaluator.SetSelection(sphere);
        selected = sphere;
        lastHitSphere = sphere;
    }
    
    protected void ResetSelected()
    {
        if (selected != null)
        {
            selected.GetComponent<Renderer>().material.color = originalColor;
            selected = null;
        }
    }
    
    private void ConfirmSelection(InputAction.CallbackContext context)
    {
        if (!selected) {
            return;
        }
        selected.GetComponent<Renderer>().material.color = originalColor;
        selected = null;
        selectionEvaluator.ConfirmSelection();
    }
    
    #region callbacks 
    
    // called when ray hits anything in scene 
    protected virtual void OnRaycastHit(RaycastHit hit, Ray ray) {}
    
    // called when ray hits a sphere 
    protected virtual void OnRaycastHitSphere(RaycastHit hit, Ray ray, Transform sphere) {} 
    
    // called when ray hits sphere after a no-sphere-hit 
    // note OnRaycastHit() and OnRaycastHitSphere() are also called 
    protected virtual void OnRaycastHitNewSphere(RaycastHit hit, Ray ray, Transform sphere) {}
    
    // called when ray hits a sphere after a different sphere hit 
    // note OnRaycastHit() and OnRaycastHitSphere() are also called 
    protected virtual void OnRaycastHitDifferentSphere(RaycastHit hit, Ray ray, Transform sphere) {}
    
    // called when ray does not hit a sphere 
    protected virtual void OnRaycastMissSphere(Ray ray) {} 
    
    // called when ray does not hit anything in scene 
    protected virtual void OnRaycastMiss(Ray ray) {}
    
    #endregion callbacks 
}
