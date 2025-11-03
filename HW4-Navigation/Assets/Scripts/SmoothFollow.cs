using UnityEngine;

// imitates child-parent following 
public class SmoothFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset; // z: how far in front of target, x/y: offset from target's forward 
    public float followSpeed = 5f; 

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + target.forward * offset.z + target.right * offset.x + target.up * offset.y;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
        
        // face object 
        Quaternion targetRot = Quaternion.LookRotation(-target.forward, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);
    }
}