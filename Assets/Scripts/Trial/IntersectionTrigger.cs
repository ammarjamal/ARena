using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class IntersectionTrigger : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private string roadUserTag = "RoadUser";

    [Header("Cars to start")]
    [SerializeField] private List<DriveAtIntersectionController> carsToStart = new();

    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        // Strongly recommended for Quest reliability:
        // Add a Rigidbody on this trigger object and set isKinematic=true.
    }

    public void ResetTrigger()
    {
        if (col) col.enabled = true;
        gameObject.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!MatchesRoadUser(other)) return;

        Debug.Log($"IntersectionTrigger: ENTER by {other.name}");

        foreach (var car in carsToStart)
            if (car) car.StartDriving();

        gameObject.SetActive(false);
    }

    private bool MatchesRoadUser(Collider other)
    {
        if (other.CompareTag(roadUserTag)) return true;

        var t = other.transform;
        while (t != null)
        {
            if (t.CompareTag(roadUserTag)) return true;
            t = t.parent;
        }
        return false;
    }
}
