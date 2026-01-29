using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string xrSceneName = "XRRig";   // persistent
    [SerializeField] private string uiSceneName = "UI";      // menu only
    [SerializeField] private string trialSceneName = "Trial";// trial only

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    private bool startupComplete;
    private bool busy;

    // prevent re-entrant / double loads
    private readonly HashSet<string> inFlight = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(Startup());
    }

    private IEnumerator Startup()
    {
        if (startupComplete) yield break;
        startupComplete = true;

        // Ensure XR is loaded and remains the active scene (stable EventSystem + XR input)
        yield return LoadAdditiveOnce(xrSceneName);

        var xr = SceneManager.GetSceneByName(xrSceneName);
        if (xr.IsValid() && xr.isLoaded)
            SceneManager.SetActiveScene(xr);

        // Start in menu
        yield return EnterMenu();
    }

    /// <summary>Call from UI button.</summary>
    public void StartTrial()
    {
        if (!startupComplete || busy) return;
        StartCoroutine(StartTrialRoutine());
    }

    /// <summary>Call from EndTrialTrigger.</summary>
    public void EndTrial()
    {
        if (!startupComplete || busy) return;
        StartCoroutine(EndTrialRoutine());
    }

    private IEnumerator StartTrialRoutine()
    {
        busy = true;

        // Ensure UI is gone
        yield return UnloadIfLoaded(uiSceneName);

        // Always reload Trial fresh
        yield return ReloadTrial();

        busy = false;
    }

    private IEnumerator EndTrialRoutine()
    {
        busy = true;

        yield return UnloadIfLoaded(trialSceneName);
        yield return EnterMenu();

        busy = false;
    }

    // -------------------------
    // Modes
    // -------------------------
    private IEnumerator EnterMenu()
    {
        // Ensure Trial is gone
        yield return UnloadIfLoaded(trialSceneName);

        // Load UI
        yield return LoadAdditiveOnce(uiSceneName);

        // Keep XR active (EventSystem lives there)
        var xr = SceneManager.GetSceneByName(xrSceneName);
        if (xr.IsValid() && xr.isLoaded)
            SceneManager.SetActiveScene(xr);

        if (verboseLogs) DumpLoadedScenes("ENTER MENU");
    }

    private IEnumerator ReloadTrial()
    {
        yield return UnloadIfLoaded(trialSceneName);
        yield return null; // allow cleanup

        yield return LoadAdditiveOnce(trialSceneName);

        // Keep XR active (EventSystem lives there)
        var xr = SceneManager.GetSceneByName(xrSceneName);
        if (xr.IsValid() && xr.isLoaded)
            SceneManager.SetActiveScene(xr);

        yield return null; // allow Awake/OnEnable in Trial to run

        if (verboseLogs) DumpLoadedScenes("TRIAL LOADED");
    }

    // -------------------------
    // Scene ops
    // -------------------------
    private IEnumerator LoadAdditiveOnce(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            yield break;

        if (IsLoaded(sceneName))
            yield break;

        if (!inFlight.Add(sceneName))
            yield break;

        if (verboseLogs) Debug.Log($"[SceneFlow] Load '{sceneName}' (Additive)");

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError($"[SceneFlow] LoadSceneAsync returned null for '{sceneName}'. Is it in Build Settings?");
            inFlight.Remove(sceneName);
            yield break;
        }

        while (!op.isDone) yield return null;

        inFlight.Remove(sceneName);
    }

    private IEnumerator UnloadIfLoaded(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            yield break;

        var s = SceneManager.GetSceneByName(sceneName);
        if (!s.IsValid() || !s.isLoaded)
            yield break;

        if (!inFlight.Add("UNLOAD:" + sceneName))
            yield break;

        if (verboseLogs) Debug.Log($"[SceneFlow] Unload '{sceneName}'");

        var op = SceneManager.UnloadSceneAsync(s);
        while (op != null && !op.isDone) yield return null;

        inFlight.Remove("UNLOAD:" + sceneName);
    }

    private static bool IsLoaded(string sceneName)
    {
        var s = SceneManager.GetSceneByName(sceneName);
        return s.IsValid() && s.isLoaded;
    }

    // -------------------------
    // Debug (optional)
    // -------------------------
    private void DumpLoadedScenes(string label)
    {
        Debug.Log($"--- [SceneFlow] {label} (sceneCount={SceneManager.sceneCount}) ---");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            Debug.Log($"[{i}] '{s.name}' loaded={s.isLoaded} path='{s.path}'");
        }
    }
}
