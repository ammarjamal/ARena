using UnityEngine;

[DisallowMultipleComponent]
public class YieldAtIntersection : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private WheelCollider wcFL;
    [SerializeField] private WheelCollider wcFR;
    [SerializeField] private WheelCollider wcRL;
    [SerializeField] private WheelCollider wcRR;

    [Header("Motion constraints")]
    [Tooltip("Maximum allowed speed (km/h), not constant")]
    [SerializeField] private float maxSpeedKmh = 45f;

    [Tooltip("Distance from start to full stop (m)")]
    [SerializeField] private float stopDistanceMeters = 20f;

    [Header("Driver comfort")]
    [Tooltip("Comfortable deceleration (m/s²). 5–7 is realistic.")]
    [SerializeField] private float comfortableDecel = 6f;

    [SerializeField] private float maxMotorTorque = 900f;
    [SerializeField] private float maxBrakeTorque = 4000f;

    [Header("Low-speed smoothing")]
    [Tooltip("Below this speed (m/s), reduce brake aggressiveness")]
    [SerializeField] private float lowSpeedThreshold = 0.8f;

    [Tooltip("Brake holding torque after stop")]
    [SerializeField] private float holdBrakeTorque = 2000f;

    private Vector3 startPos;
    private bool finished;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        startPos = rb.position;
    }

    private void FixedUpdate()
    {
        if (finished) return;

        float travelled = Vector3.Distance(startPos, rb.position);
        float remaining = Mathf.Max(stopDistanceMeters - travelled, 0f);

        float currentSpeed = rb.linearVelocity.magnitude;   // m/s
        float maxSpeedMs = maxSpeedKmh / 3.6f;

        // --- Desired speed profile (guarantees stop at exactly 20 m) ---
        float targetSpeed =
            Mathf.Min(maxSpeedMs, Mathf.Sqrt(2f * comfortableDecel * remaining));

        ApplySpeedTracking(targetSpeed, currentSpeed, remaining);
    }

    private void ApplySpeedTracking(float targetSpeed, float currentSpeed, float remaining)
    {
        float error = targetSpeed - currentSpeed;

        // -------- Above target speed → brake --------
        if (error < 0f)
        {
            wcRL.motorTorque = 0f;
            wcRR.motorTorque = 0f;

            // Reduce brake aggressiveness at low speed to avoid hop
            float brakeStrength =
                Mathf.InverseLerp(0f, lowSpeedThreshold, currentSpeed);

            float brake01 = Mathf.Clamp01(-error / Mathf.Max(targetSpeed, 0.5f));
            ApplyBrake(brake01 * brakeStrength * maxBrakeTorque);
        }
        // -------- Below target speed → light throttle --------
        else
        {
            float torque = Mathf.Clamp(error * 200f, 0f, maxMotorTorque);
            wcRL.motorTorque = torque;
            wcRR.motorTorque = torque;
            ApplyBrake(0f);
        }

        // -------- Final rest (no snapping) --------
        if (remaining < 0.05f && currentSpeed < 0.05f)
        {
            wcRL.motorTorque = 0f;
            wcRR.motorTorque = 0f;
            ApplyBrake(holdBrakeTorque);
            finished = true;
            enabled = false;
        }
    }

    private void ApplyBrake(float torque)
    {
        wcFL.brakeTorque = torque;
        wcFR.brakeTorque = torque;
        wcRL.brakeTorque = torque;
        wcRR.brakeTorque = torque;
    }
}
