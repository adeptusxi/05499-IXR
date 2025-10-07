using UnityEngine;
using UnityEngine.InputSystem;

// joystick-based forward/backward motion in the camera's view direction 
// options for how to combine two joysticks (combine linearly for camera forward/backward, or use one to move up/down) 
// option to translate source object by parenting it to the user 
// assumes confirmScript will handle reparenting in OnConfirmTrigger
public class CameraDirectionJoystickTranslate : MonoBehaviour
{
    [SerializeField] private TransformationEvaluator evaluator;
    [SerializeField] private ConfirmSelect confirmScript;

    [SerializeField] private bool moveSource; // whether to translate object when user moves 
    [SerializeField] private JoystickType joystickType = JoystickType.LeftVertical;
    
    [Header("User")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform userRoot;
    [SerializeField] private float userScaleMultiplier = 1f;

    [Header("Input")]
    [SerializeField] private InputActionReference leftJoystick;
    [SerializeField] private InputActionReference rightJoystick;
    [SerializeField] private float joystickDrift = 0.01f; // max joystick input value to ignore 
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float verticalMoveSpeed = 2.5f; // for LeftVertical/RightVertical modes 
    [SerializeField] private Vector3 viewOffset = new(0, 0, 1); // move user so that cube is here relative to camera 

    private Transform sourceTransform;
    private Transform initialSourceParent;
    private Vector3 originalUserPosition;
    private Quaternion originalCameraRotation;
    private Vector3 originalUserScale;
    private bool isActivated = false;

    public enum JoystickType
    {
        BothCamerDir, // both joysticks' x/y values move user sideways/forwards along camera direction 
        LeftVertical, // left joystick's y value moves user along world y-axis  
        RightVertical // right joystick's y value moves user along world y-axis  
    }

    private void Awake()
    {
        evaluator.onTrialStarted += ActivateTranslation;
        confirmScript.OnConfirmTrigger += DeactivateTranslation;
        
        initialSourceParent = evaluator.GetSourceTransform().parent;
    }
    
    private void Update()
    {
        if (!isActivated) return;

        // get input 
        Vector2 leftJoystickInput = Vector2.zero;
        Vector2 rightJoystickInput = Vector2.zero;
        float joystickVerticalInput = 0f;
        
        if (leftJoystick != null)
            leftJoystickInput = leftJoystick.action.ReadValue<Vector2>();
        if (rightJoystick != null)
            rightJoystickInput = rightJoystick.action.ReadValue<Vector2>();
        
        // special behavior for applicable modes 
        if (joystickType == JoystickType.LeftVertical)
        {
            leftJoystickInput.x = 0;
            joystickVerticalInput = leftJoystickInput.y;
        } else if (joystickType == JoystickType.RightVertical)
        {
            rightJoystickInput.x = 0;
            joystickVerticalInput =  rightJoystickInput.y;
        }
        
        Vector2 joystickInput = leftJoystickInput + rightJoystickInput;
        if (joystickInput.sqrMagnitude < joystickDrift)
        {
            // don't do anything if joysticks not activated 
            if (sourceTransform.parent == cameraTransform)
            {
                sourceTransform.SetParent(initialSourceParent);
            }
            return;
        }

        if (moveSource && sourceTransform.parent != cameraTransform)
        {
            sourceTransform.SetParent(cameraTransform, true);
        }
        // move user 
        Vector3 cameraForward = cameraTransform.forward.normalized;
        Vector3 cameraRight = cameraTransform.right.normalized;
        Vector3 move = (cameraForward * joystickInput.y + cameraRight * joystickInput.x) * (moveSpeed * Time.deltaTime) 
                       + Vector3.up * (joystickVerticalInput * (verticalMoveSpeed * Time.deltaTime));
        userRoot.position += move;
    }
    
    private void ActivateTranslation()
    {
        sourceTransform = evaluator.GetSourceTransform();
        
        originalUserPosition = userRoot.position;
        originalCameraRotation = cameraTransform.rotation;
        originalUserScale = userRoot.localScale;
        
        // scale user 
        userRoot.localScale *= userScaleMultiplier;
        
        // move user to source 
        Vector3 rootCameraOffset = cameraTransform.localPosition;
        userRoot.position = sourceTransform.position - viewOffset - rootCameraOffset;
        cameraTransform.LookAt(sourceTransform);
        
        isActivated = true;
    }
    
    private void DeactivateTranslation()
    {
        if (!isActivated) return;
        isActivated = false;

        // restore user position and scale 
        userRoot.position = originalUserPosition;
        cameraTransform.rotation = originalCameraRotation;
        userRoot.localScale = originalUserScale;
    }
}
