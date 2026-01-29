using UnityEngine;

public class WatchController : MonoBehaviour
{
    [SerializeField] private GameObject watchObject;

    private Transform leftAnchor;

    private void Start()
    {
        if (!watchObject)
        {
            Debug.LogError("WatchController: watchObject not assigned.");
            return;
        }

        var rig = FindObjectOfType<OVRCameraRig>(true);
        if (!rig)
        {
            Debug.LogError("WatchController: OVRCameraRig not found.");
            return;
        }

        leftAnchor = rig.leftControllerAnchor;
        if (!leftAnchor)
        {
            Debug.LogError("WatchController: rig.leftControllerAnchor is NULL.");
            return;
        }

        ApplyActiveState();
    }

    private void LateUpdate()
    {
        ApplyActiveState();

        if (!watchObject || !watchObject.activeSelf) return;
        if (!leftAnchor) return;

        // Follow controller pose (position + rotation)
        watchObject.transform.position = leftAnchor.position;
        watchObject.transform.rotation = leftAnchor.rotation;

        // DO NOT touch scale
    }

    private void ApplyActiveState()
    {
        bool showWatch =
            ExperimentConfig.Instance != null &&
            ExperimentConfig.Instance.Location == DisplayLocation.Watch;

        if (watchObject && watchObject.activeSelf != showWatch)
            watchObject.SetActive(showWatch);
    }
}
