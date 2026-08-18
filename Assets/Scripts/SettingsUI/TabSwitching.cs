using UnityEngine;

public class TabSwitching : MonoBehaviour
{
    [SerializeField] private GameObject CompanionMenu;
    [SerializeField] private GameObject GeneralMenu; //Always Default

    void OnEnable()
    {
        OnGeneralMenu();
    }

    void OnDisable()
    {
        GeneralMenu.SetActive(false);
        CompanionMenu.SetActive(false);
    }

    public void OnGeneralMenu()
    {
        if (GeneralMenu.activeSelf)
        {
            return;
        }
        GeneralMenu.SetActive(true);
        CompanionMenu.SetActive(false);
    }

    public void OnCompanionMenu()
    {
        if (CompanionMenu.activeSelf)
        {
            return;
        }
        GeneralMenu.SetActive(false);
        CompanionMenu.SetActive(true);
    }
}
