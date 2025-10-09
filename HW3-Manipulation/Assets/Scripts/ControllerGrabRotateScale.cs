using UnityEngine;
using UnityEngine.InputSystem;

// grip press grabbing for rotation and scale 
// assumes confirmScript will handle reparenting in OnConfirmTrigger
public class ControllerGrabRotateScale : TransformMode
{
    [SerializeField] private TransformationEvaluator evaluator;
    
    [Tooltip("For standalone use, without a TransformModeManager")][SerializeField] private bool activateOnAwake = false;
    [Tooltip("For standalone use, without a TransformModeManager")][SerializeField] private ConfirmSelect confirmScript;

    [Header("User")]
    [SerializeField] private Transform userRoot; 
    [SerializeField] private Transform cameraTransform;
    
    [Header("Input")]
    [SerializeField] private InputActionReference leftGrip;
    [SerializeField] private InputActionReference rightGrip;
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;
    
    [Header("Settings")]
    [SerializeField] private bool moveUserToSource = false; // false if used with another ITransformMode that moves the user
    [SerializeField] private float userScaleMultiplier = 1f;
    [SerializeField] private float grabDistance = 0.8f;
    [SerializeField] private float viewOffset = 0.1f;

    private bool isActivated = false;
    
    private Transform sourceTransform;
    private Collider sourceCollider;
    private Transform initialSourceParent; 
    
    private Vector3 originalUserPosition;
    private Quaternion originalUserRotation;
    private Vector3 originalUserScale;
    
    private bool leftGrabbing = false;
    private bool rightGrabbing = false;
    private bool twoHandMode = false;

    // two-handed grab 
    private Vector3 initialTwoHandVector; // vector between controllers at grab start
    private float initialTwoHandDistance; // distance between controllers at grab start 
    private Vector3 initialScale;
    private Vector3 scalingAxis; // cube's local axis that grabbed faces are opposites on 
    private Quaternion initialRotationOffset; // rotation between cube and initialTwoHandVector

    private void Awake()
    {
        if (activateOnAwake)
        {
            evaluator.onTrialStarted += GetSourceInfo;
            confirmScript.OnConfirmTrigger += Reset;

            leftGrip.action.performed += TryGrabLeft;
            rightGrip.action.performed +=TryGrabRight;

            leftGrip.action.canceled += ReleaseLeft;
            rightGrip.action.canceled += ReleaseRight;

            ActivateRotateScale();
        }
    }
    
    public override void StartTransformMode()
    {
        leftGrip.action.performed += TryGrabLeft;
        rightGrip.action.performed += TryGrabRight;

        leftGrip.action.canceled += ReleaseLeft;
        rightGrip.action.canceled += ReleaseRight;
        
        ActivateRotateScale();
    }

    public override void StopTransformMode()
    {
        leftGrip.action.performed -= TryGrabLeft;
        rightGrip.action.performed -= TryGrabRight;

        leftGrip.action.canceled -= ReleaseLeft;
        rightGrip.action.canceled -= ReleaseRight;

        DeactivateRotateScale();
    }

    public override string ModeInstructions() =>
        "Rough Rotate/Scale:\nGrip press on the cube to grab it.\nOrient its axes roughly.";

    private void ActivateRotateScale()
    {
        GetSourceInfo();

        if (moveUserToSource)
        {
            originalUserPosition = userRoot.position;
            originalUserRotation = userRoot.rotation;
            originalUserScale = userRoot.localScale;

            userRoot.localScale *= userScaleMultiplier;
            Vector3 rootCameraOffset = cameraTransform.localPosition;
            userRoot.position = sourceTransform.position + Vector3.forward * viewOffset - rootCameraOffset;
            cameraTransform.LookAt(sourceTransform);
        }
    }

    private void DeactivateRotateScale()
    {
        if (moveUserToSource)
        {
            userRoot.position = originalUserPosition;
            userRoot.rotation = originalUserRotation;
            userRoot.localScale = originalUserScale;
        }
        
        ReleaseGrab(leftController, true);
        ReleaseGrab(rightController, false);
        Reset();
    }
    
