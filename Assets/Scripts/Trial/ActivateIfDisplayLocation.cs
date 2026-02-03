using UnityEngine;

public class ActivateIfDisplayLocation : MonoBehaviour
{
    [Header("What to toggle (leave empty to toggle this GameObject)")]
    [SerializeField] private GameObject target;

    [Header("Required ExperimentConfig Location")]
    [SerializeField] private DisplayLocation requiredLocation = DisplayLocation.Watch;

    [Header("Behaviour")]
    [SerializeField] private bool deactivateIfNotMatch = true;

    private void Awake()
    {
        if (!target) target = gameObject;

        if (ExperimentConfig.Instance == null)
        {
            Debug.LogError("ActivateIfLocation: ExperimentConfig.Instance is NULL");
            if (deactivateIfNotMatch) target.SetActive(false);
            return;
        }

        bool match = ExperimentConfig.Instance.Location == requiredLocation;

        if (match)
            target.SetActive(true);
        else if (deactivateIfNotMatch)
            target.SetActive(false);
    }
}
