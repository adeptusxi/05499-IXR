using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

// extends RaycastSelect to inflate the nearest sphere if none are selected 
public class RaycastInflateSelect : RaycastSelect
{
    [Header("Inflation")]
    [SerializeField] private float inflateScale = 1.5f; 
    
    private Transform inflatedSphere;   // reference to currently inflated sphere so we can reset its size 
    private Vector3 originalScale;      // original size of currently inflated sphere 

    protected override void OnRaycastHit(Ray ray, Transform sphere)
    {
        InflateSphereDynamic(sphere, ray);
    } 
    
    protected override void OnRaycastMiss(Ray ray)
    {
        // if no sphere is hit, inflate the one closest to being hit 
        var nearestSphere = GetNearestSphereCenterToRay(ray);
        if (nearestSphere != null)
        {
            InflateSphereDynamic(nearestSphere, ray);
            SetSelected(nearestSphere);
        }
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
    
    private void ResetInflatedSphere()
    {
        if (inflatedSphere != null)
        {
            inflatedSphere.localScale = originalScale;
            originalScale = Vector3.zero;
            inflatedSphere = null;
        }
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
    
    // inflates sphere to be just hit by ray 
    private void InflateSphereDynamic(Transform sphere, Ray ray)
    {
        // if inflating a new sphere, reset the old one 
        if (inflatedSphere != sphere)
            ResetInflatedSphere();

        // if we haven't already, store the original scale 
        if (inflatedSphere != sphere || originalScale == Vector3.zero)
            originalScale = sphere.localScale;

        float originalRadius = originalScale.x * 0.5f;

        // calculate closest point to ray (from viewer POV, not actually) 
        Vector3 toSphere = sphere.position - ray.origin;
        float proj = Vector3.Dot(toSphere, ray.direction.normalized);
        Vector3 closestPoint = ray.origin + proj * ray.direction.normalized;
        float dist = Vector3.Distance(closestPoint, sphere.position);

        // inflate sphere so it's just big enough for ray to hit 
        if (dist <= originalRadius)
        {
            sphere.localScale = originalScale;
        }
        else
        {
            float inflateFactor = dist / originalRadius; 
            inflateFactor = Mathf.Min(inflateFactor, 5f/originalScale.x); // don't get too huge 
            sphere.localScale = originalScale * inflateFactor;
        }
        
        inflatedSphere = sphere;
    }

}
