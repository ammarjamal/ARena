using UnityEngine;

public class StartTrialButton : MonoBehaviour
{
    public void StartTrial()
    {
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.StartTrial();
        else
            Debug.LogError("SceneFlowManager.Instance is null (Bootstrap not loaded / SceneFlowManager missing).");
    }
}

