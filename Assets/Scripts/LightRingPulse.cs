using UnityEngine;

[DisallowMultipleComponent]
public class LightRingPulse : MonoBehaviour
{
    [Header("Only animate when ExperimentConfig.Location matches this")]
    public DisplayLocation requiredLocation = DisplayLocation.EHMI;

    [Header("Start/Stop by distance between two tags")]
    [Tooltip("First tag to track (e.g., \"Player\")")]
    public string tagA = "Player";

    [Tooltip("Second tag to track (e.g., \"Car\")")]
    public string tagB = "Car";

    [Tooltip("Pulsing/flashing is active only when distance(A,B) <= this")]
    [Min(0f)] public float activateWithinMeters = 5f;

    [Tooltip("How often to re-find objects by tag (seconds).")]
    [Min(0.05f)] public float retargetIntervalSeconds = 0.5f;

    [Header("Renderer to animate")]
    public Renderer targetRenderer;

    [Header("Materials")]
    [Tooltip("Applied when cfg.Location != requiredLocation OR distance condition not met.")]
    public Material baseMaterial;

    [Tooltip("The OFF state for pulsing/flashing (the material we pulse FROM/TO).")]
    public Material offMaterial;

    [Tooltip("Material used when yielding (ON material for pulsing).")]
    public Material yieldingMaterial;

    [Tooltip("Material used when not yielding (ON material for flashing).")]
    public Material nonYieldingMaterial;

    [Header("Speed (per second)")]
    public float pulsesPerSecond = 1f;   // yielding: smooth fade in/out
    public float flashesPerSecond = 2f;  // not yielding: on/off

    [Header("Optional glow on top")]
    public bool alsoAnimateEmission = false;
    public float emissionIntensity = 2f;

    // URP Lit properties
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionId  = Shader.PropertyToID("_EmissionColor");

    private Material _runtimeMat;
    private Material _currentAssignedShared; // what we set into sharedMaterial last
    private Color _offBaseColor = Color.black;
    private Color _offEmissionColor = Color.black;
    private Color _onBaseColor = Color.white;
    private Color _onEmissionColor = Color.white;

    private Transform _a;
    private Transform _b;
    private float _nextRetargetTime;

    private void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>();
        Retarget();
    }

    private void Update()
    {
        var cfg = ExperimentConfig.Instance;
        if (cfg == null || targetRenderer == null) return;

        // Periodically re-find tagged objects (handles respawns / scene loads)
        if (Time.time >= _nextRetargetTime)
        {
            Retarget();
            _nextRetargetTime = Time.time + retargetIntervalSeconds;
        }

        bool locationOk = (cfg.Location == requiredLocation);
        bool distanceOk = AreTagsWithinDistance();

        // If location doesn't match OR distance condition not met: set BaseMaterial and stop.
        if (!locationOk || !distanceOk)
        {
            ApplySharedMaterial(baseMaterial);
            _runtimeMat = null; // rebuild cleanly when active again
            return;
        }

        // Active: decide ON material based on yielding
        bool yielding = cfg.AVYielding;
        Material onMat = yielding ? yieldingMaterial : nonYieldingMaterial;
        if (offMaterial == null || onMat == null) return;

        // Ensure we're working from the ON material (we animate its colors between OFF and ON)
        if (_currentAssignedShared != onMat || _runtimeMat == null)
        {
            ApplySharedMaterial(onMat);
            _runtimeMat = targetRenderer.material; // runtime instance we can edit safely

            // Cache ON colors from ON material
            _onBaseColor = _runtimeMat.HasProperty(BaseColorId) ? _runtimeMat.GetColor(BaseColorId) : Color.white;
            _onEmissionColor = _runtimeMat.HasProperty(EmissionId) ? _runtimeMat.GetColor(EmissionId) : _onBaseColor;

            // Cache OFF colors from OffMaterial (fallback to black)
            _offBaseColor = offMaterial.HasProperty(BaseColorId) ? offMaterial.GetColor(BaseColorId) : Color.black;
            _offEmissionColor = offMaterial.HasProperty(EmissionId) ? offMaterial.GetColor(EmissionId) : _offBaseColor;

            if (alsoAnimateEmission) _runtimeMat.EnableKeyword("_EMISSION");
        }

        // Strength 0..1
        float strength01;
        if (yielding)
        {
            float s = Mathf.Max(0.01f, pulsesPerSecond);
            strength01 = 0.5f + 0.5f * Mathf.Sin(Time.time * s * Mathf.PI * 2f); // smooth
        }
        else
        {
            float s = Mathf.Max(0.01f, flashesPerSecond);
            strength01 = (Mathf.Sin(Time.time * s * Mathf.PI * 2f) > 0f) ? 1f : 0f; // hard
        }

        // Animate base colour between OFF material colour and ON material colour
        if (_runtimeMat.HasProperty(BaseColorId))
            _runtimeMat.SetColor(BaseColorId, Color.Lerp(_offBaseColor, _onBaseColor, strength01));

        // Optional: animate emission too (also between OFF and ON)
        if (alsoAnimateEmission && _runtimeMat.HasProperty(EmissionId))
        {
            Color e = Color.Lerp(_offEmissionColor, _onEmissionColor, strength01) * emissionIntensity;
            _runtimeMat.SetColor(EmissionId, e);
        }
    }

    private void Retarget()
    {
        _a = FindFirstWithTag(tagA);
        _b = FindFirstWithTag(tagB);
    }

    private bool AreTagsWithinDistance()
    {
        if (_a == null || _b == null) return false;
        return Vector3.Distance(_a.position, _b.position) <= activateWithinMeters;
    }

    private static Transform FindFirstWithTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;
        try
        {
            var go = GameObject.FindGameObjectWithTag(tag);
            return go ? go.transform : null;
        }
        catch
        {
            // Tag doesn't exist in Tag Manager
            return null;
        }
    }

    private void ApplySharedMaterial(Material m)
    {
        if (m == null) return;
        if (_currentAssignedShared == m && targetRenderer.sharedMaterial == m) return;

        _currentAssignedShared = m;
        targetRenderer.sharedMaterial = m;
    }
}
