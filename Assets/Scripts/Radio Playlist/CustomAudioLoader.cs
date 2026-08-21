using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class CustomMusicLoader : MonoBehaviour
{
    [Header("Folder Settings")]
    public string folderName = "CustomMusic";
    private static readonly string[] SupportedExtensions = { ".mp3", ".wav", ".ogg" };

    public List<AudioClip> LoadedClips { get; private set; } = new List<AudioClip>();
    public List<string> LoadedClipNames { get; private set; } = new List<string>();

    public event Action OnTracksChanged;

    public string FolderPath => Path.Combine(Application.persistentDataPath, folderName);

    void Awake()
    {
        if (!Directory.Exists(FolderPath))
            Directory.CreateDirectory(FolderPath);
    }

    public IEnumerator LoadAllTracks()
    {
        LoadedClips.Clear();
        LoadedClipNames.Clear();

        if (!Directory.Exists(FolderPath)) yield break;

        foreach (string filePath in Directory.GetFiles(FolderPath))
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (Array.IndexOf(SupportedExtensions, ext) < 0) continue;

            yield return LoadSingleClip(filePath, ext);
        }

        OnTracksChanged?.Invoke();
    }

    //Called when the user picks a file via the native browser.
    //Copies it into our managed folder, then decodes it into an AudioClip.
    public IEnumerator AddTrackFromExternalPath(string sourcePath, Action<AudioClip> onComplete = null)
    {
        string ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (Array.IndexOf(SupportedExtensions, ext) < 0)
        {
            Debug.LogWarning($"Unsupported file type: {sourcePath}");
            onComplete?.Invoke(null);
            yield break;
        }

        string destPath = GetUniqueDestinationPath(sourcePath);

        try
        {
            File.Copy(sourcePath, destPath, overwrite: false);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to copy {sourcePath}: {e.Message}");
            onComplete?.Invoke(null);
            yield break;
        }

        yield return LoadSingleClip(destPath, ext);

        AudioClip loaded = LoadedClips.Count > 0 ? LoadedClips[LoadedClips.Count - 1] : null;
        onComplete?.Invoke(loaded);
        OnTracksChanged?.Invoke();
    }

    IEnumerator LoadSingleClip(string filePath, string ext)
    {
        AudioType type = GetAudioType(ext);
        string uri = "file://" + filePath;

        using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(uri, type))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Failed to load {filePath}: {req.error}");
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip != null)
            {
                clip.name = Path.GetFileNameWithoutExtension(filePath);
                LoadedClips.Add(clip);
                LoadedClipNames.Add(clip.name);
            }
        }
    }

    //Avoids overwriting a file if the player adds two songs with the same name
    string GetUniqueDestinationPath(string sourcePath)
    {
        string fileName = Path.GetFileName(sourcePath);
        string destPath = Path.Combine(FolderPath, fileName);

        int counter = 1;
        string nameOnly = Path.GetFileNameWithoutExtension(sourcePath);
        string ext = Path.GetExtension(sourcePath);

        while (File.Exists(destPath))
        {
            destPath = Path.Combine(FolderPath, $"{nameOnly} ({counter}){ext}");
            counter++;
        }

        return destPath;
    }

    AudioType GetAudioType(string ext)
    {
        switch (ext)
        {
            case ".mp3": return AudioType.MPEG;
            case ".wav": return AudioType.WAV;
            case ".ogg": return AudioType.OGGVORBIS;
            default: return AudioType.UNKNOWN;
        }
    }
}