using System;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class BehaviourLog : MonoBehaviour
{
    [Header("Head source")]
    [Tooltip("Assign the head/camera transform (CenterEyeAnchor / Camera).")]
    [SerializeField] private Transform headTransform;

    [Header("Logging rate")]
    [Min(0.01f)] public float logIntervalSeconds = 0.1f;

    [Header("Trial gating")]
    [SerializeField] private string trialSceneName = "Trial";

    [Header("CSV output")]
    public string folderName = "Logs";
    public string fileName = "BehaviourLog.csv";

    private StreamWriter _writer;
    private float _timer;

    private void Start()
    {
        OpenOrAppendCsv();
        _timer = 0f;
    }

    private void Update()
    {
        if (_writer == null) return;

        if (!IsTrialLoaded())
        {
            _timer = 0f;
            return;
        }

        if (!headTransform) return;

        _timer += Time.deltaTime;
        if (_timer < logIntervalSeconds) return;
        _timer = 0f;

        float speed = (PlayerSpeedTracker.Instance != null) ? PlayerSpeedTracker.Instance.SpeedMps : 0f;

        // Absolute yaw angle with 1 decimal (e.g., -95.2534 -> 95.3)
        float yaw = headTransform.eulerAngles.y;
        float signedYaw = Mathf.DeltaAngle(0f, yaw);
        float absYaw = Mathf.Abs(signedYaw);
        float absYawRounded = (float)Math.Round(absYaw, 1, MidpointRounding.AwayFromZero);

        WriteRow(speed, absYawRounded);
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
                _writer.WriteLine("Timestamp,Scene Name,Participant Number,Display Location,AV-Yielding,Speed (m/s),Abs Head Yaw (deg)");

            Debug.Log($"BehaviourLog: Appending to {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"BehaviourLog: Failed to open CSV: {e.Message}");
            _writer = null;
        }
    }

    private void WriteRow(float speedMps, float absYawDeg)
    {
        string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        int p = ExperimentConfig.Instance ? ExperimentConfig.Instance.ParticipantNumber : -1;
        DisplayLocation loc = ExperimentConfig.Instance ? ExperimentConfig.Instance.Location : DisplayLocation.None;
        bool av = ExperimentConfig.Instance && ExperimentConfig.Instance.AVYielding;

        _writer.WriteLine(
            $"{ts},{trialSceneName},{p},{loc},{(av ? "true" : "false")}," +
            $"{speedMps.ToString("F4", CultureInfo.InvariantCulture)}," +
            $"{absYawDeg.ToString("F1", CultureInfo.InvariantCulture)}"
        );
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
