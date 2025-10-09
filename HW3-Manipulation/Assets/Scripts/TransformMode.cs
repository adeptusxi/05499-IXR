using UnityEngine;

public abstract class TransformMode : MonoBehaviour
{
    public abstract string ModeInstructions();
    public abstract void StartTransformMode(); // setup, not the same as mode-specific activation 
    public abstract void StopTransformMode(); // cleanup 
}