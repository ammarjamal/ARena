using UnityEngine;
using TMPro;

public class SliderValueText : MonoBehaviour
{
    public UnityEngine.UI.Slider slider;
    public TMP_Text valueText;

    void Start()
    {
        UpdateText(slider.value);
        slider.onValueChanged.AddListener(UpdateText);
    }

    void UpdateText(float v)
    {
        valueText.text = v.ToString("0");
    }
}
