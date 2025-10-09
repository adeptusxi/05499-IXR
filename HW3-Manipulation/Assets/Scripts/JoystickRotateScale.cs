using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// toggles through source's local axes, placing user along some offset of the current axis 
// left joystick rotates about current axis 
// right joystick scales on other two axes 
public class JoystickRotateScale : MonoBehaviour, ITransformMode
{
    [SerializeField] private TransformationEvaluator evaluator;
    
    [Tooltip("For standalone use, without a TransformModeManager")][SerializeField] private bool activateOnAwake = false;
    [Tooltip("For standalone use, without a TransformModeManager")][SerializeField] private ConfirmSelect confirmScript;
    
    [Header("User")]
    [SerializeField] private Transform userRoot; 

    [Header("Input")]
    [SerializeField] private InputActionReference rotateJoystick;
    [SerializeField] private InputActionReference scaleJoystick;
    [SerializeField] private InputActionReference toggleAxesTrigger;

    [Header("Settings")] 
    [SerializeField] private float viewOffset = 2f; // for each axis, move user so that cube CENTER is this far from camera
    [SerializeField] private float rotationSpeed = 50f; // degree/sec, multiplier on rotateJoystick value 
    [SerializeField] private float scaleSpeed = 1f; // unit/sec, multiplier on scaleJoystick value  

    private bool isActivated = false;

    private Transform sourceTransform;
    
    private Vector3 originalUserPosition;
    private Quaternion originalUserRotation;
    
    private Axis activeAxis = Axis.X;

    private enum Axis
    {
        X = 0,
        Y = 1,
        Z = 2,
    }
    // for consistency 
    // maps active axis -> (source's forward in user's pov, source's up in user's pov) 
    private static readonly Dictionary<Axis, (Vector3 forward, Vector3 up)> axisDirections =
        new Dictionary<Axis, (Vector3 forward, Vector3 up)>
        {
            { Axis.X, (Vector3.right, Vector3.up) }, // user looking down X: Y is "up"
            { Axis.Y, (Vector3.up, Vector3.forward) }, // user looking down Y: Z is "up" 
            { Axis.Z, (Vector3.forward, Vector3.up) }  // user looking down X: Y is "up"
        };
    
    private void Awake()
    {
        if (activateOnAwake)
        {
            evaluator.onTrialStarted += ActivateRotateScale;
            confirmScript.OnConfirmTrigger += DeactivateRotateScale;
            toggleAxesTrigger.action.performed += CycleAxis;
        }
    }

    public void StartTransformMode()
    {
        ActivateRotateScale();
        toggleAxesTrigger.action.performed += CycleAxis;
    }
    
    public void StopTransformMode()
    {
        toggleAxesTrigger.action.performed -= CycleAxis;
        DeactivateRotateScale();
    }

    private void ActivateRotateScale()
    {
        sourceTransform = evaluator.GetSourceTransform();
        if (sourceTransform == null) return;
        
        originalUserPosition = userRoot.position;
        originalUserRotation = userRoot.rotation;

        isActivated = true;
        PositionUserAlongAxis(activeAxis);
    }

    private void DeactivateRotateScale()
    {
        userRoot.position = originalUserPosition;
        userRoot.rotation = originalUserRotation;
        isActivated = false;
    }

    // switches active axis to next axis 
    private void CycleAxis()
    {
        if (!isActivated || sourceTransform == null) return;

        activeAxis = activeAxis = (Axis)(((int)activeAxis + 1) % 3);
        PositionUserAlongAxis(activeAxis);
    }

    // moves user to viewOffset along source's local axis, looking at source 
    private void PositionUserAlongAxis(Axis axis)
    {
        // get local axes from user's pov 
        var (forwardUser, upUser) = axisDirections[axis];

        // convert to world directions
        Vector3 worldForward = sourceTransform.TransformDirection(forwardUser);
        Vector3 worldUp = sourceTransform.TransformDirection(upUser);

        // move user to source 
        Vector3 worldOffset = worldForward * viewOffset;
        userRoot.position = sourceTransform.position + worldOffset;
        userRoot.rotation = Quaternion.LookRotation(-worldForward, worldUp);
    }
    
    private void Update()
    {
        if (!isActivated || sourceTransform == null) return;

        // rotation 
        Vector2 leftInput = rotateJoystick.action.ReadValue<Vector2>();
        if (Mathf.Abs(leftInput.x) > 0.01f)
        {
            float angle = leftInput.x * rotationSpeed * Time.deltaTime;
            var (axis, _) = axisDirections[activeAxis];
            sourceTransform.Rotate(axis, angle, Space.Self);
        }

        // scale 
        // dominantly horizontal value -> scale on axis that is currently "horizontal" to player 
        // dominantly vertical value -> scale on axis that is currently "vertical" to player 
        Vector2 rightInput = scaleJoystick.action.ReadValue<Vector2>();
        if (rightInput.sqrMagnitude > 0.01f)
        {
            // get dominant direction 
            bool horizontal = Mathf.Abs(rightInput.x) > Mathf.Abs(rightInput.y);
            float input = horizontal ? rightInput.x : rightInput.y;

            // apply scale depending on current axis
            Vector3 scale = sourceTransform.localScale;
            float delta = input * scaleSpeed * Time.deltaTime;

            switch (activeAxis)
            {
                case Axis.X:
                    if (horizontal) scale.z += delta;
                    else scale.y += delta;
                    break;
                case Axis.Y: 
                    if (horizontal) scale.x += delta;
                    else scale.z += delta;
                    break;
                case Axis.Z:
                    if (horizontal) scale.x += delta;
                    else scale.y += delta;
                    break;
            }

            // prevent inverting scale 
            scale.x = Mathf.Max(scale.x, 0.01f);
            scale.y = Mathf.Max(scale.y, 0.01f);
            scale.z = Mathf.Max(scale.z, 0.01f);
            sourceTransform.localScale = scale;
        }
    }
    
    #region callbacks 
    
    private void CycleAxis(InputAction.CallbackContext context)
    {
        CycleAxis();
    }
    
    #endregion
}
