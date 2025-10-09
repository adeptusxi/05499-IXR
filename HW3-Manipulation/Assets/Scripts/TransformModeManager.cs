using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TransformModeManager : MonoBehaviour
{
    [SerializeField] private TransformationEvaluator evaluator;
    [SerializeField] private ConfirmSelect confirmScript;
    [SerializeField] private List<TransformMode> transformModes = new();
    [SerializeField] private InputActionReference[] switchModeTriggers;
    [SerializeField] private GameObject introUI;
    [SerializeField] private GameObject instructionUI;
    [SerializeField] private GameObject confirmUI;
    [SerializeField] private TMP_Text instructionText;

    private int currModeIdx = -1;

    private void Awake()
    {
        confirmScript.OnConfirmTrigger += DeactivateCurrentMode;
        evaluator.onTrialStarted += OnTrialStarted;
        foreach (InputActionReference trigger in switchModeTriggers)
        {
            trigger.action.performed += OnToggle;
        }
        
        //instructionUI.SetActive(false);
        confirmUI.SetActive(false);
    }

    private void OnTrialStarted()
    {
        //instructionUI.SetActive(true);
        confirmUI.SetActive(true);
        introUI.SetActive(false);
        ActivateMode(0);
    }

    private void OnToggle(InputAction.CallbackContext context)
    {
        if (!evaluator.InProgress || transformModes.Count == 0)
            return;

        int nextIndex = (currModeIdx + 1) % transformModes.Count;
        ActivateMode(nextIndex);
    }

    private void ActivateMode(int index)
    {
        if (!evaluator.InProgress) return;
        
        DeactivateCurrentMode();

        currModeIdx = index;
        transformModes[currModeIdx].StartTransformMode();
        instructionText.text = transformModes[currModeIdx].ModeInstructions();
    }

    private void DeactivateCurrentMode()
    {
        if (currModeIdx < 0 || currModeIdx >= transformModes.Count) return;
        transformModes[currModeIdx].StopTransformMode();
    }
}
