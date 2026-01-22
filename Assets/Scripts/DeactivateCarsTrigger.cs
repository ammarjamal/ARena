using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DeactivateCarsTrigger : MonoBehaviour
{
    [Header("Trigger filter")]
    [SerializeField] private string triggerTag = "RoadUser";

    [Header("Objects to deactivate")]
    [Tooltip("Drag any car root GameObjects (or any objects) here.")]
    [SerializeField] private List<GameObject> objectsToDeactivate = new();

    private bool fired;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(triggerTag)) return;

        fired = true;

        for (int i = 0; i < objectsToDeactivate.Count; i++)
        {
            var go = objectsToDeactivate[i];
            if (go) go.SetActive(false);
        }
        gameObject.SetActive(false);
    }
}
