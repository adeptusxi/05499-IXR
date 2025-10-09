using UnityEngine;
using UnityEngine.InputSystem;
using System;

// on input action trigger: 
// (1) resets source's parent 
// (2) broadcasts confirm trigger
// (3) confirms selection with evaluator
public class ConfirmSelect : MonoBehaviour
{
    [SerializeField] private TransformationEvaluator evaluator;
    [SerializeField] private InputActionReference[] confirmTriggers;

    public Action OnConfirmTrigger; // called when buttons are pressed
                                    // AFTER resetting source's parent
                                    // BEFORE confirming selection with evaluator 
    
    private Transform sourceTransform;
    private Transform initialSourceParent;
    
    private void Awake()
    {
        foreach (InputActionReference trigger in confirmTriggers)
        {
            trigger.action.performed += ConfirmSelection;
        }
        sourceTransform = evaluator.GetSourceTransform();
        initialSourceParent = transform.parent;
    }

    public void ConfirmSelection()
    {
        sourceTransform = evaluator.GetSourceTransform();
        sourceTransform.SetParent(initialSourceParent);
        OnConfirmTrigger?.Invoke();
        evaluator.ConfirmSelection(); 
    }
    
    public void ConfirmSelection(InputAction.CallbackContext context)
    {
        ConfirmSelection();
    }
}
