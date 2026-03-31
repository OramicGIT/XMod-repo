using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class OfficialMapEntry
{
    public string localizationKey;
    public Sprite previewIcon;
    public string sceneName;
}

public class MapGridCreator : MonoBehaviour
{
    [Header("Official Maps Content")]
    public List<OfficialMapEntry> officialMaps;

    [Header("UI Setup")]
    public GameObject mapButtonPrefab;
    public Transform gridParent;
    public Sprite customMapDefaultIcon;

    private void Start() => RefreshGrid();

    public void RefreshGrid()
    {
        ClearGrid();
        AddOfficialMaps();
        AddCustomMaps();
    }

    private void ClearGrid()
    {
        foreach (Transform child in gridParent)
            Destroy(child.gameObject);
    }

    private void AddOfficialMaps()
    {
        foreach (var entry in officialMaps)
            CreateButton(entry.localizationKey, null, entry.previewIcon, entry.sceneName);
    }

    private void AddCustomMaps()
    {
        string path = Application.persistentDataPath;
        if (!Directory.Exists(path))
            return;

        int maxCustomMaps = 50; // Prevent overflow with too many custom maps
        int count = 0;
        foreach (string file in Directory.GetFiles(path, "*.json"))
        {
            if (count >= maxCustomMaps)
                break;
            string fileName = Path.GetFileNameWithoutExtension(file);
            string json = File.ReadAllText(file);
            CreateButton(fileName, json, customMapDefaultIcon, "Level");
            count++;
        }
    }

    private void CreateButton(string key, string json, Sprite icon, string scene)
    {
        GameObject btn = Instantiate(mapButtonPrefab, gridParent);

        // Set localized text
        LocalizedTextUI localized = btn.GetComponentInChildren<LocalizedTextUI>();
        if (localized != null)
        {
            localized.key = key;
            localized.Refresh();
        }

        // Set icon
        Transform iconTransform = btn.transform.Find("Icon");
        if (iconTransform != null && iconTransform.TryGetComponent<Image>(out var img))
            img.sprite = icon;

        // Set button click
        if (btn.TryGetComponent<Button>(out var button))
        {
            button.onClick.AddListener(() =>
            {
                // Only create flag if this is a CUSTOM map
                if (!string.IsNullOrEmpty(json))
                {
                    GameObject flag = new GameObject("CMap");
                    flag.AddComponent<Flag>();
                    MapTransfer.JsonToLoad = json;
                }
                else
                {
                    MapTransfer.JsonToLoad = null;
                }

                SceneManager.LoadScene(scene);
            });
        }
    }
}

// Static holder for JSON map data
public static class MapTransfer
{
    public static string JsonToLoad;
}
