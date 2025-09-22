using UnityEngine;

// extends RaycastInflateSelect to inflate the nearest sphere dynamically 
public class RaycastInflateSelectDynamic : RaycastInflateSelect
{
    [Header("Dynamic Inflation")]
    [SerializeField] private float maxInflatedDiamater = 5f;
    
    protected override void OnRaycastHitSphere(RaycastHit hit, Ray ray, Transform sphere)
    {
        InflateSphere(sphere, ray);
    } 
    
    // inflates sphere to be just hit by ray 
    protected override void InflateSphere(Transform sphere, Ray ray)
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
            inflateFactor = Mathf.Min(inflateFactor, maxInflatedDiamater/originalScale.x); // don't get too huge 
            sphere.localScale = originalScale * inflateFactor;
        }
        
        inflatedSphere = sphere;
    }
}
