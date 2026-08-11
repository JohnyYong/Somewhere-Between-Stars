using System.IO;
using LLMUnity;
using UnityEngine;

public class SlimeMemoryManager : MonoBehaviour
{
    [SerializeField] private LLMAgent _llmAgent;

    [Tooltip("Must match the Save field on the LLMAgent component exactly, so the correct file gets deleted on clear.")]
    [SerializeField] private string saveFileName = "slime_conversation";

    public System.Action OnMemoryCleared;

    public void ClearMemory()
    {
        if (_llmAgent != null && _llmAgent.chat != null)
        {
            _llmAgent.chat.Clear();
        }

        DeleteSaveFile();

        Debug.Log("Slime memory cleared.");
        OnMemoryCleared?.Invoke();
    }

    void DeleteSaveFile()
    {
        if (string.IsNullOrEmpty(saveFileName)) return;

        string path = Path.Combine(Application.persistentDataPath, saveFileName + ".json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public int GetMessageCount()
    {
        if (_llmAgent == null || _llmAgent.chat == null) return 0;
        return _llmAgent.chat.Count;
    }
}