using System.IO;
using UnityEditor;
using UnityEngine;

public class MapPreviewCreator : EditorWindow
{
    private Camera targetCamera;
    private string fileName = "MapPreview";
    private Vector2Int resolution = new Vector2Int(512, 512);

    [MenuItem("Tools/Map Preview Capture")]
    public static void ShowWindow()
    {
        GetWindow<MapPreviewCreator>("Map Capture");
    }

    void OnGUI()
    {
        GUILayout.Label("Map Preview Settings", EditorStyles.boldLabel);

        targetCamera = (Camera)
            EditorGUILayout.ObjectField("Preview Camera", targetCamera, typeof(Camera), true);
        fileName = EditorGUILayout.TextField("File Name", fileName);
        resolution = EditorGUILayout.Vector2IntField("Resolution", resolution);

        if (GUILayout.Button("Capture and Save PNG"))
        {
            Capture();
        }
    }

    private void Capture()
    {
        if (targetCamera == null)
        {
            Debug.LogError("Please assign a Camera!");
            return;
        }

        // 1. Setup Render Texture
        RenderTexture rt = new RenderTexture(resolution.x, resolution.y, 24);
        targetCamera.targetTexture = rt;

        // 2. Render the Camera view
        Texture2D screenShot = new Texture2D(
            resolution.x,
            resolution.y,
            TextureFormat.RGB24,
            false
        );
        targetCamera.Render();

        // 3. Read Pixels into Texture2D
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
        screenShot.Apply();

        // 4. Clean up Camera/RT
        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);

        // 5. Save to Disk
        byte[] bytes = screenShot.EncodeToPNG();
        string folderPath = Path.Combine(Application.dataPath, "MapPreviews");

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string fullPath = Path.Combine(folderPath, fileName + ".png");
        File.WriteAllBytes(fullPath, bytes);

        // 6. Refresh Asset Database so it shows up in Unity immediately
        AssetDatabase.Refresh();
        Debug.Log($"Preview saved to: {fullPath}");
    }
}
