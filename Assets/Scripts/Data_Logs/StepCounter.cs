using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class StepCounter : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [Tooltip("CenterEyeAnchor / Main Camera transform")]
    public Transform headTransform;

    [Tooltip("3D TextMeshPro object (NOT UGUI)")]
    public TextMeshPro stepText;

    [Header("Step detection (HMD-only)")]
    [Tooltip("Vertical acceleration peak threshold (m/s^2). Lower = more sensitive.")]
    public float stepAccelThreshold = 1.2f;

    [Tooltip("Minimum seconds between steps (prevents double-counting).")]
    public float minStepIntervalSeconds = 0.24f;

    [Header("Smoothing")]
    [Range(1f, 60f)] public float velSmooth = 22f;
    [Range(1f, 60f)] public float accelSmooth = 22f;

    [Header("Step size (stride length estimate)")]
    [Tooltip("Optional: Use PlayerSpeedTracker speed if available (recommended). Otherwise uses HMD-derived horizontal speed.")]
    public bool preferPlayerSpeedTracker = true;

    [Header("Derivative stability")]
    [Tooltip("Clamps dt used for velocity/accel derivatives to reduce hitch spikes.")]
    [Min(0.001f)] public float maxDtForDerivatives = 1f / 30f; // ~33ms

    [Header("Debug")]
    public bool showCadence = true;
    public bool showStepSize = true;

    // internal state
    private Vector3 _prevHeadPos;
    private Vector3 _velSmoothed;
    private Vector3 _prevVelSmoothed;
    private Vector3 _accelSmoothed;

    private float _prevAy, _ay;
    private float _lastStepTime = -1f; // < 0 means "no step yet"

    private int _stepCount;
    private float _cadenceSpm;
    private float _stepInterval;
    private float _stepSizeM; // meters per step (estimated)

    private void Start()
    {
        if (!headTransform)
        {
            Debug.LogError("StepCounter: headTransform is not assigned.");
            enabled = false;
            return;
        }

        if (!stepText)
        {
            Debug.LogError("StepCounter: stepText (TextMeshPro 3D) is not assigned.");
            enabled = false;
            return;
        }

        _prevHeadPos = headTransform.position;
        _prevVelSmoothed = Vector3.zero;
        _stepCount = 0;

        UpdateText();
    }

    private void Update()
    {
        // Clamp dt to reduce derivative spikes on hitches, but keep it non-zero.
        float dt = Mathf.Max(Time.deltaTime, 1e-6f);
        dt = Mathf.Min(dt, maxDtForDerivatives);

        // Position -> Velocity
        Vector3 headPos = headTransform.position;
        Vector3 velRaw = (headPos - _prevHeadPos) / dt;
        _prevHeadPos = headPos;

        float velAlpha = 1f - Mathf.Exp(-velSmooth * dt);
        _velSmoothed = Vector3.Lerp(_velSmoothed, velRaw, velAlpha);

        // Velocity -> Acceleration
        Vector3 accelRaw = (_velSmoothed - _prevVelSmoothed) / dt;
        _prevVelSmoothed = _velSmoothed;

        float accAlpha = 1f - Mathf.Exp(-accelSmooth * dt);
        _accelSmoothed = Vector3.Lerp(_accelSmoothed, accelRaw, accAlpha);

        // Step detection from vertical acceleration peak
        _prevAy = _ay;
        _ay = _accelSmoothed.y;

        bool refractoryOK = (_lastStepTime < 0f) || (Time.time - _lastStepTime) >= minStepIntervalSeconds;
        bool isPeak = (_prevAy > stepAccelThreshold) && (_ay < _prevAy); // local max above threshold

        if (!(isPeak && refractoryOK))
            return;

        // ---- Step event ----
        float now = Time.time;

        // First ever step: initialize timing but DON'T compute cadence/step size from bogus interval.
        if (_lastStepTime < 0f)
        {
            _stepCount++;
            _lastStepTime = now;

            _stepInterval = 0f;
            _cadenceSpm = 0f;
            _stepSizeM = 0f;

            UpdateText();
            return;
        }

        _stepCount++;

        _stepInterval = now - _lastStepTime;
        _lastStepTime = now;

        // Speed source
        float speedMps;
        if (preferPlayerSpeedTracker && PlayerSpeedTracker.Instance != null)
            speedMps = Mathf.Max(0f, PlayerSpeedTracker.Instance.SpeedMps);
        else
            speedMps = new Vector2(_velSmoothed.x, _velSmoothed.z).magnitude;

        // Cadence + step size computed from THIS interval (prevents "stale cadence" bug)
        bool intervalValid = (_stepInterval > 0.10f && _stepInterval < 1.0f);

        if (intervalValid)
        {
            _cadenceSpm = 60f / _stepInterval;

            // stepSize = speed * timePerStep (equivalent to speed / stepFrequency)
            _stepSizeM = speedMps * _stepInterval;
        }
        else
        {
            // Don't carry old cadence forward; otherwise step size becomes wrong.
            _cadenceSpm = 0f;
            _stepSizeM = 0f;
        }

        UpdateText();
    }

    private void UpdateText()
    {
        if (!showCadence && !showStepSize)
        {
            stepText.text = $"Steps: {_stepCount}";
            return;
        }

        string s = $"Steps: {_stepCount}";

        if (showCadence)
            s += $"\nCadence: {_cadenceSpm:F0} spm";

        if (showStepSize)
            s += $"\nStep Size: {_stepSizeM:F2} m";

        stepText.text = s;
    }

    public void ResetSteps()
    {
        _stepCount = 0;
        _cadenceSpm = 0f;
        _stepInterval = 0f;
        _lastStepTime = -1f; // "no step yet"
        _stepSizeM = 0f;

        _prevHeadPos = headTransform ? headTransform.position : Vector3.zero;
        _velSmoothed = Vector3.zero;
        _prevVelSmoothed = Vector3.zero;
        _accelSmoothed = Vector3.zero;
        _prevAy = _ay = 0f;

        UpdateText();
    }
}
