using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppBootstrapper : MonoBehaviour
{
    [Header("Scene names (must match exactly)")]
    public string xrRigScene = "XRRig";
    public string uiScene = "UI";

    private static bool booted;

    private void Awake()
    {
        if (booted) { Destroy(gameObject); return; }
        booted = true;

        DontDestroyOnLoad(gameObject);
        StartCoroutine(BootRoutine());
    }

    private IEnumerator BootRoutine()
    {
        yield return LoadIfNotLoaded(xrRigScene);
        yield return LoadIfNotLoaded(uiScene);
    }

    private IEnumerator LoadIfNotLoaded(string sceneName)
    {
        if (IsLoaded(sceneName)) yield break;

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;
    }

    private bool IsLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == sceneName)
                return true;
        }
        return false;
    }
}
