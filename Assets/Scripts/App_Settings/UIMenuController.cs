using UnityEngine;

public class UIMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;

    public void ShowMenu(bool show)
    {
        if (menuRoot) menuRoot.SetActive(show);
    }
}
