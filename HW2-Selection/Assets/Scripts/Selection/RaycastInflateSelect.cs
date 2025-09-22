using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

// extends RaycastSelect to inflate the nearest sphere if none are selected 
public class RaycastInflateSelect : RaycastSelect
{
    [Header("Inflation")]
    [SerializeField] private float inflateScale = 1.5f; 
    
    protected Transform inflatedSphere;   // reference to currently inflated sphere so we can reset its size 
    protected Vector3 originalScale;      // original size of currently inflated sphere 

    protected override void OnRaycastHitSphere(RaycastHit hit, Ray ray, Transform sphere)
    {
        if (inflatedSphere != sphere)
            InflateSphere(sphere, ray);
    } 
    
    protected override void OnRaycastMissSphere(Ray ray)
    {
        // if no sphere is hit, inflate the one closest to being hit 
        var nearestSphere = GetNearestSphereCenterToRay(ray);
        if (nearestSphere != null)
        {
            InflateSphere(nearestSphere, ray);
            SetSelected(nearestSphere);
        }
    }

    protected override void OnRaycastHitDifferentSphere(RaycastHit hit, Ray ray, Transform sphere)
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
    
    protected void ResetInflatedSphere()
    {
        if (inflatedSphere != null)
        {
            inflatedSphere.localScale = originalScale;
            originalScale = Vector3.zero;
            inflatedSphere = null;
        }
    }

    // inflates sphere by constant inflateScale 
    protected virtual void InflateSphere(Transform sphere, Ray ray)
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
