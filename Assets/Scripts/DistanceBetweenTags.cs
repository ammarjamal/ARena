using UnityEngine;
using TMPro;

public class DistanceBetweenTags : MonoBehaviour
{
    [Header("Tags to measure between")]
    public string tagA = "Player";
    public string tagB = "Car";

    [Header("TMP Output (3D TextMeshPro)")]
    public TextMeshPro outputText;

    [Header("Debug")]
    public bool logToTMP = true;

    void Update()
    {
        GameObject objA;
        GameObject objB;

        try
        {
            objA = GameObject.FindGameObjectWithTag(tagA);
            objB = GameObject.FindGameObjectWithTag(tagB);
        }
        catch
        {
            WriteTMP("Tag missing in Tag Manager");
            return;
        }

        if (objA == null || objB == null)
        {
            WriteTMP($"Missing: {(objA ? "" : tagA)} {(objB ? "" : tagB)}");
            return;
        }

        float distance = Vector3.Distance(objA.transform.position, objB.transform.position);

        if (logToTMP)
            WriteTMP($"{tagA}({objA.name}) ↔ {tagB}({objB.name}) = {distance:F2}m");
    }

    void WriteTMP(string msg)
    {
        if (!outputText) return;
        outputText.text = msg;
    }

    public float GetDistance()
    {
        try
        {
            var objA = GameObject.FindGameObjectWithTag(tagA);
            var objB = GameObject.FindGameObjectWithTag(tagB);
            if (objA == null || objB == null) return -1f;
            return Vector3.Distance(objA.transform.position, objB.transform.position);
        }
        catch
        {
            return -1f;
        }
    }
}
