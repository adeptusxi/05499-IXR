using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class MiniMapNavigation : MonoBehaviour
{    
    [Serializable]
    public class MapMarker
    {
        public enum TrackedObject
        {
            User, 
            Target, 
            Other // use worldObject 
        }

        public TrackedObject trackedObject;
        public Transform worldObject; // object being tracked if trackedObject = Other 
        public RectTransform mapIcon; // icon on map to indicate object 
        public bool useRotation = false; // if true, map icon faces corresponding direction as worldObject
        public float rotationOffsetDeg; // if useRotation, how much to offset (e.g. if image is rotated already)
    }
    
    [SerializeField] private RandomRouteEvaluator evaluator;
    [SerializeField] private JoystickNavigation joystickNav;
    
    [Header("User")] 
    [SerializeField] private Transform objectToMove; // user rig or preview object
    [SerializeField] private Transform userRig;
    [SerializeField] private Transform userCamera;

    [Header("Map")] 
    [SerializeField] private GameObject mapObj;
    [SerializeField] private RectTransform mapRect;
    [SerializeField] private List<MapMarker> markers;
    [SerializeField] private Transform worldMin; // world-space position of top left of map
    [SerializeField] private Transform worldMax; // world-space position of bottom right of map

    [Header("Teleportation")]
    [SerializeField] private InputActionReference[] teleportTriggers;
    [SerializeField] private LayerMask landableMask; // layers to land on 
    [SerializeField] private float raycastDistance = 100f; // how high above y=0 to raycast for landable ground
    [SerializeField] private Transform rayOrigin; 
    [SerializeField] private LineRenderer rayRenderer;
    [SerializeField] private RectTransform raycastIcon;
    
    [Header("Settings")] 
    [SerializeField] private float mapOffset = 0.2f; // how far in front of user to put map 

    private bool isHittingMap = false;
    private Vector3 hitMapPoint;
    
    private void Start()
    {
        mapObj.SetActive(false);
        raycastIcon.gameObject.SetActive(false);
        evaluator.OnTrialStart += () => mapObj.SetActive(true);
        evaluator.OnTrialEnd += () => mapObj.SetActive(false);
        
        foreach (var trigger in teleportTriggers)
        {
            trigger.action.performed += TryTeleport;
        }
    }
    
    private void Update()
    {
        if (!evaluator.InProgress) return;

        foreach (var marker in markers)
        {
            UpdateMapMarker(marker);
        }

        MapRaycast();
    }

    private void UpdateMapMarker(MapMarker marker)
    {
        Transform obj;
        switch (marker.trackedObject)
        {
            case MapMarker.TrackedObject.User:
                obj = userCamera;
                break;
            case MapMarker.TrackedObject.Target:
                obj = evaluator.ActiveWaypointTransform;
                break;
            default:
                obj = marker.worldObject;
                break;
        }
        
        // project object onto map UV coords 
        Vector3 pos = obj.position;
        float u = Mathf.InverseLerp(worldMin.transform.position.x, worldMax.transform.position.x, pos.x);
        float v = Mathf.InverseLerp(worldMin.transform.position.z, worldMax.transform.position.z, pos.z);
        
        // convert to position on map 
        Vector2 anchoredPos = new Vector2(
            (u - 0.5f) * mapRect.rect.width,
            (v - 0.5f) * mapRect.rect.height
        );
        
        // update marker position 
        marker.mapIcon.localPosition = anchoredPos;
        
        // rotate marker 
        if (marker.useRotation)
        {
            // match icon's Z rotation with object's Y rotation 
            float worldYaw = obj.eulerAngles.y;
            marker.mapIcon.localRotation = Quaternion.Euler(0f, 0f, -worldYaw + marker.rotationOffsetDeg);
        }
    }

    private void MapRaycast()
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 50f))
        {
            if (hit.collider.gameObject == mapObj)
            {
                // convert 3D hit point to UV coordinates on map rect 
                Vector3 localPoint = mapRect.InverseTransformPoint(hit.point);
                Vector2 uv = new Vector2(
                    (localPoint.x + mapRect.rect.width * 0.5f) / mapRect.rect.width,
                    (localPoint.y + mapRect.rect.height * 0.5f) / mapRect.rect.height
                );
                
                isHittingMap = true;
                hitMapPoint = UVToWorld(uv);
                
                if (rayRenderer != null)
                {
                    rayRenderer.enabled = true;
                    rayRenderer.SetPosition(0, ray.origin);
                    rayRenderer.SetPosition(1, hit.point);
                    raycastIcon.transform.position = hit.point;
                    raycastIcon.gameObject.SetActive(true);
                }
            }
            else
            {
                rayRenderer.enabled = false;
                isHittingMap = false;
                raycastIcon.gameObject.SetActive(false);
            }
        }
        else
        {
            rayRenderer.enabled = false;
            isHittingMap = false;
            raycastIcon.gameObject.SetActive(false);
        }
    }

    private void TryTeleport(InputAction.CallbackContext context)
    {
        if (!isHittingMap) return;
        
        Vector3 teleportRayOrigin = hitMapPoint + Vector3.up * raycastDistance;
        if (Physics.Raycast(teleportRayOrigin, Vector3.down, out RaycastHit hit, raycastDistance * 2f, landableMask))
        {
            Vector3 destination = hit.point;
            userRig.position = destination;
            joystickNav.ResetObject();
        }
    }
    
    // convert uv point to world coordinate with corresponding x and z (y = 0)
    private Vector3 UVToWorld(Vector2 mapUV)
    {
        float worldX = Mathf.Lerp(worldMin.position.x, worldMax.position.x, mapUV.x);
        float worldZ = Mathf.Lerp(worldMin.position.z, worldMax.position.z, mapUV.y);
        float worldY = 0f;
        return new Vector3(worldX, worldY, worldZ);
    }
}
