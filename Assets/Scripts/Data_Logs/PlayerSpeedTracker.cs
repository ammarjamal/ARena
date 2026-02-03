using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PlayerSpeedTracker : MonoBehaviour
{
    public static PlayerSpeedTracker Instance { get; private set; }

    [Header("Motion source")]
    [SerializeField] private Transform playerRoot;

    [Header("Trial gating")]
    [SerializeField] private string trialSceneName = "Trial";

    [Header("Optional smoothing")]
    [Tooltip("0 = no smoothing. Higher = smoother but laggier.")]
    [Range(0f, 30f)] public float smoothing = 10f;

    public float SpeedMps { get; private set; }

    private Vector3 _prevPos;
    private bool _hasPrev;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!IsTrialLoaded())
        {
            ResetState();
            return;
        }

        if (!playerRoot)
        {
            SpeedMps = 0f;
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 pos = playerRoot.position;

        if (!_hasPrev)
        {
            _prevPos = pos;
            _hasPrev = true;
            SpeedMps = 0f;
            return;
        }

        float instant = Vector3.Distance(pos, _prevPos) / dt;
        _prevPos = pos;

        if (smoothing <= 0f)
        {
            SpeedMps = instant;
        }
        else
        {
            // exponential smoothing
            float a = 1f - Mathf.Exp(-smoothing * dt);
            SpeedMps = Mathf.Lerp(SpeedMps, instant, a);
        }
    }

    private void ResetState()
    {
        _hasPrev = false;
        SpeedMps = 0f;
    }

    private bool IsTrialLoaded()
    {
        var s = SceneManager.GetSceneByName(trialSceneName);
        return s.IsValid() && s.isLoaded;
    }
}
