using UnityEngine;
using UnityEngine.InputSystem;

// joystick-based forward/backward motion in the camera's view direction 
// (two activated joysticks combine linearly)  
// translates source object by parenting it to the user 
public class CameraDirectionJoystickTranslate : MonoBehaviour
{
    [SerializeField] private TransformationEvaluator evaluator;
    
    [Header("User")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform userRoot;
    [SerializeField] private float userScaleMultiplier = 1f;

    [Header("Input")]
    public InputActionReference primaryButtonPress;
    public InputActionReference secondaryButtonPress;
    [SerializeField] private InputActionReference leftJoystick;
    [SerializeField] private InputActionReference rightJoystick;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float viewOffset = 1.4f; // distance in front of the cube to place user at

    private Transform sourceTransform;
    private Transform originalSourceParent;
    private Vector3 originalUserPosition;
    private Quaternion originalUserRotation;
    private Vector3 originalUserScale;
    private bool isActivated = false;

    void Awake()
    {
        evaluator.onTrialStarted += ActivateTranslation;
        
        primaryButtonPress.action.performed += ConfirmSelection;
        secondaryButtonPress.action.performed += ConfirmSelection;
    }
    
    private void Update()
    {
        if (!isActivated) return;

        // get input 
        float joystickInput = 0f;
        if (leftJoystick != null)
            joystickInput += leftJoystick.action.ReadValue<Vector2>().y;
        if (rightJoystick != null)
            joystickInput += rightJoystick.action.ReadValue<Vector2>().y;

        // ignore joystick drift 
        if (Mathf.Approximately(joystickInput, 0f))
            return;

        // get camera direction 
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.Normalize();

        // move user 
        userRoot.position += joystickInput * moveSpeed * Time.deltaTime * cameraForward;
    }
    
    private void ActivateTranslation()
    {
        sourceTransform = evaluator.GetSourceTransform();
        
        originalUserPosition = userRoot.position;
        originalUserRotation = userRoot.rotation;
        originalUserScale = userRoot.localScale;
        
        // scale user 
        userRoot.localScale *= userScaleMultiplier;
        
        // move user to source 
        userRoot.position = sourceTransform.position + Vector3.forward * viewOffset;
        userRoot.LookAt(sourceTransform);

        // parent source to user 
        originalSourceParent = sourceTransform.parent;
        sourceTransform.SetParent(cameraTransform, worldPositionStays: true);
        
        isActivated = true;
    }
    
    private void DeactivateTranslation()
    {
        if (!isActivated) return;
        isActivated = false;

        // restore source's parent 
        sourceTransform.SetParent(originalSourceParent, worldPositionStays: true);
        
        // restore user position and scale 
        userRoot.position = originalUserPosition;
        userRoot.rotation = originalUserRotation;
        userRoot.localScale = originalUserScale;
    }
    
    void ConfirmSelection(InputAction.CallbackContext context)
    {
        DeactivateTranslation();
        evaluator.ConfirmSelection();
    }
}