    private void Update()
    {
        if (!isActivated) return;
        
        if (twoHandMode)
        {
            Vector3 currentVector = leftController.position - rightController.position;
            float currentDistance = currentVector.magnitude;

            float scaleFactor = Mathf.Abs(currentDistance / initialTwoHandDistance);

            Vector3 newScale = initialScale;
            if (Mathf.Abs(scalingAxis.x) > 0.5f)
                newScale.x = initialScale.x * scaleFactor;
            if (Mathf.Abs(scalingAxis.y) > 0.5f)
                newScale.y = initialScale.y * scaleFactor;
            if (Mathf.Abs(scalingAxis.z) > 0.5f)
                newScale.z = initialScale.z * scaleFactor;

            sourceTransform.localScale = newScale;

            // rotate cube to match rotation of "axis" between controllers 
            Quaternion rotationDelta = Quaternion.FromToRotation(initialTwoHandVector.normalized, currentVector.normalized);
            sourceTransform.rotation = rotationDelta * initialRotationOffset;
        }
    }

    private void GetSourceInfo()
    {
        sourceTransform = evaluator.GetSourceTransform();
        sourceCollider = sourceTransform.GetComponent<Collider>();
    }

    // if not already grabbing and cube is close enough, grab it 
    private void TryGrab(Transform controller, bool isLeft)
    {
        if (sourceTransform == null || sourceCollider == null) return;
        
        Vector3 closestPoint = sourceCollider.ClosestPoint(controller.position);
        float distance = Vector3.Distance(controller.position, closestPoint);
        if (distance > grabDistance) return;
        
        if (isLeft && !leftGrabbing)
            leftGrabbing = true;
        else if (!isLeft && !rightGrabbing)
            rightGrabbing = true;
        
        if (leftGrabbing ^ rightGrabbing)
        {
            // one-handed grab, just regular parenting 
            sourceTransform.SetParent(controller, true);
        }
        else if (leftGrabbing && rightGrabbing)
        {
            // potential two-handed grab 
            // which faces are being grabbed 
            Vector3 leftDir = sourceTransform.InverseTransformPoint(leftController.position).normalized;
            Vector3 rightDir = sourceTransform.InverseTransformPoint(rightController.position).normalized;

            int leftAxis = DominantAxis(leftDir);
            int rightAxis = DominantAxis(rightDir);

            if (leftAxis == rightAxis && !Mathf.Approximately(Mathf.Sign(leftDir[leftAxis]), Mathf.Sign(rightDir[rightAxis])))
            {
                // grabbed faces are opposites, enable scaling 
                twoHandMode = true;
                sourceTransform.SetParent(initialSourceParent, true);

                scalingAxis = Vector3.zero;
                scalingAxis[leftAxis] = 1f;

                initialTwoHandVector = leftController.position - rightController.position;
                initialTwoHandDistance = initialTwoHandVector.magnitude;
                initialScale = sourceTransform.localScale;

                Vector3 localAxisDir = sourceTransform.TransformDirection(scalingAxis);
                initialRotationOffset = Quaternion.FromToRotation(localAxisDir, initialTwoHandVector.normalized) * sourceTransform.rotation;
                
                // TODO bug - sometimes two-handed grab inverts cube on grabbing axis 
            }
            else
            {
                // ignore second grab if not opposite sides 
                if (isLeft)
                    leftGrabbing = false;
                else
                    rightGrabbing = false;
            }
        }
    }

    private void ReleaseGrab(Transform controller, bool isLeft)
    {
        if (isLeft) leftGrabbing = false;
        else rightGrabbing = false;

        // restore parent 
        if (twoHandMode)
        {
            twoHandMode = false;
            sourceTransform.SetParent(initialSourceParent, true);
        }
        else if (!leftGrabbing && !rightGrabbing)
        {
            sourceTransform.SetParent(initialSourceParent, true);
        }
    }

    private void Reset()
    {
        leftGrabbing = false;
        rightGrabbing = false;
        twoHandMode = false;
    }
    
    // finds dominant xyz axis of a vector 
    private int DominantAxis(Vector3 v)
    {
        v = v.normalized;
        float ax = Mathf.Abs(v.x);
        float ay = Mathf.Abs(v.y);
        float az = Mathf.Abs(v.z);
        if (ax > ay && ax > az) return 0;
        if (ay > az) return 1;
        return 2;
    }
    
    #region callbacks

    private void TryGrabLeft(InputAction.CallbackContext context)
    {
        TryGrab(leftController, true);
    }

    private void TryGrabRight(InputAction.CallbackContext context)
    {
        TryGrab(rightController, false);
    }

    private void ReleaseLeft(InputAction.CallbackContext context)
    {
        ReleaseGrab(leftController, true);
    }

    private void ReleaseRight(InputAction.CallbackContext context)
    {
        ReleaseGrab(rightController, false);
    }
    
    #endregion
} ;