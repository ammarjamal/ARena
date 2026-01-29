using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class TMPLogConsole : MonoBehaviour
{
    [Header("Find TMP by tag")]
    [SerializeField] private string logTag = "Log";

    [Header("What to show")]
    [SerializeField] private bool includeWarnings = true;
    [SerializeField] private bool includeLogs = false; // usually false

    private TMP_Text tmp;

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void Start()
    {
        FindTMP();
        SetText("Log ready.");
    }

    private void FindTMP()
    {
        var go = GameObject.FindWithTag(logTag);
        if (go != null) tmp = go.GetComponent<TMP_Text>();

        if (tmp == null)
            Debug.LogError($"TMPLogConsole: Could not find TMP_Text with tag '{logTag}'.");
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (tmp == null)
        {
            FindTMP();
            if (tmp == null) return;
        }

        // Filter
        if (type == LogType.Log && !includeLogs) return;
        if (type == LogType.Warning && !includeWarnings) return;
        if (type == LogType.Log) return;

        string prefix = type switch
        {
            LogType.Error => "[ERROR] ",
            LogType.Assert => "[ASSERT] ",
            LogType.Exception => "[EXCEPTION] ",
            LogType.Warning => "[WARN] ",
            _ => "[LOG] "
        };

        // Extract "File.cs:line" from stackTrace if available
        string fileLine = ExtractFileLine(stackTrace); // e.g. "SomeScript.cs:42"

        // Extract a short "source name"
        // - For exceptions: often shows "ScriptName.Method" in stack trace
        // - For errors: might be empty, so fallback to "Unity"
        string sourceName = ExtractSourceName(stackTrace);
        if (string.IsNullOrEmpty(sourceName)) sourceName = "Unity";

        // Format: Name: [ERROR] message (File.cs:line)
        if (!string.IsNullOrEmpty(fileLine))
            SetText($"{sourceName}: {prefix}{condition}\n({fileLine})");
        else
            SetText($"{sourceName}: {prefix}{condition}");
    }

    private void SetText(string msg)
    {
        tmp.text = msg;
    }

    private string ExtractFileLine(string stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace)) return "";

        // Matches: (at Assets/Scripts/MyFile.cs:123)
        var m = Regex.Match(stackTrace, @"\(at .*\/([^\/]+\.cs):(\d+)\)");
        if (m.Success)
            return $"{m.Groups[1].Value}:{m.Groups[2].Value}";

        return "";
    }

    private string ExtractSourceName(string stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace)) return "";

        // Matches: SomeClass.SomeMethod() (at ...)
        // We'll just take SomeClass
        var m = Regex.Match(stackTrace, @"^\s*([A-Za-z0-9_]+)\.");
        if (m.Success)
            return m.Groups[1].Value;

        return "";
    }
}
