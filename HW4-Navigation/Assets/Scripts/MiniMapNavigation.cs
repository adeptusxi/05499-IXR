using UnityEngine;
using UnityEngine.InputSystem;

public class MiniMapNavigation : MonoBehaviour
{
    private enum Mode
    {
        MoveUser, // directly move user 
        MoveObject // move objectToMove, and let user teleport to it 
    };

    [SerializeField] private RandomRouteEvaluator evaluator;
    
    [Header("User")] 
    [SerializeField] private Transform objectToMove; // user rig or preview object
    [SerializeField] private Transform userRig;
    [SerializeField] private Transform camera;

    [Header("Map")] 
    [SerializeField] private GameObject mapObj;

    [Header("Input")]
    [SerializeField] private InputActionReference leftJoystick;
    [SerializeField] private InputActionReference rightJoystick;
    [SerializeField] private InputActionReference[] teleportTriggers; // if objectToMove isn't user rig, these
                                                                      // triggers teleport userRig to objectToMove on the horizontal plane
    [SerializeField] private InputActionReference[] resetTriggers; // move objectToMove back in front of user 

    [Header("Settings")] 
    [SerializeField] private Mode mode = Mode.MoveUser;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float collisionBuffer = 0.2f; // padding on obstacles for collision detection
    [SerializeField] private float previewOffset = 0.2f; // default offset of objectToMove in front of user 
    [SerializeField] private float mapOffset = 0.2f; // how far in front of user to put map 
    
    private void Start()
    {
        if (mode == Mode.MoveObject)
        {
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
        
        mapObj.SetActive(false);
        evaluator.OnTrialStart += () => mapObj.SetActive(true);
        evaluator.OnTrialEnd += () => mapObj.SetActive(false);
    }
    
    private void Update()
    {
        if (!evaluator.InProgress) return;
        
        //mapObj.transform.position = camera.transform.position + camera.transform.forward * mapOffset;
        
        Vector2 leftValue = leftJoystick.action.ReadValue<Vector2>();
        Vector2 rightValue = rightJoystick.action.ReadValue<Vector2>();

        // combine both joysticks  
        Vector2 combined = leftValue + rightValue;

        if (combined.sqrMagnitude > 0.01f) // ignore joystick drift 
        {
            // get direction relative to camera, restricting movement to horizontal plane
            Vector3 forward = camera.forward;
            Vector3 right = camera.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = forward * combined.y + right * combined.x;
            Vector3 moveStep = moveDirection * (moveSpeed * Time.deltaTime);

            TryMove(moveStep);
        }
    }

    private void TryMove(Vector3 vec)
    {
        // prevent moving through obstacles 
        if (Physics.Raycast(objectToMove.position, vec.normalized, out RaycastHit hit, vec.magnitude + collisionBuffer, obstacleLayer))
        {
            objectToMove.position += vec.normalized * Mathf.Max(0f, hit.distance - collisionBuffer);
        }
        else
        {
            objectToMove.position += vec;
        }
    }

    private void TeleportUser(InputAction.CallbackContext context)
    {
        if (!evaluator.InProgress) return;
        var newUserPos = objectToMove.position;
        newUserPos.y = userRig.position.y;
        userRig.position = newUserPos;
    }

    private void ResetObject(InputAction.CallbackContext context)
    {
        if (!evaluator.InProgress) return;
        Vector3 userForward = camera.forward;
        userForward.y = 0;
        userForward.Normalize();
        
        var newObjPos = userRig.position + userForward * previewOffset;
        newObjPos.y = objectToMove.position.y;
        objectToMove.position = newObjPos;
    }
}
