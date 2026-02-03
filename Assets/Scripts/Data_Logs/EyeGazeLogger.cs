/*
EyeGazeCsvLogger.cs

Logs eye-gaze hits to ONE continuous CSV file (GazeLog.csv), ONLY while the Trial scene is loaded.

Columns:
Timestamp (yyyy-MM-dd HH:mm), Scene Name, Participant Number, Display Location, AV-Yielding,
Gaze Hit Location (GazeTargetId.TargetId OR collider name fallback), Dwell Time (seconds)

Key guarantees:
- Raycast ONLY against hitMask (set to Eye Tracking Target layer ONLY)
- Uses FIRST HIT ONLY
- "None" means: NO collider hit in that layer
- If hit occurs but no GazeTargetId exists, logs collider name (never silently becomes None)
- Single file name, append forever (no per-trial/per-participant files)
- Trial gating works with your additive scene flow (XRRig stays loaded, Trial loads/unloads)
*/

using System;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class EyeGazeLogger : MonoBehaviour
{
    [Header("Gaze source (assign these)")]
    public OVREyeGaze leftEyeGaze;
    public OVREyeGaze rightEyeGaze;

    [Header("Raycast (Eye layer ONLY)")]
    [Min(0.1f)] public float maxDistance = 35f;
    [Tooltip("Set this to ONLY the Eye Tracking Target layer.")]
    public LayerMask hitMask;
    [Tooltip("If your target colliders are triggers, keep this ON.")]
    public bool includeTriggers = true;

    [Header("Dwell logging")]
    [Tooltip("Logs a row when gaze target changes. Dwell time = time spent on previous target.")]
    public bool segmentByHitChange = true;
    [Min(0f)] public float minDwellSecondsToLog = 0.02f;

    [Header("Trial gating")]
    [SerializeField] private string trialSceneName = "Trial";

    [Header("CSV output")]
    public string folderName = "Logs";
    public string fileName = "GazeLog.csv";

    private StreamWriter writer;
    private bool trialLoaded;

    // dwell state
    private bool hasCurrent;
    private string currentHitLabel = "";
    private float currentDwellSeconds = 0f;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void Start()
    {
        OpenOrAppendCsv();

        trialLoaded = IsSceneLoaded(trialSceneName);

        // reset state at start
        ResetDwellState();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != trialSceneName) return;
        trialLoaded = true;
        ResetDwellState();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name != trialSceneName) return;

        // flush open segment (but only if it wasn't None)
        FlushOpenSegment();

        trialLoaded = false;
        ResetDwellState();
    }

    private void Update()
    {
        if (!trialLoaded || writer == null) return;

        if (!TryGetMidpointEyeRay(out Vector3 origin, out Vector3 dir))
            return;

        QueryTriggerInteraction qti = includeTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        bool hit = Physics.Raycast(origin, dir, out RaycastHit rh, maxDistance, hitMask, qti);

        // If no hit in Eye layer => label is None => DO NOT LOG
        string labelThisFrame = hit ? ResolveHitLabel(rh) : "None";

        float dt = Time.deltaTime;

        // -------- PER-FRAME MODE --------
        if (!segmentByHitChange)
        {
            if (labelThisFrame != "None")
                WriteRow(labelThisFrame, dt);
            return;
        }

        // -------- SEGMENTED DWELL MODE --------
        if (!hasCurrent)
        {
            // Start tracking only if it's a real hit (not None)
            if (labelThisFrame == "None")
                return;

            hasCurrent = true;
            currentHitLabel = labelThisFrame;
            currentDwellSeconds = 0f;
            return;
        }

        // If current segment exists but we now see None, we END the segment and log it
        if (labelThisFrame == "None")
        {
            if (currentDwellSeconds >= minDwellSecondsToLog)
                WriteRow(currentHitLabel, currentDwellSeconds);

            ResetDwellState();
            return;
        }

        // Normal segment accumulation
        if (labelThisFrame == currentHitLabel)
        {
            currentDwellSeconds += dt;
        }
        else
        {
            // log previous
            if (currentDwellSeconds >= minDwellSecondsToLog)
                WriteRow(currentHitLabel, currentDwellSeconds);

            // start new
            currentHitLabel = labelThisFrame;
            currentDwellSeconds = dt;
        }
    }

    /// <summary>
    /// Returns a valid label for an Eye-layer hit.
    /// If no GazeTargetId exists, falls back to collider name (still not "None").
    /// </summary>
    private string ResolveHitLabel(RaycastHit hit)
    {
        if (hit.collider == null) return "HIT_UNKNOWN";

        // collider GO
        var id = hit.collider.GetComponent<GazeTargetId>();
        if (id != null && !string.IsNullOrWhiteSpace(id.TargetId))
            return id.TargetId.Trim();

        // parent chain
        id = hit.collider.GetComponentInParent<GazeTargetId>();
        if (id != null && !string.IsNullOrWhiteSpace(id.TargetId))
            return id.TargetId.Trim();

        // child chain
        id = hit.collider.GetComponentInChildren<GazeTargetId>();
        if (id != null && !string.IsNullOrWhiteSpace(id.TargetId))
            return id.TargetId.Trim();

        // Still a valid hit in Eye layer, so never return None
        return hit.collider.name;
    }

    private bool TryGetMidpointEyeRay(out Vector3 origin, out Vector3 dir)
    {
        origin = Vector3.zero;
        dir = Vector3.forward;

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

    private void ResetDwellState()
    {
        hasCurrent = false;
        currentHitLabel = "";
        currentDwellSeconds = 0f;
    }

    private void FlushOpenSegment()
    {
        if (!segmentByHitChange) return;
        if (!hasCurrent) return;
        if (writer == null) return;

        if (currentDwellSeconds >= minDwellSecondsToLog)
            WriteRow(currentHitLabel, currentDwellSeconds);

        ResetDwellState();
    }

    private void OpenOrAppendCsv()
    {
        try
        {
            string folderPath = Path.Combine(Application.persistentDataPath, folderName);
            Directory.CreateDirectory(folderPath);

            string path = Path.Combine(folderPath, fileName);
            bool exists = File.Exists(path);

            writer = new StreamWriter(path, true, new UTF8Encoding(false)); // append forever
            writer.AutoFlush = true;

            if (!exists)
            {
                writer.WriteLine("Timestamp,Scene Name,Participant Number,Display Location,AV-Yielding,Gaze Hit Location,Dwell Time");
            }

            Debug.Log($"EyeGazeCsvLogger: Appending to {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"EyeGazeCsvLogger: Failed to open CSV: {e.Message}");
            writer = null;
        }
    }

    private void WriteRow(string gazeHitLocation, float dwellSeconds)
    {
        if (writer == null) return;

        string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        int participantNumber = ExperimentConfig.Instance ? ExperimentConfig.Instance.ParticipantNumber : -1;
        DisplayLocation displayLocation = ExperimentConfig.Instance ? ExperimentConfig.Instance.Location : DisplayLocation.None;
        bool avYielding = ExperimentConfig.Instance && ExperimentConfig.Instance.AVYielding;

        string dwell = dwellSeconds.ToString("F4", CultureInfo.InvariantCulture);

        writer.WriteLine(
            $"{ts},{EscapeCsv(trialSceneName)},{participantNumber},{displayLocation},{(avYielding ? "true" : "false")},{EscapeCsv(gazeHitLocation)},{dwell}"
        );
    }

    private static string EscapeCsv(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        bool needsQuotes = s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r");
        if (!needsQuotes) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    private static bool IsSceneLoaded(string sceneName)
    {
        var s = SceneManager.GetSceneByName(sceneName);
        return s.IsValid() && s.isLoaded;
    }

    private void OnApplicationQuit() => Close();
    private void OnDestroy() => Close();

    private void Close()
    {
        if (writer == null) return;

        FlushOpenSegment();

        try { writer.Flush(); writer.Close(); } catch { }
        writer = null;
    }
}
