using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExperimentConfigUIBinder : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider participantSlider;
    [SerializeField] private TMP_Text participantLabel;
    [SerializeField] private Toggle avYieldingToggle;

    [Header("Location (choose ONE approach)")]
    [SerializeField] private Toggle locNone;
    [SerializeField] private Toggle locEHMI;
    [SerializeField] private Toggle locCrossing;
    [SerializeField] private Toggle locWatch;

    private void Start()
    {
        // Wait until ExperimentConfig exists (Bootstrap should have loaded it)
        if (ExperimentConfig.Instance == null)
        {
            Debug.LogError("ExperimentConfig.Instance is null. Make sure ExperimentConfig is in the Bootstrap scene and Bootstrap loads first.");
            enabled = false;
            return;
        }

        // Sync Config -> UI once
        participantSlider.SetValueWithoutNotify(ExperimentConfig.Instance.ParticipantNumber);
        avYieldingToggle.SetIsOnWithoutNotify(ExperimentConfig.Instance.AVYielding);
        UpdateParticipantLabel(ExperimentConfig.Instance.ParticipantNumber);

        // Set location toggles based on config
        SetLocationToggles(ExperimentConfig.Instance.Location);

        // Hook UI -> Config
        participantSlider.onValueChanged.AddListener(ExperimentConfig.Instance.SetParticipantNumber);
        participantSlider.onValueChanged.AddListener(v => UpdateParticipantLabel(Mathf.RoundToInt(v)));

        avYieldingToggle.onValueChanged.AddListener(ExperimentConfig.Instance.SetAVYielding);

        locNone.onValueChanged.AddListener(ExperimentConfig.Instance.SetLocationNone);
        locEHMI.onValueChanged.AddListener(ExperimentConfig.Instance.SetLocationEHMI);
        locCrossing.onValueChanged.AddListener(ExperimentConfig.Instance.SetLocationCrossing);
        locWatch.onValueChanged.AddListener(ExperimentConfig.Instance.SetLocationWatch);
    }

    private void OnDestroy()
    {
        // Prevent duplicate listeners if UI scene reloads
        if (ExperimentConfig.Instance == null) return;

        if (participantSlider)
        {
            participantSlider.onValueChanged.RemoveListener(ExperimentConfig.Instance.SetParticipantNumber);
        }

        if (avYieldingToggle)
        {
            avYieldingToggle.onValueChanged.RemoveListener(ExperimentConfig.Instance.SetAVYielding);
        }

        if (locNone) locNone.onValueChanged.RemoveListener(ExperimentConfig.Instance.SetLocationNone);
        if (locEHMI) locEHMI.onValueChanged.RemoveListener(ExperimentConfig.Instance.SetLocationEHMI);
        if (locCrossing) locCrossing.onValueChanged.RemoveListener(ExperimentConfig.Instance.SetLocationCrossing);
        if (locWatch) locWatch.onValueChanged.RemoveListener(ExperimentConfig.Instance.SetLocationWatch);
    }

    private void UpdateParticipantLabel(int v)
    {
        if (participantLabel) participantLabel.text = $"P{Mathf.Max(1, v)}";
    }

    private void SetLocationToggles(DisplayLocation loc)
    {
        if (locNone) locNone.SetIsOnWithoutNotify(loc == DisplayLocation.None);
        if (locEHMI) locEHMI.SetIsOnWithoutNotify(loc == DisplayLocation.EHMI);
        if (locCrossing) locCrossing.SetIsOnWithoutNotify(loc == DisplayLocation.Crossing);
        if (locWatch) locWatch.SetIsOnWithoutNotify(loc == DisplayLocation.Watch);
    }
}
