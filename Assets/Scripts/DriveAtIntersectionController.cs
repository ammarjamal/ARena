using UnityEngine;

[DisallowMultipleComponent]
public class DriveAtIntersectionController : MonoBehaviour
{
    [Header("Behaviour")]
    public bool Yield = true;
    public float yieldDistanceMeters = 20f;
    public float nonYieldExtraMeters = 20f;

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private WheelCollider wcFL;
    [SerializeField] private WheelCollider wcFR;
    [SerializeField] private WheelCollider wcRL;
    [SerializeField] private WheelCollider wcRR;

    [Header("Motion")]
    public float maxSpeedKmh = 45f;
    public float comfortableDecel = 6f;
    public float maxMotorTorque = 900f;
    public float maxBrakeTorque = 4000f;
    public float lowSpeedThreshold = 0.8f;
    public float holdBrakeTorque = 2000f;

    private Vector3 startPos;
    private float targetDistance;
    private bool driving;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
    }

    public void StartDriving()
    {
        startPos = rb.position;

        // THIS IS THE ONLY LOGIC YOU ASKED FOR
        targetDistance = Yield
            ? yieldDistanceMeters
            : yieldDistanceMeters + nonYieldExtraMeters;

        driving = true;
    }

    private void FixedUpdate()
    {
        if (!driving) return;

        float travelled = Vector3.Distance(startPos, rb.position);
        float remaining = Mathf.Max(targetDistance - travelled, 0f);

        float currentSpeed = rb.linearVelocity.magnitude;
        float maxSpeedMs = maxSpeedKmh / 3.6f;

        // Smooth stop envelope (same for yield & non-yield)
        float targetSpeed =
            Mathf.Min(maxSpeedMs, Mathf.Sqrt(2f * comfortableDecel * remaining));

        ApplySpeedTracking(targetSpeed, currentSpeed);

        // Stop controlling once target reached
        if (remaining < 0.05f && currentSpeed < 0.05f)
        {
            wcRL.motorTorque = 0f;
            wcRR.motorTorque = 0f;
            ApplyBrake(holdBrakeTorque);
            driving = false;
        }
    }

    private void ApplySpeedTracking(float targetSpeed, float currentSpeed)
    {
        float error = targetSpeed - currentSpeed;

        if (error < 0f)
        {
            wcRL.motorTorque = 0f;
            wcRR.motorTorque = 0f;

            float brakeStrength =
                Mathf.InverseLerp(0f, lowSpeedThreshold, currentSpeed);

            float brake01 =
                Mathf.Clamp01(-error / Mathf.Max(targetSpeed, 0.5f));

            ApplyBrake(brake01 * brakeStrength * maxBrakeTorque);
        }
        else
        {
            float torque = Mathf.Clamp(error * 200f, 0f, maxMotorTorque);
            wcRL.motorTorque = torque;
            wcRR.motorTorque = torque;
            ApplyBrake(0f);
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
