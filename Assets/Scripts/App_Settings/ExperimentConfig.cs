using UnityEngine;

public enum DisplayLocation
{
    None,
    EHMI,
    Crossing,
    Watch
}

public class ExperimentConfig : MonoBehaviour
{
    public static ExperimentConfig Instance { get; private set; }

    [SerializeField] private int participantNumber = 1;
    [SerializeField] private bool avYielding = true;
    [SerializeField] private DisplayLocation location = DisplayLocation.None;

    public int ParticipantNumber => participantNumber;
    public bool AVYielding => avYielding;
    public DisplayLocation Location => location;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Slider (Single = float)
    public void SetParticipantNumber(float value)
    {
        participantNumber = Mathf.Max(1, Mathf.RoundToInt(value));
    }

    // Toggle (Boolean = bool)
    public void SetAVYielding(bool value)
    {
        avYielding = value;
    }

    // Location toggles (Boolean = bool) — only set when turned ON
    public void SetLocationNone(bool isOn)    { if (isOn) location = DisplayLocation.None; }
    public void SetLocationEHMI(bool isOn)    { if (isOn) location = DisplayLocation.EHMI; }
    public void SetLocationCrossing(bool isOn){ if (isOn) location = DisplayLocation.Crossing; }
    public void SetLocationWatch(bool isOn)   { if (isOn) location = DisplayLocation.Watch; }
}
