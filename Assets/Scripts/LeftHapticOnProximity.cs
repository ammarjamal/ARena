using System.Collections;
using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
public class LeftHapticOnProximity : MonoBehaviour
{
[Header("Tags + distance")]
    public string tagX = "Player";
    public string tagY = "Car";
    [Min(0f)] public float triggerDistanceMeters = 10f;

    [Header("Haptics (OVR)")]
    [Min(0f)] public float vibrateSeconds = 0.5f;
    [Range(0f, 1f)] public float amplitude = 0.7f;
    [Range(0f, 1f)] public float frequency = 1.0f;

    bool _wasInside;
    bool _isVibrating;

    void Update()
    {
        var X = GameObject.FindGameObjectWithTag(tagX);
        var Y = GameObject.FindGameObjectWithTag(tagY);
        if (X == null || Y == null) return;

        float d = Vector3.Distance(X.transform.position, Y.transform.position);
        bool inside = d <= triggerDistanceMeters;

        // one-shot when entering range
        if (inside && !_wasInside && !_isVibrating)
        {
            StartCoroutine(VibrateLeftOnce());
        }

        _wasInside = inside;
    }

    IEnumerator VibrateLeftOnce()
    {
        _isVibrating = true;

        // Start vibration on LEFT controller
        OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.LTouch);

        yield return new WaitForSeconds(vibrateSeconds);

        // Stop vibration
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);

        _isVibrating = false;
    }
}
