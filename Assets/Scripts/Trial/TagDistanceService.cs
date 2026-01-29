using UnityEngine;

[DisallowMultipleComponent]
public class TagDistanceService : MonoBehaviour
{
    public static TagDistanceService Instance { get; private set; }

    [Header("Tags to track")]
    public string tagA = "Player";
    public string tagB = "Car";

    [Header("Refresh / caching")]
    [Tooltip("How often to re-find objects by tag (seconds).")]
    [Min(0.05f)] public float retargetIntervalSeconds = 0.5f;

    [Tooltip("How often to recompute distance (seconds). Lower = more realtime, higher = cheaper.")]
    [Min(0.00f)] public float distanceUpdateIntervalSeconds = 0.0f; // 0 = every frame

    [Header("Debug (optional)")]
    public bool logIfMissing = false;

    public Transform A => _a;
    public Transform B => _b;

    /// <summary>Latest cached distance in meters. Use HasValidTargets to know if it's meaningful.</summary>
    public float DistanceMeters => _distanceMeters;

    /// <summary>True if both targets exist and distance is valid.</summary>
    public bool HasValidTargets => _hasValidTargets;

    private Transform _a;
    private Transform _b;

    private float _nextRetargetTime;
    private float _nextDistanceUpdateTime;

    private float _distanceMeters;
    private bool _hasValidTargets;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Retarget();
        RecomputeDistance();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        // Periodically re-find tagged objects (handles respawns / scene loads)
        if (Time.time >= _nextRetargetTime)
        {
            Retarget();
            _nextRetargetTime = Time.time + retargetIntervalSeconds;
        }

        // Recompute distance (every frame if interval == 0)
        if (distanceUpdateIntervalSeconds <= 0f || Time.time >= _nextDistanceUpdateTime)
        {
            RecomputeDistance();
            if (distanceUpdateIntervalSeconds > 0f)
                _nextDistanceUpdateTime = Time.time + distanceUpdateIntervalSeconds;
        }
    }

    /// <summary>
    /// Convenience check used by other scripts.
    /// Returns false if targets missing.
    /// </summary>
    public bool IsWithin(float meters)
    {
        if (!_hasValidTargets) return false;
        return _distanceMeters <= meters;
    }

    /// <summary>Force an immediate retarget + distance recompute (optional).</summary>
    public void ForceRefresh()
    {
        Retarget();
        RecomputeDistance();
    }

    private void Retarget()
    {
        _a = FindFirstWithTag(tagA);
        _b = FindFirstWithTag(tagB);

        _hasValidTargets = (_a != null && _b != null);

        if (logIfMissing && !_hasValidTargets)
            Debug.LogWarning($"[TagDistanceService] Missing targets. A({tagA})={( _a ? "OK" : "NULL" )}, B({tagB})={( _b ? "OK" : "NULL" )}");
    }

    private void RecomputeDistance()
    {
        if (!_hasValidTargets)
        {
            _distanceMeters = float.PositiveInfinity;
            return;
        }

        _distanceMeters = Vector3.Distance(_a.position, _b.position);
    }

    private static Transform FindFirstWithTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;
        try
        {
            var go = GameObject.FindGameObjectWithTag(tag);
            return go ? go.transform : null;
        }
        catch
        {
            // Tag doesn't exist in Tag Manager
            return null;
        }
    }
}
