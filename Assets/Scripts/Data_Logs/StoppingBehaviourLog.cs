using System;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class StoppingBehaviourLog : MonoBehaviour
{
    [Header("Stop detection")]
    [Min(0f)] public float stopSpeedThresholdMps = 0.1f;
    [Min(0f)] public float minStoppedSeconds = 0.2f;

    [Header("Trial gating")]
    [SerializeField] private string trialSceneName = "Trial";

    [Header("CSV output")]
    public string folderName = "Logs";
    public string fileName = "Stopping Behaviour.csv";

    private StreamWriter _writer;

    private bool _isStopped;
    private float _stoppedAccum;

    private void Start()
    {
        OpenOrAppendCsv();
        ResetState();
    }

    private void Update()
    {
        if (_writer == null) return;

        if (!IsTrialLoaded())
        {
            ResetState();
            return;
        }

        float speed = (PlayerSpeedTracker.Instance != null) ? PlayerSpeedTracker.Instance.SpeedMps : 0f;

        bool under = speed < stopSpeedThresholdMps;

        if (under)
        {
            _stoppedAccum += Time.deltaTime;

            if (!_isStopped && _stoppedAccum >= minStoppedSeconds)
            {
                _isStopped = true;
                WriteRow(speed);
            }
        }
        else
        {
            _isStopped = false;
            _stoppedAccum = 0f;
        }
    }

    private bool IsTrialLoaded()
    {
        var s = SceneManager.GetSceneByName(trialSceneName);
        return s.IsValid() && s.isLoaded;
    }

    private void ResetState()
    {
        _isStopped = false;
        _stoppedAccum = 0f;
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
                _writer.WriteLine("Timestamp,Scene Name,Participant Number,Display Location,AV-Yielding,Speed (m/s)");

            Debug.Log($"StoppingBehaviourLog: Appending to {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"StoppingBehaviourLog: Failed to open CSV: {e.Message}");
            _writer = null;
        }
    }

    private void WriteRow(float speedMps)
    {
        string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        int p = ExperimentConfig.Instance ? ExperimentConfig.Instance.ParticipantNumber : -1;
        DisplayLocation loc = ExperimentConfig.Instance ? ExperimentConfig.Instance.Location : DisplayLocation.None;
        bool av = ExperimentConfig.Instance && ExperimentConfig.Instance.AVYielding;

        _writer.WriteLine($"{ts},{trialSceneName},{p},{loc},{(av ? "true" : "false")},{speedMps.ToString("F4", CultureInfo.InvariantCulture)}");
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
