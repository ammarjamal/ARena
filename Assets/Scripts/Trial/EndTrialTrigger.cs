using UnityEngine;

[DisallowMultipleComponent]
public class EndTrialTrigger : MonoBehaviour
{
    [SerializeField] private string triggerTag = "RoadUser";

    private bool fired;
    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        // Quest reliability note:
        // If your player is a CharacterController (no Rigidbody),
        // put a kinematic Rigidbody on THIS trigger object.
    }

    private void OnEnable()
    {
        // New trial scene load => clean trigger every time
        fired = false;
        if (col) col.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (fired) return;
        if (!MatchesRoadUser(other)) return;

        fired = true;
        if (col) col.enabled = false;

        var flow = SceneFlowManager.Instance;
        if (!flow)
        {
            Debug.LogError("EndTrialTrigger: SceneFlowManager.Instance is NULL.");
            return;
        }

        flow.EndTrial();
    }

    private bool MatchesRoadUser(Collider other)
    {
        if (other.CompareTag(triggerTag)) return true;

        // Walk up the hierarchy in case the collider is on a child object
        for (Transform t = other.transform; t != null; t = t.parent)
        {
            if (t.CompareTag(triggerTag)) return true;
        }

        return false;
    }
}
