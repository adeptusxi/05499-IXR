using UnityEngine;
using UnityEngine.InputSystem;

public class RaycastGrabTranslate : MonoBehaviour, ITransformMode
{
    [SerializeField] private TransformationEvaluator evaluator;
    [Tooltip("For standalone use, without a TransformModeManager")][SerializeField] private bool activateOnAwake = false;
    [Tooltip("For standalone use, without a TransformModeManager")][SerializeField] private ConfirmSelect confirmScript;
    [SerializeField] private GameObject ceiling; // to disable for top-down view
    
    [Header("User")]
    [SerializeField] private Transform userRoot;
    
    [Header("Input")]
    [SerializeField] private InputActionReference toggleViewTrigger;
    [SerializeField] private InputActionReference gripPress;
    
    [Header("Raycast")]
    [SerializeField] private OVRControllerHelper rayOrigin;
    [SerializeField] private float maxRayDistance = 200f;
    [SerializeField] private LayerMask grabbableLayer;
    [SerializeField] private LineRenderer rayRenderer;

    [Header("Settings")]
    [SerializeField] private float viewOffset = 5f; // distance from source in each view

    private bool isActivated = false;
    
    private Transform sourceTransform;
    
    private Vector3 originalUserPosition;
    private Quaternion originalUserRotation;
    
    private bool hitSource; // is the ray currently hitting sourceTransform 
    private bool isGrabbing = false; // is grip press currently pressed 
    private Vector3 grabOffset;
    private ViewAxis currentView = ViewAxis.Front; // which world axis user is currently looking down
    
    private enum ViewAxis
    {
        Front, 
        Top
    }

    private void Awake()
    {
        rayRenderer.enabled = false;
        
        if (activateOnAwake)
        {
            evaluator.onTrialStarted += ActivateTranslate;
            confirmScript.OnConfirmTrigger += DeactivateTranslate;
            toggleViewTrigger.action.performed += ToggleView;
            gripPress.action.performed += TryGrabCube;
            gripPress.action.canceled += ReleaseGrab;
        }
        
        rayRenderer.positionCount = 2;
        rayRenderer.startWidth = 0.004f;
        rayRenderer.endWidth = 0.004f;
        sourceTransform = evaluator.GetSourceTransform();
    }

    public void StartTransformMode()
    {
        toggleViewTrigger.action.performed += ToggleView;
        gripPress.action.performed += TryGrabCube;
        gripPress.action.canceled += ReleaseGrab;

        rayRenderer.enabled = true;
        ActivateTranslate();
    }

    public void StopTransformMode()
    {
        toggleViewTrigger.action.performed -= ToggleView;
        gripPress.action.performed -= TryGrabCube;
        gripPress.action.canceled -= ReleaseGrab;
        
        rayRenderer.enabled = false;
        DeactivateTranslate();
    }

    private void ActivateTranslate()
    {
        originalUserPosition = userRoot.position;
        originalUserRotation = userRoot.rotation;
        
        PositionUserAlongAxis(currentView);
        isActivated = true;
    }

    private void DeactivateTranslate()
    {
        userRoot.position = originalUserPosition;
        userRoot.rotation = originalUserRotation;
        isActivated = false;
    }

    private void Update()
    {
        if (!isActivated) return;
        
        Transform rayTransform = rayOrigin.GetPointerRayTransform();
        Ray ray = new Ray(rayTransform.position, rayTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, maxRayDistance, grabbableLayer))
        {
            if (sourceTransform == hitInfo.transform) 
                hitSource = true;
            rayRenderer.SetPosition(0, rayTransform.position);
            rayRenderer.SetPosition(1, hitInfo.point);
        }
        else
        {
            hitSource = false;
            rayRenderer.SetPosition(0, rayTransform.position);
            rayRenderer.SetPosition(1, rayTransform.position + rayTransform.forward * maxRayDistance);
        }

        // grabbing cube if grip is pressed and ray hit source 
        if (isGrabbing && hitSource)
        {
            Vector3 hitPoint = hitInfo.point + grabOffset;

            // lock to plane perpendicular to currentView 
            if (currentView == ViewAxis.Front) hitPoint.z = sourceTransform.position.z;
            else if (currentView == ViewAxis.Top) hitPoint.y = sourceTransform.position.y;

            sourceTransform.position = hitPoint;
        }
    }

    private void TryGrabCube()
    {
        isGrabbing = true;
    }

    private void ReleaseGrab()
    {
        isGrabbing = false;
    }

    private void ToggleView()
    {
        currentView = currentView == ViewAxis.Front ? ViewAxis.Top : ViewAxis.Front;
        PositionUserAlongAxis(currentView);
    }

    // moves user to viewOffset from sourceTransform along world axis (currentView)
    private void PositionUserAlongAxis(ViewAxis axis)
    {
        switch (axis)
        {
            case ViewAxis.Front:
                userRoot.position = sourceTransform.position - Vector3.forward * viewOffset;
                userRoot.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                ceiling.SetActive(true);
                break;
            case ViewAxis.Top:
                userRoot.position = sourceTransform.position + Vector3.up * viewOffset;
                userRoot.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                ceiling.SetActive(false);
                break;
        }
    }
    
    #region callbacks

    private void ToggleView(InputAction.CallbackContext context)
    {
        ToggleView();
    }

    private void TryGrabCube(InputAction.CallbackContext context)
    {
        TryGrabCube();
    }

    private void ReleaseGrab(InputAction.CallbackContext context)
    {
        ReleaseGrab();
    }

    #endregion 
}
