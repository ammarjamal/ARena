using System;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;

[DisallowMultipleComponent]
public class CollisionLog : MonoBehaviour
{
    [Header("Filter")]
    [SerializeField] private string roadUserTag = "RoadUser";

    [Header("File")]
    [SerializeField] private string fileName = "Collisions.csv";

    private StreamWriter _w;
    private string _path;

    private void Awake()
    {
        string folder = Path.Combine(Application.persistentDataPath, "Logs");
        Directory.CreateDirectory(folder);

        _path = Path.Combine(folder, fileName);
        bool exists = File.Exists(_path);

        _w = new StreamWriter(_path, append: true, new UTF8Encoding(false))
        {
            AutoFlush = true
        };

        if (!exists)
            _w.WriteLine("Timestamp,Scene Name,Participant Number,Display Location,AV-Yielding");
    }

    private void OnCollisionEnter(Collision c)
    {
        if (_w == null || c == null || c.collider == null) return;

        // Only log if we hit RoadUser (collider OR its root)
        Transform t = c.collider.transform;
        if (!(t.CompareTag(roadUserTag) || t.root.CompareTag(roadUserTag)))
            return;

        // Timestamp
        string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        // ExperimentConfig values (safe fallback if missing)
        int p = -1;
        DisplayLocation loc = DisplayLocation.None;
        bool avYielding = false;

        if (ExperimentConfig.Instance != null)
        {
            p = ExperimentConfig.Instance.ParticipantNumber;
            loc = ExperimentConfig.Instance.Location;
            avYielding = ExperimentConfig.Instance.AVYielding;
        }

        _w.WriteLine($"{ts},{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name},{p},{loc},{(avYielding ? "true" : "false")}");
        Debug.Log("[Collisions] Car hit RoadUser -> logged");
    }

    private void OnDestroy() => CloseWriter();
    private void OnApplicationQuit() => CloseWriter();

    private void CloseWriter()
    {
        try { _w?.Flush(); _w?.Close(); } catch { }
        _w = null;
    }
}
