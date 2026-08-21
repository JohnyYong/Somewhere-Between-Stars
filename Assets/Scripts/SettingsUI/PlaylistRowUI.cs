using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlaylistRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text trackNameText;
    [SerializeField] private Image background;
    [SerializeField] private Button button;

    [SerializeField] private Color normalColor = new Color(1, 1, 1, 0.05f);
    [SerializeField] private Color playingColor = new Color(1, 1, 1, 0.25f);

    private int _index;
    private RadioPlaylistUI _owner;

    public string TrackName { get; private set; }

    public void Setup(int index, string trackName, RadioPlaylistUI owner)
    {
        _index = index;
        _owner = owner;
        TrackName = trackName;

        trackNameText.text = trackName;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => _owner.SelectTrack(_index));

        SetHighlighted(false);
    }

    public void SetHighlighted(bool isPlaying)
    {
        if (background != null)
            background.color = isPlaying ? playingColor : normalColor;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}