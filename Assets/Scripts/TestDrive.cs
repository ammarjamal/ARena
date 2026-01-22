using UnityEngine;

public class TestDrive : MonoBehaviour
{
    public WheelCollider wcRL;
    public WheelCollider wcRR;

    public float motorTorque = 800f;

    void FixedUpdate()
    {
        wcRL.motorTorque = motorTorque;
        wcRR.motorTorque = motorTorque;
    }
}
