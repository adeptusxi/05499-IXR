using UnityEngine;
using UnityEngine.InputSystem;

public class JoystickRotate : MonoBehaviour
{
    [Header("References")]
    public InputActionReference leftJoystick; 
    public Transform objectToRotate; 
    public float rotationSpeed = 100f; 

    void OnEnable()
    {
        leftJoystick.action.Enable();
    }

    void OnDisable()
    {
        leftJoystick.action.Disable();
    }

    void Update()
    {
        Vector2 input = leftJoystick.action.ReadValue<Vector2>();
        float horizontal = input.x; // positive `horizontal` rotates counter-clockwise, negative x clockwise
        
        // rotate about y-axis 
        objectToRotate.Rotate(Vector3.up, horizontal * -rotationSpeed * Time.deltaTime, Space.World);
    }
}