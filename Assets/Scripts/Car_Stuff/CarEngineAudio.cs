using UnityEngine;

[DisallowMultipleComponent]
public class CarEngineAudio : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private AudioSource engineSource;

    [Header("Behaviour")]
    [Tooltip("If true, engine audio stops when the car is idle. If false, engine idles continuously.")]
    [SerializeField] private bool stopAudioWhenIdle = false;

    [Header("Idle thresholds (used only if stopAudioWhenIdle = true)")]
    [SerializeField] private float startSpeed = 0.3f; // m/s
    [SerializeField] private float stopSpeed  = 0.15f; // m/s

    [Header("Pitch / Volume")]
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.5f;

    [Tooltip("Idle volume (used when engine is playing at low speed).")]
    [SerializeField] private float minVolume = 0.4f;

    [SerializeField] private float maxVolume = 0.9f;

    [Header("Speed mapping")]
    [SerializeField] private float maxSpeedKmh = 45f;

    [Tooltip("How fast pitch/volume react (bigger = snappier).")]
    [SerializeField] private float response = 8f;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!engineSource) engineSource = GetComponent<AudioSource>();

        if (engineSource)
        {
            engineSource.playOnAwake = false;
            engineSource.loop = true;
        }
    }

    private void OnEnable()
    {
        // If idling is allowed, start immediately
        if (!stopAudioWhenIdle && engineSource && !engineSource.isPlaying)
            engineSource.Play();
    }

    private void Update()
    {
        if (!rb || !engineSource) return;

        float speedMs = rb.linearVelocity.magnitude;

        // ---------- Optional auto stop / start ----------
        if (stopAudioWhenIdle)
        {
            if (!engineSource.isPlaying)
            {
                if (speedMs >= startSpeed)
                    engineSource.Play();
                else
                    return;
            }
            else
            {
                if (speedMs <= stopSpeed)
                {
                    engineSource.Stop();
                    return;
                }
            }
        }
        else
        {
            // Always-on idle mode
            if (!engineSource.isPlaying)
                engineSource.Play();
        }

        // ---------- Pitch & volume mapping ----------
        float speedKmh = speedMs * 3.6f;
        float t = Mathf.Clamp01(speedKmh / Mathf.Max(1f, maxSpeedKmh));

        float targetPitch = Mathf.Lerp(minPitch, maxPitch, t);
        float targetVol   = Mathf.Lerp(minVolume, maxVolume, t);

        float k = 1f - Mathf.Exp(-response * Time.deltaTime);
        engineSource.pitch  = Mathf.Lerp(engineSource.pitch,  targetPitch, k);
        engineSource.volume = Mathf.Lerp(engineSource.volume, targetVol,   k);
    }
}
