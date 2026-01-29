using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class LeftHapticOnProximity : MonoBehaviour
{
    [Header("Distance (from TagDistanceService)")]
    [Min(0f)] public float triggerDistanceMeters = 10f;

    [Header("Haptics (OVR)")]
    [Min(0f)] public float vibrateSeconds = 0.5f;
    [Range(0f, 1f)] public float amplitude = 0.7f;
    [Range(0f, 1f)] public float frequency = 1.0f;

    private bool _wasInside;
    private bool _isVibrating;

    private void Update()
    {
        var distSvc = TagDistanceService.Instance;
        if (distSvc == null) return;

        // If targets are missing, treat as "not inside"
        bool inside = distSvc.HasValidTargets && distSvc.IsWithin(triggerDistanceMeters);

        // One-shot when entering range
        if (inside && !_wasInside && !_isVibrating)
        {
            StartCoroutine(VibrateLeftOnce());
        }

        _wasInside = inside;
    }

    private IEnumerator VibrateLeftOnce()
    {
        _isVibrating = true;

        // Start vibration on LEFT controller
        OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.LTouch);

        yield return new WaitForSeconds(vibrateSeconds);

        // Stop vibration
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);

        _isVibrating = false;
    }

    // Safety: ensure vibration is stopped if object disables/destroys mid-vibration
    private void OnDisable()
    {
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        _isVibrating = false;
        _wasInside = false;
    }

    private void OnDestroy()
    {
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
    }
}
