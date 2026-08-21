using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SFB; // Standalone File Browser

public class RadioPlaylistUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RadioInteraction radioInteraction;
    [SerializeField] private CustomAudioLoader customMusicLoader;

    [Header("List UI")]
    [SerializeField] private Transform listContent;  
    [SerializeField] private PlaylistRowUI rowPrefab;
    [SerializeField] private Button addMusicButton;
    [SerializeField] private TMP_InputField searchInput;

    private readonly List<PlaylistRowUI> _rows = new List<PlaylistRowUI>();

    void OnEnable()
    {
        RebuildList();

        if (radioInteraction != null)
            radioInteraction.OnTrackChanged += HandleTrackChanged;
    }

    void OnDisable()
    {
        if (radioInteraction != null)
            radioInteraction.OnTrackChanged -= HandleTrackChanged;
    }

    void Start()
    {
        if (addMusicButton != null)
            addMusicButton.onClick.AddListener(OnAddMusicClicked);

        if (searchInput != null)
            searchInput.onValueChanged.AddListener(FilterList);
    }

    void OnAddMusicClicked()
    {
        var extensions = new[]
        {
            new ExtensionFilter("Audio Files", "mp3", "wav", "ogg")
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Music", "", extensions, true);

        foreach (string path in paths)
        {
            if (string.IsNullOrEmpty(path)) continue;
            StartCoroutine(customMusicLoader.AddTrackFromExternalPath(path, OnTrackAdded));
        }
    }

    void OnTrackAdded(AudioClip clip)
    {
        if (clip == null) return; 
        RebuildList();
    }

    public void RebuildList()
    {
        foreach (var row in _rows)
        {
            if (row != null) Destroy(row.gameObject);
        }
        _rows.Clear();

        if (radioInteraction == null) return;

        var names = radioInteraction.AllTrackNames;
        for (int i = 0; i < names.Count; i++)
        {
            var row = Instantiate(rowPrefab, listContent);
            row.Setup(i, names[i], this);
            row.SetHighlighted(i == radioInteraction.CurrentTrackIndex);
            _rows.Add(row);
        }

        if (searchInput != null && !string.IsNullOrEmpty(searchInput.text))
            FilterList(searchInput.text);
    }

    public void SelectTrack(int index)
    {
        radioInteraction.PlayTrack(index);
    }

    void HandleTrackChanged(int newIndex)
    {
        for (int i = 0; i < _rows.Count; i++)
            _rows[i].SetHighlighted(i == newIndex);
    }

    void FilterList(string query)
    {
        query = query.ToLowerInvariant();
        foreach (var row in _rows)
        {
            bool matches = string.IsNullOrEmpty(query) ||
                           row.TrackName.ToLowerInvariant().Contains(query);
            row.SetVisible(matches);
        }
    }
}