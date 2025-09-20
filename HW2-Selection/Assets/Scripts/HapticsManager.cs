using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using System.Collections.Generic;

public class HapticsManager : MonoBehaviour
{
    public static void SendHaptics(XRNode node, float amplitude, float duration)
    {
        var devices = new List<UnityEngine.XR.InputDevice>();
        InputDevices.GetDevicesAtXRNode(node, devices);

        foreach (var device in devices)
        {
            if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
            {
                device.SendHapticImpulse(0, amplitude, duration); 
            }
        }
    }
}
