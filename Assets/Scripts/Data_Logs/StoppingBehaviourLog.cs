using System;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class StoppingBehaviourLog : MonoBehaviour
{
    [Header("Head source")]
    [Tooltip("Assign the player camera / CenterEyeAnchor.")]
    public Transform headTransform;

    [Header("Stop detection")]
    [Min(0f)] public float stopEnterThresholdMps = 0.10f;
    [Min(0f)] public float stopExitThresholdMps = 0.15f;
    [Min(0f)] public float minStoppedSeconds = 0.20f;

    [Header("Trial gating")]
    [SerializeField] private string trialSceneName = "Trial";

    [Header("CSV output")]
    public string folderName = "Logs";
    public string fileName = "StoppingBehaviour.csv";

    private StreamWriter _writer;

    private bool _inStoppedState = false;
    private float _belowEnterAccum = 0f;

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

        var speedTracker = PlayerSpeedTracker.Instance;
        if (speedTracker == null) return;

        float speed = speedTracker.SpeedMps;

        if (!_inStoppedState)
        {
            if (speed < stopEnterThresholdMps)
            {
                _belowEnterAccum += Time.deltaTime;

                if (_belowEnterAccum >= minStoppedSeconds)
                {
                    _inStoppedState = true;

                    float z = headTransform ? headTransform.position.z : float.NaN;
                    WriteRow(speed, z);
                }
            }
            else
            {
                _belowEnterAccum = 0f;
            }
        }
        else
        {
            if (speed > stopExitThresholdMps)
            {
                _inStoppedState = false;
                _belowEnterAccum = 0f;
            }
        }
    }

    private bool IsTrialLoaded()
    {
        var s = SceneManager.GetSceneByName(trialSceneName);
        return s.IsValid() && s.isLoaded;
    }

    private void ResetState()
    {
        _inStoppedState = false;
        _belowEnterAccum = 0f;
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
                _writer.WriteLine("Timestamp,Scene Name,Participant Number,Display Location,AV-Yielding,Speed (m/s),HeadZ");

            Debug.Log($"StoppingBehaviourLog: Appending to {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"StoppingBehaviourLog: Failed to open CSV: {e.Message}");
            _writer = null;
        }
    }

    private void WriteRow(float speedMps, float headZ)
    {
        string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        int p = ExperimentConfig.Instance ? ExperimentConfig.Instance.ParticipantNumber : -1;
        DisplayLocation loc = ExperimentConfig.Instance ? ExperimentConfig.Instance.Location : DisplayLocation.None;
        bool av = ExperimentConfig.Instance && ExperimentConfig.Instance.AVYielding;

        _writer.WriteLine(
            $"{ts},{trialSceneName},{p},{loc},{(av ? "true" : "false")}," +
            $"{speedMps.ToString("F4", CultureInfo.InvariantCulture)}," +
            $"{headZ.ToString("F4", CultureInfo.InvariantCulture)}"
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
