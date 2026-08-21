using UnityEngine;

public class TabSwitching : MonoBehaviour
{
    public enum SettingsState
    {
        General,
        Companion,
        RadioPlaylist,
    }

    [System.Serializable]
    private struct StatePanel
    {
        public SettingsState state;
        public GameObject panel;
    }

    [SerializeField] private StatePanel[] panels;

    [SerializeField] public SettingsState currentState = SettingsState.General;

    void OnEnable()
    {
        SwitchTo(SettingsState.General);
    }

    void OnDisable()
    {
        foreach (var p in panels)
        {
            if (p.panel != null) p.panel.SetActive(false);
        }
    }

    public void SwitchTo(SettingsState newState)
    {
        if (currentState == newState && IsCurrentPanelActive())
        {
            return; // already showing this state, nothing to do
        }

        currentState = newState;

        foreach (var p in panels)
        {
            if (p.panel != null) p.panel.SetActive(p.state == newState);
        }
    }

    bool IsCurrentPanelActive()
    {
        foreach (var p in panels)
        {
            if (p.state == currentState)
                return p.panel != null && p.panel.activeSelf;
        }
        return false;
    }

    public void OnGeneralMenu() => SwitchTo(SettingsState.General);
    public void OnCompanionMenu() => SwitchTo(SettingsState.Companion);
    public void OnRadioPlaylistMenu() => SwitchTo(SettingsState.RadioPlaylist);
}