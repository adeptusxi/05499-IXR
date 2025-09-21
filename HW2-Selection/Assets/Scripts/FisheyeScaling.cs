using UnityEngine;
using System.Collections.Generic;

public class FisheyeScaling : MonoBehaviour
{
    [SerializeField] protected SelectionEvaluator selectionEvaluator;
    [SerializeField] private GameObject evaluationUI;
    [SerializeField] protected Camera cam;
    
    [SerializeField] private float fisheyeAngle = 30f;  // degrees around camera center
    [SerializeField] private float maxScaleMultiplier = 1.5f; // max scale for nearby objects

    private Dictionary<Transform, Vector3> originalScales = new();
    
    protected void Update()
    {
        if (!evaluationUI.activeSelf) // don't apply effect if trial hasn't started 
            ApplyFisheyeEffect();
    }

    // scales spheres close to center of vision up to create the illusion 
    // don't use this !! not good, makes it harder to select since spheres overlap when they grow
    private void ApplyFisheyeEffect()
    {
        if (selectionEvaluator == null) return;

        Transform[] spheres = selectionEvaluator.GetSpheres().ToArray();
        Vector3 camForward = cam.transform.forward;
        Vector3 camPosition = cam.transform.position;

        foreach (var sphere in spheres)
        {
            if (!originalScales.ContainsKey(sphere))
                originalScales[sphere] = sphere.localScale;

            Vector3 toSphere = (sphere.position - camPosition).normalized;
            float angle = Vector3.Angle(camForward, toSphere);

            if (angle < fisheyeAngle)
            {
                float t = 1f - (angle / fisheyeAngle);
                sphere.localScale = originalScales[sphere] * Mathf.Lerp(1f, maxScaleMultiplier, t);
            }
            else
            {
                sphere.localScale = originalScales[sphere];
            }
        }
    }

}
