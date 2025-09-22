using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// extends RaycastSelect (or derived classes) to remap raycast from screenspace to
// a smaller viewport controlled by head direction
public class RaycastInViewportSelect : RaycastInflateSelectDynamic
{
    [Header("Viewport")]
    [SerializeField] private RectTransform viewportRect;
    [SerializeField] private bool showRaycastCursor = false; 
    [SerializeField] private RectTransform raycastCursor; // different from headCursor, which is fixed to center of vision
    [SerializeField] private Canvas raycastCursorCanvas;
    [SerializeField] private RectTransform raycastCursorCanvasRect;

    private Vector3 camForward;
    private Vector3 camRight;
    private Vector3 camUp;
    private Vector3 viewportBL;
    private Vector3 viewportTR;

    protected override void Start()
    {
        base.Start();
        
        if (!showRaycastCursor && raycastCursor != null)
            raycastCursor.gameObject.SetActive(false);

        // cache some variables 
        
        camForward = cam.transform.forward;
        camRight = cam.transform.right;
        camUp = cam.transform.up;
        
        Vector3[] viewportCorners = new Vector3[4];
        viewportRect.GetWorldCorners(viewportCorners);
        viewportBL = cam.WorldToViewportPoint(viewportCorners[0]);
        viewportTR = cam.WorldToViewportPoint(viewportCorners[2]);
    }

    // map original raycast from rayOrigin.position in direction rayOrigin.forward onto viewport 
    protected override Ray GetRay(Transform rayOrigin)
    {
        // project onto camera's local axes 
        float x = Vector3.Dot(rayOrigin.forward, camRight);
        float y = Vector3.Dot(rayOrigin.forward, camUp);
        float z = Vector3.Dot(rayOrigin.forward, camForward);

        // convert camera-space direction into normalized xy coordinates on [-1,1] 
        float ndcX = x / z;
        float ndcY = y / z;

        // map from [-1,1] into [0,1] 
        float u = (ndcX + 1f) * 0.5f;
        float v = (ndcY + 1f) * 0.5f;
        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        // interpolate normalized coordinates within viewport 
        // basically maps camera frustum to viewport 
        float viewportX = Mathf.Lerp(viewportBL.x, viewportTR.x, u);
        float viewportY = Mathf.Lerp(viewportBL.y, viewportTR.y, v);

        // remapped ray originates from the viewport point 
        return cam.ViewportPointToRay(new Vector3(viewportX, viewportY, 0f));
    }

    protected override void OnRaycastHit(RaycastHit hit, Ray ray)
    {
        // this isn't exactly right 
        
        if (!showRaycastCursor) return;
        
        Vector3 screenHitPoint = cam.WorldToScreenPoint(hit.point);
        
        // get local position of hit point relative to cursor's canvas 
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            raycastCursorCanvasRect,
            screenHitPoint,
            cam, 
            out var canvasLocalPoint);
        
        raycastCursor.localPosition = canvasLocalPoint;
        raycastCursor.gameObject.SetActive(true);
    }

    protected override void OnRaycastMiss(Ray ray)
    {
        if (!showRaycastCursor) return;
        raycastCursor.gameObject.SetActive(false);
    }
}
