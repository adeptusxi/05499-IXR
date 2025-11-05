using UnityEngine;
using UnityEngine.InputSystem;

public class JoystickNavigation : MonoBehaviour
{
    private enum Mode
    {
        MoveUser, // directly move user 
        MoveObject // move objectToMove, and let user teleport to it 
    };

    [SerializeField] private RandomRouteEvaluator evaluator;
    
    [Header("User")] 
    [SerializeField] private Transform objectToMove; // if mode = MoveObject, move objectToMove 
    [SerializeField] private Transform userRig;
    [SerializeField] private Transform camera;

    [Header("Input")]
    [SerializeField] private InputActionReference vertMvmtJoystick; // y value mapes to y movement 
    [SerializeField] private InputActionReference horizMvmtJoystick; // x/y values map to x/z movement 
    [SerializeField] private InputActionReference[] teleportTriggers; // if objectToMove isn't user rig, these
                                                                      // triggers teleport userRig to objectToMove on the horizontal plane
    [SerializeField] private InputActionReference[] resetTriggers; // move objectToMove back in front of user 

    [Header("Settings")] 
    [SerializeField] private Mode mode = Mode.MoveUser;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float collisionBuffer = 0.2f; // padding on obstacles for collision detection
    [SerializeField] private float previewOffset = 0.2f; // default offset of objectToMove in front of user 
    
    private Transform toMove;
    
    private void Start()
    {
        if (mode == Mode.MoveObject)
        {
            toMove = objectToMove;
            
            foreach (var trigger in teleportTriggers)
            {
                trigger.action.performed += TeleportUser;
            }

            foreach (var trigger in resetTriggers)
            {
                trigger.action.performed += ResetObject;
            }
            
            Vector3 userForward = camera.forward;
            userForward.y = 0;
            userForward.Normalize();
        
            var objPos = userRig.position + userForward * previewOffset;
            objPos.y = objectToMove.position.y;
            objectToMove.position = objPos;
        }
        else
        {
            toMove = userRig;
        }
    }
    
    private void Update()
    {
        if (!evaluator.InProgress) return;
        
        Vector2 vertValue = vertMvmtJoystick.action.ReadValue<Vector2>();
        Vector2 horizValue = horizMvmtJoystick.action.ReadValue<Vector2>();
        
        // ignore joystick drift 
        if (vertValue.sqrMagnitude <= 0.01f) vertValue = Vector2.zero;
        if (horizValue.sqrMagnitude <= 0.01f) horizValue = Vector2.zero;

        if (vertValue.sqrMagnitude > 0.01f || horizValue.sqrMagnitude > 0.01f)
        {
            // get direction relative to camera, restricting movement to horizontal plane
            Vector3 forward = camera.forward;
            Vector3 right = camera.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = forward * horizValue.y + right * horizValue.x + Vector3.up * vertValue.y;
            Vector3 moveStep = moveDirection * (moveSpeed * Time.deltaTime);

            TryMove(moveStep);
        }
    }

    private void TryMove(Vector3 vec)
    {
        // prevent moving through obstacles 
        if (Physics.Raycast(toMove.position, vec.normalized, out RaycastHit hit, vec.magnitude + collisionBuffer, obstacleLayer))
        {
            toMove.position += vec.normalized * Mathf.Max(0f, hit.distance - collisionBuffer);
        }
        else
        {
            toMove.position += vec;
        }
    }

    private void TeleportUser(InputAction.CallbackContext context)
    {
        if (!evaluator.InProgress) return;
        var newUserPos = objectToMove.position;
        newUserPos.y = userRig.position.y;
        userRig.position = newUserPos;
    }

    public void ResetObject()
    {
        if (!evaluator.InProgress) return;
        Vector3 userForward = camera.forward;
        userForward.y = 0;
        userForward.Normalize();
        
        var newObjPos = userRig.position + userForward * previewOffset;
        objectToMove.position = newObjPos;
    }
    
    public void ResetObject(InputAction.CallbackContext context)
    {
        ResetObject();
    }
}
