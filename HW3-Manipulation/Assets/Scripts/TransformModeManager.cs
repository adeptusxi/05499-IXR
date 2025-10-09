using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TransformModeManager : MonoBehaviour
{
    [SerializeField] private TransformationEvaluator evaluator;
    [SerializeField] private ConfirmSelect confirmScript;
    [SerializeField] private List<MonoBehaviour> transformModes = new List<MonoBehaviour>();
    [SerializeField] private InputActionReference switchModeTrigger;

    private readonly List<ITransformMode> modeScripts = new List<ITransformMode>();
    private int currModeIdx = -1;

    private void Awake()
    {
        confirmScript.OnConfirmTrigger += DeactivateCurrentMode;
        switchModeTrigger.action.performed += OnToggle;
        evaluator.onTrialStarted += () => ActivateMode(0);
        
        // validate and cache 
        foreach (var mb in transformModes)
        {
            if (mb is ITransformMode transformMode)
            {
                modeScripts.Add(transformMode);
            }
        }
    }

    private void OnToggle(InputAction.CallbackContext context)
    {
        if (modeScripts.Count == 0)
            return;

        int nextIndex = (currModeIdx + 1) % modeScripts.Count;
        ActivateMode(nextIndex);
    }

    private void ActivateMode(int index)
    {
        DeactivateCurrentMode();

        currModeIdx = index;
        modeScripts[currModeIdx].StartTransformMode();
    }

    private void DeactivateCurrentMode()
    {
        if (currModeIdx < 0 || currModeIdx >= modeScripts.Count) return;
        modeScripts[currModeIdx].StopTransformMode();
    }
}
