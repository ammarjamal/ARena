using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class ResetSceneTrigger : MonoBehaviour
{
    [Header("Trigger filter")]
    [SerializeField] private string triggerTag = "RoadUser";

    [Header("Reset behavior")]
    [Tooltip("If true, the trigger collider is disabled immediately after firing.")]
    [SerializeField] private bool disableTriggerOnFire = true;

    [Tooltip("Optional delay before reload (useful if you want a sound/flash).")]
    [SerializeField] private float reloadDelaySeconds = 0f;

    private bool fired;
    private Collider triggerCollider;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null) triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (fired) return;
        if (!other.CompareTag(triggerTag)) return;

        fired = true;

        if (disableTriggerOnFire && triggerCollider != null)
            triggerCollider.enabled = false;

        if (reloadDelaySeconds > 0f)
            Invoke(nameof(ReloadActiveScene), reloadDelaySeconds);
        else
            ReloadActiveScene();
    }

    private void ReloadActiveScene()
    {
        // Safety: if timeScale was changed elsewhere, restore it before reload.
        Time.timeScale = 1f;

        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
    }
}
