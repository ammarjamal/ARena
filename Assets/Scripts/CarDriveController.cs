using UnityEngine;

[DisallowMultipleComponent]
public class CarDriveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private WheelCollider wcFL;
    [SerializeField] private WheelCollider wcFR;
    [SerializeField] private WheelCollider wcRL;
    [SerializeField] private WheelCollider wcRR;

    [Header("Motion constraints")]
    [SerializeField] private float maxSpeedKmh = 45f;

    [Header("Driver comfort")]
    [SerializeField] private float comfortableDecel = 6f;
    [SerializeField] private float maxMotorTorque = 900f;
    [SerializeField] private float maxBrakeTorque = 4000f;

    [Header("Low-speed smoothing")]
    [SerializeField] private float lowSpeedThreshold = 0.8f;
    [SerializeField] private float holdBrakeTorque = 2000f;

    private Vector3 startPos;
    private float targetDistance;
    private Mode mode;
    private bool active;

    private enum Mode { Idle, Yield, DriveThrough }

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        mode = Mode.Idle;
        active = false;
    }

    public void StartYield(float stopDistanceMeters)
    {
        startPos = rb.position;
        targetDistance = Mathf.Max(0f, stopDistanceMeters);
        mode = Mode.Yield;
        active = true;
        enabled = true;
    }

    public void DriveThrough(float driveDistanceMeters)
    {
        startPos = rb.position;
        targetDistance = Mathf.Max(0f, driveDistanceMeters);
        mode = Mode.DriveThrough;
        active = true;
        enabled = true;
    }

    private void FixedUpdate()
    {
        if (!active) return;

        float travelled = Vector3.Distance(startPos, rb.position);
        float remaining = Mathf.Max(targetDistance - travelled, 0f);
        float currentSpeed = rb.linearVelocity.magnitude;
        float maxSpeedMs = maxSpeedKmh / 3.6f;

        if (mode == Mode.Yield)
        {
            float targetSpeed = Mathf.Min(maxSpeedMs, Mathf.Sqrt(2f * comfortableDecel * remaining));
            ApplySpeedTracking(targetSpeed, currentSpeed, remaining);

            if (remaining < 0.05f && currentSpeed < 0.05f)
            {
                StopAndHold();
            }
        }
        else if (mode == Mode.DriveThrough)
        {
            DriveUpToMaxSpeed(maxSpeedMs, currentSpeed);

            if (remaining <= 0.01f)
            {
                StopAndDeactivate();
            }
        }
    }

    private void DriveUpToMaxSpeed(float maxSpeedMs, float currentSpeedMs)
    {
        float error = maxSpeedMs - currentSpeedMs;

        if (error > 0f)
        {
            float torque = Mathf.Clamp(error * 200f, 0f, maxMotorTorque);
            wcRL.motorTorque = torque;
            wcRR.motorTorque = torque;
        }
        else
        {
            wcRL.motorTorque = 0f;
            wcRR.motorTorque = 0f;
        }

        ApplyBrake(0f);
    }

    private void ApplySpeedTracking(float targetSpeed, float currentSpeed, float remaining)
    {
        float error = targetSpeed - currentSpeed;

        if (error < 0f)
        {
            wcRL.motorTorque = 0f;
            wcRR.motorTorque = 0f;

            float brakeStrength = Mathf.InverseLerp(0f, lowSpeedThreshold, currentSpeed);
            float brake01 = Mathf.Clamp01(-error / Mathf.Max(targetSpeed, 0.5f));
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

    private void StopAndHold()
    {
        wcRL.motorTorque = 0f;
        wcRR.motorTorque = 0f;
        ApplyBrake(holdBrakeTorque);
        active = false;
        enabled = false;
    }

    private void StopAndDeactivate()
    {
        wcRL.motorTorque = 0f;
        wcRR.motorTorque = 0f;
        ApplyBrake(0f);
        active = false;
        enabled = false;
        gameObject.SetActive(false);
    }

    private void ApplyBrake(float torque)
    {
        wcFL.brakeTorque = torque;
        wcFR.brakeTorque = torque;
        wcRL.brakeTorque = torque;
        wcRR.brakeTorque = torque;
    }
}
