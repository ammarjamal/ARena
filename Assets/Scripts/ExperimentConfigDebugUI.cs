using TMPro;
using UnityEngine;

public class ExperimentConfigDebugUI : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private float refreshRate = 0.1f; // update 10x/sec

    private float timer;

    private void Awake()
    {
        if (debugText == null)
            debugText = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer < refreshRate) return;
        timer = 0f;

        if (ExperimentConfig.Instance == null)
        {
            debugText.text = "ExperimentConfig: NOT FOUND";
            return;
        }

        var cfg = ExperimentConfig.Instance;

        debugText.text =
            $"<b>Experiment Config</b>\n" +
            $"Participant: {cfg.ParticipantNumber}\n" +
            $"AV Yielding: {cfg.AVYielding}\n" +
            $"Location: {cfg.Location}";
    }
}
