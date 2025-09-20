using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

// extends RaycastSelect to inflate the nearest sphere if none are selected 
public class RaycastInflateSelect : RaycastSelect
{
    [Header("Inflation")]
    [SerializeField] private Camera cam;
    [SerializeField] private float inflateScale = 1.5f; 
    
    private Transform inflatedSphere;   // reference to currently inflated sphere so we can reset its size 
    private Vector3 originalScale;      // original size of currently inflated sphere 

    protected override void OnRaycastHit(Ray ray, Transform sphere)
    {
        if (inflatedSphere != sphere)
            InflateSphere(ray, sphere);
    } 
    
    protected override void OnRaycastMiss(Ray ray)
    {
        // if no sphere is hit, inflate the one closest to being hit 
        //var nearestSphere = GetNearestSphereToRay(ray);
        var nearestSphere = GetNearestSphereCenterToRay(ray);
        if (inflatedSphere != nearestSphere)
            InflateSphere(ray, nearestSphere);
        SetSelected(nearestSphere);
    }

    protected override void OnRaycastDifferentHit(Ray ray, Transform sphere)
    {
        ResetInflatedSphere();
    }
    
    private Transform GetNearestSphereCenterToRay(Ray ray)
    {
        Transform nearest = null;
        float minSqrDistance = Mathf.Infinity;

        foreach (var sphere in selectionEvaluator.GetSpheres())
        {
            Vector3 spherePos = sphere.position;
            // closest point on ray to sphere center
            Vector3 toSphere = spherePos - ray.origin;
            Vector3 closestPoint = ray.origin + Vector3.Project(toSphere, ray.direction);
            // squared distance from ray to sphere center (ignoring radius)
            float sqrDistance = (closestPoint - spherePos).sqrMagnitude;

            if (sqrDistance < minSqrDistance)
            {
                minSqrDistance = sqrDistance;
                nearest = sphere;
            }
        }

        return nearest;
    }

    private Transform GetNearestSphereToRay(Ray ray)
    {
        Transform nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (var sphere in selectionEvaluator.GetSpheres())
        {
            Vector3 spherePos = sphere.position;
            float radius = sphere.localScale.x * 0.5f; 
            // closest point on ray to sphere center
            Vector3 closestPoint = ray.origin + Vector3.Project(spherePos - ray.origin, ray.direction);
            // distance from ray to sphere surface (sphere radii are not uniform) 
            float distance = Vector3.Distance(closestPoint, spherePos) - radius;
            
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = sphere;
            }
        }

        return nearest;
    }

    private void InflateSphere(Ray ray, Transform sphere)
    {
        InflateSphereStatic(sphere);
    }

    private void ResetInflatedSphere()
    {
        if (inflatedSphere != null)
            inflatedSphere.localScale = originalScale;
    }

    // inflates sphere by constant inflateScale 
    private void InflateSphereStatic(Transform sphere)
    {
        ResetInflatedSphere();

        // store new sphere's original scale and then inflate 
        inflatedSphere = sphere;
        if (sphere != null)
        {
            originalScale = sphere.localScale;
            sphere.localScale = originalScale * inflateScale;
        }
    }
}
