using UnityEngine;

public class WheelVisualSync : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider wcFL;
    public WheelCollider wcFR;
    public WheelCollider wcRL;
    public WheelCollider wcRR;

    [Header("Wheel Meshes")]
    public Transform meshFL;
    public Transform meshFR;
    public Transform meshRL;
    public Transform meshRR;

    void Update()
    {
        Sync(wcFL, meshFL);
        Sync(wcFR, meshFR);
        Sync(wcRL, meshRL);
        Sync(wcRR, meshRR);
    }

    void Sync(WheelCollider wc, Transform mesh)
    {
        wc.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }
}
