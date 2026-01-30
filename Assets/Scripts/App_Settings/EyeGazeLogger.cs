using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EyeGazeLogger : MonoBehaviour
{
     [Header("Gaze source (assign these)")]
    public OVREyeGaze leftEyeGaze;
    public OVREyeGaze rightEyeGaze;

    [Header("Ray")]
    [Min(0.1f)] public float maxDistance = 20f;
    [Min(0f)] public float startOffsetMeters = 0.12f;
    [Min(0.0001f)] public float widthMeters = 0.01f;

    [Header("Hit detection")]
    public bool stopAtHit = false;
    public LayerMask hitMask = ~0; // what colliders the gaze ray can hit

    [Header("Enable")]
    public bool drawRay = true;

    private LineRenderer line;

    void Awake()
    {
        EnsureLine();
    }

    void Update()
    {
        if (!drawRay)
        {
            if (line) line.enabled = false;
            return;
        }

        if (!TryGetMidpointEyeRay(out Vector3 origin, out Vector3 dir))
        {
            if (line) line.enabled = false;
            return;
        }

        EnsureLine();
        line.enabled = true;

        Vector3 start = origin + dir * startOffsetMeters;
        Vector3 end = start + dir * maxDistance;

        bool hitSomething = false;

        // Raycast from the TRUE origin for correct hit testing
        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxDistance, hitMask, QueryTriggerInteraction.Collide))
        {
            hitSomething = true;

            if (stopAtHit)
                end = hit.point;
        }

        // Color: green when hit, red otherwise
        SetRayColor(hitSomething ? Color.green : Color.red);

        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    private void SetRayColor(Color c)
    {
        if (!line) return;

        line.startColor = c;
        line.endColor = c;

        if (line.material != null)
            line.material.color = c;
    }

    private void EnsureLine()
    {
        if (line) return;

        line = GetComponent<LineRenderer>();
        if (!line) line = gameObject.AddComponent<LineRenderer>();

        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = widthMeters;
        line.endWidth = widthMeters;

        // Material that renders on Quest
        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            line.material = new Material(shader);
            line.material.color = Color.red;
        }

        SetRayColor(Color.red);
        line.enabled = false;
    }

    private bool TryGetMidpointEyeRay(out Vector3 origin, out Vector3 dir)
    {
        origin = Vector3.zero; dir = Vector3.forward;

        bool leftOK = leftEyeGaze && leftEyeGaze.EyeTrackingEnabled;
        bool rightOK = rightEyeGaze && rightEyeGaze.EyeTrackingEnabled;

        if (leftOK && rightOK)
        {
            origin = (leftEyeGaze.transform.position + rightEyeGaze.transform.position) * 0.5f;
            dir = ((leftEyeGaze.transform.forward + rightEyeGaze.transform.forward) * 0.5f).normalized;
            return true;
        }
        if (leftOK)
        {
            origin = leftEyeGaze.transform.position;
            dir = leftEyeGaze.transform.forward;
            return true;
        }
        if (rightOK)
        {
            origin = rightEyeGaze.transform.position;
            dir = rightEyeGaze.transform.forward;
            return true;
        }

        return false;
    }
}
