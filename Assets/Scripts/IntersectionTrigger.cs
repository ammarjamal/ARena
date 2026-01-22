using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class IntersectionTrigger : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private string roadUserTag = "Player";

    [Header("Cars to start")]
    [SerializeField] private List<DriveAtIntersectionController> carsToStart = new();


        [Header("UI")]
        [SerializeField] private TMP_Text debugText;

        

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        debugText.text += "ENTER: " + other.tag + "\n";
        if (!other.CompareTag(roadUserTag)) return;

        foreach (var car in carsToStart)
        {
            if (car) car.StartDriving();
        }

        gameObject.SetActive(false);
    }
}