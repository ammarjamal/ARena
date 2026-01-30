using UnityEngine;

[DisallowMultipleComponent]
public class GazeTargetId : MonoBehaviour
{
    [Tooltip("Unique ID for this gaze target (use a stable name for logging).")]
    [SerializeField] private string targetId = "Target";

    public string TargetId => targetId;
}
