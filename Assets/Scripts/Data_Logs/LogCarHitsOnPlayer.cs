using System;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class LogCarHitsOnPlayer : MonoBehaviour
{
    [Header("Filter")]
    [SerializeField] private string carTag = "Car";

    [Header("Trial gating")]
    [SerializeField] private string trialSceneName = "Trial";

    [Header("CSV output")]
    public string folderName = "Logs";
    public string fileName = "Collisions.csv";

    private StreamWriter _writer;

    private void Start()
    {
        OpenOrAppendCsv();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (_writer == null) return;
        if (hit.collider == null) return;

        // Only log during trial
        if (!IsTrialLoaded()) return;

        // Log only Car
        if (!hit.collider.CompareTag(carTag) && !hit.collider.transform.root.CompareTag(carTag))
            return;

        WriteRow();
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
                _writer.WriteLine("Timestamp,Scene Name,Participant Number,Display Location,AV-Yielding");

            Debug.Log($"CollisionsLog: Appending to {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"CollisionsLog: Failed to open CSV: {e.Message}");
            _writer = null;
        }
    }

    private void WriteRow()
    {
        string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        int p = ExperimentConfig.Instance ? ExperimentConfig.Instance.ParticipantNumber : -1;
        DisplayLocation loc = ExperimentConfig.Instance ? ExperimentConfig.Instance.Location : DisplayLocation.None;
        bool av = ExperimentConfig.Instance && ExperimentConfig.Instance.AVYielding;

        _writer.WriteLine($"{ts},{trialSceneName},{p},{loc},{(av ? "true" : "false")}");
        Debug.Log("[CollisionsLog] Collision with Car logged.");
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
