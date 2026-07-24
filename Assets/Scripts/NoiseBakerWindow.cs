#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class NoiseBakerWindow : EditorWindow
{
    private Material _noiseMat;
    private int _resolution = 512;
    private string _path = "Assets/Textures/PlanetNoise.png";

    [MenuItem("Tools/Bake Noise Texture")]
    static void Open() => GetWindow<NoiseBakerWindow>("Bake Noise");

    void OnGUI()
    {
        _noiseMat = (Material)EditorGUILayout.ObjectField("Noise Material", _noiseMat, typeof(Material), false);
        _resolution = EditorGUILayout.IntField("Resolution", _resolution);
        _path = EditorGUILayout.TextField("Save Path", _path);

        if (GUILayout.Button("Bake") && _noiseMat != null)
            Bake();
    }

    void Bake()
    {
        var rt = RenderTexture.GetTemporary(_resolution, _resolution, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(null, rt, _noiseMat);

        var prev = RenderTexture.active;
        RenderTexture.active = rt;

        var tex = new Texture2D(_resolution, _resolution, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, _resolution, _resolution), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        Directory.CreateDirectory(Path.GetDirectoryName(_path));
        File.WriteAllBytes(_path, tex.EncodeToPNG());
        DestroyImmediate(tex);

        AssetDatabase.Refresh();
        Debug.Log($"[NoiseBaker] Saved to {_path}");
    }
}
#endif