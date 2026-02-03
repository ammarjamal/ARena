using System;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class StepLog : MonoBehaviour
{
    [Header("Head source")]
    [Tooltip("Assign the head/camera transform (CenterEyeAnchor / Camera).")]
    [SerializeField] private Transform headTransform;

    [Header("Trial gating")]
    [SerializeField] private string trialSceneName = "Trial";

    [Header("CSV output")]
    public string folderName = "Logs";
    public string fileName = "StepLog.csv";

    [Header("Step detection (same as StepCounter)")]
    [Tooltip("Vertical acceleration peak threshold (m/s^2). Lower = more sensitive.")]
    public float stepAccelThreshold = 1.2f;

    [Tooltip("Minimum seconds between steps (prevents double-counting).")]
    public float minStepIntervalSeconds = 0.24f;

    [Header("Smoothing")]
    [Range(1f, 60f)] public float velSmooth = 22f;
    [Range(1f, 60f)] public float accelSmooth = 22f;

    [Header("Step size (m/step)")]
    [Tooltip("Use PlayerSpeedTracker speed if available. Otherwise uses HMD-derived horizontal speed.")]
    public bool preferPlayerSpeedTracker = true;

    private StreamWriter _writer;

    // internal step/gait state (runs every frame)
    private bool _hadHeadInit;
    private Vector3 _prevHeadPos;

    private Vector3 _velSmoothed;
    private Vector3 _prevVelSmoothed;
    private Vector3 _accelSmoothed;

    private float _prevAy, _ay;
    private float _lastStepTime = -999f;

    private int _stepCount;
    private float _stepInterval;
    private float _cadenceSpm;
    private float _stepSizeM;

    private bool _wasInTrial;

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
        ResetStepState();
        _wasInTrial = IsTrialLoaded();
    }

    private void Update()
    {
        if (_writer == null) return;
        if (!headTransform) return;

        bool inTrial = IsTrialLoaded();

        // fallback safety: if we entered trial without scene callbacks firing
        if (inTrial && !_wasInTrial)
        {
            ResetStepState();
            InitHeadState();
        }

        _wasInTrial = inTrial;

        if (!inTrial) return;

        if (!_hadHeadInit)
            InitHeadState();

        UpdateStepCadenceAndSize();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != trialSceneName) return;

        // New trial starts here -> reset steps
        ResetStepState();
        InitHeadState();
        _wasInTrial = true;

        Debug.Log("StepLog: Trial scene loaded -> step count reset to 0");
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name != trialSceneName) return;

        // leaving trial -> clear state too
        ResetStepState();
        _wasInTrial = false;

        Debug.Log("StepLog: Trial scene unloaded -> step state cleared");
    }

    private void UpdateStepCadenceAndSize()
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-6f);

        // Position -> Velocity
        Vector3 headPos = headTransform.position;
        Vector3 velRaw = (headPos - _prevHeadPos) / dt;
        _prevHeadPos = headPos;

        float velAlpha = 1f - Mathf.Exp(-velSmooth * dt);
        _velSmoothed = Vector3.Lerp(_velSmoothed, velRaw, velAlpha);

        // Velocity -> Acceleration
        Vector3 accelRaw = (_velSmoothed - _prevVelSmoothed) / dt;
        _prevVelSmoothed = _velSmoothed;

        float accAlpha = 1f - Mathf.Exp(-accelSmooth * dt);
        _accelSmoothed = Vector3.Lerp(_accelSmoothed, accelRaw, accAlpha);

        // Step detection from vertical acceleration peak
        _prevAy = _ay;
        _ay = _accelSmoothed.y;

        bool refractoryOK = (Time.time - _lastStepTime) >= minStepIntervalSeconds;
        bool isPeak = (_prevAy > stepAccelThreshold) && (_ay < _prevAy); // local max above threshold

        if (!isPeak || !refractoryOK) return;

        // ---- Step event ----
        _stepCount++;

        float speedMps;
        if (preferPlayerSpeedTracker && PlayerSpeedTracker.Instance != null)
            speedMps = Mathf.Max(0f, PlayerSpeedTracker.Instance.SpeedMps);
        else
            speedMps = new Vector2(_velSmoothed.x, _velSmoothed.z).magnitude;

        // First step since reset: interval/cadence not meaningful
        if (_lastStepTime < 0f)
        {
            _stepInterval = 0f;
            _cadenceSpm = 0f;
            _stepSizeM = 0f;

            _lastStepTime = Time.time;

            WriteStepRow(_stepCount, _stepInterval, _cadenceSpm, _stepSizeM, speedMps);
            return;
        }

        // normal steps (2nd step onward)
        _stepInterval = Time.time - _lastStepTime;
        _lastStepTime = Time.time;

        if (_stepInterval > 0.10f && _stepInterval < 1.0f)
            _cadenceSpm = 60f / _stepInterval;

        _stepSizeM = (_cadenceSpm > 1f) ? speedMps / (_cadenceSpm / 60f) : 0f;

        WriteStepRow(_stepCount, _stepInterval, _cadenceSpm, _stepSizeM, speedMps);
    }

    private bool IsTrialLoaded()
    {
        var s = SceneManager.GetSceneByName(trialSceneName);
        return s.IsValid() && s.isLoaded;
    }

    private void OpenOrAppendCsv()
    {
        try
        {
            string folderPath = Path.Combine(Application.persistentDataPath, folderName);
            Directory.CreateDirectory(folderPath);

            string path = Path.Combine(folderPath, fileName);
            bool exists = File.Exists(path);

            _writer = new StreamWriter(path, true, new UTF8Encoding(false));
            _writer.AutoFlush = true;

            if (!exists)
            {
                _writer.WriteLine(
                    "Timestamp,Scene Name,Participant Number,Display Location,AV-Yielding," +
                    "Step Count,Step Interval (s),Cadence (spm),Step Size (m/step),Speed At Step (m/s)"
                );
            }

            Debug.Log($"StepLog: Appending to {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"StepLog: Failed to open CSV: {e.Message}");
            _writer = null;
        }
    }

    private void WriteStepRow(int stepCount, float stepInterval, float cadenceSpm, float stepSizeM, float speedAtStep)
    {
        // Include seconds + ms so rows don’t all share the same minute timestamp
        string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        int p = ExperimentConfig.Instance ? ExperimentConfig.Instance.ParticipantNumber : -1;
        DisplayLocation loc = ExperimentConfig.Instance ? ExperimentConfig.Instance.Location : DisplayLocation.None;
        bool av = ExperimentConfig.Instance && ExperimentConfig.Instance.AVYielding;

        _writer.WriteLine(
            $"{ts},{trialSceneName},{p},{loc},{(av ? "true" : "false")}," +
            $"{stepCount}," +
            $"{stepInterval.ToString("F4", CultureInfo.InvariantCulture)}," +
            $"{cadenceSpm.ToString("F2", CultureInfo.InvariantCulture)}," +
            $"{stepSizeM.ToString("F3", CultureInfo.InvariantCulture)}," +
            $"{speedAtStep.ToString("F4", CultureInfo.InvariantCulture)}"
        );
    }

    private void InitHeadState()
    {
        _prevHeadPos = headTransform.position;
        _prevVelSmoothed = Vector3.zero;

        // don’t force-reset _lastStepTime here; ResetStepState controls first-step behavior
        _hadHeadInit = true;
    }

    private void ResetStepState()
    {
        _hadHeadInit = false;

        _prevHeadPos = Vector3.zero;
        _velSmoothed = Vector3.zero;
        _prevVelSmoothed = Vector3.zero;
        _accelSmoothed = Vector3.zero;

        _prevAy = _ay = 0f;

        // IMPORTANT: negative sentinel so first step interval becomes 0 (handled in code)
        _lastStepTime = -999f;

        _stepCount = 0;
        _stepInterval = 0f;
        _cadenceSpm = 0f;
        _stepSizeM = 0f;
    }

    private void OnApplicationQuit() => Close();
    private void OnDestroy() => Close();

    private void Close()
    {
        if (_writer == null) return;
        try { _writer.Flush(); _writer.Close(); } catch { }
        _writer = null;
    }
}
