using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class LocalizationEntry
{
    public string key;

    [TextArea(2, 6)]
    public string en;

    [TextArea(2, 6)]
    public string ru;
}

public class LocaleManager : MonoBehaviour
{
    [Header("Config")]
    public LanguageSettings languageSettings;
    public List<LocalizationEntry> entries = new();

    public static string CurrentLang { get; private set; }

    private static LocaleManager _instance;
    private readonly Dictionary<string, LocalizationEntry> _dict = new();

    void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentLang = PlayerPrefs.GetString("SelectedLanguage", languageSettings.defaultLanguage);
        RebuildDict();
        RefreshAll();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RefreshAll();

    public static string GetValue(string key)
    {
        if (_instance == null || string.IsNullOrEmpty(key))
            return key;
        if (!_instance._dict.TryGetValue(key, out var entry))
            return key;

        return CurrentLang switch
        {
            "ru" => entry.ru,
            _ => entry.en,
        };
    }

    public void SetLanguage(string lang)
    {
        if (!IsSupported(lang))
        {
            Debug.LogWarning($"[Locale] Язык '{lang}' не поддерживается.");
            return;
        }
        CurrentLang = lang;
        PlayerPrefs.SetString("SelectedLanguage", lang);
        RefreshAll();
    }

    public void ToggleLanguage()
    {
        var langs = languageSettings.supportedLanguages;
        if (langs.Length < 2)
            return;

        int next = (System.Array.IndexOf(langs, CurrentLang) + 1) % langs.Length;
        SetLanguage(langs[next]);
    }

    public bool IsSupported(string lang)
    {
        return System.Array.IndexOf(languageSettings.supportedLanguages, lang) >= 0;
    }

    private void RebuildDict()
    {
        _dict.Clear();
        foreach (var entry in entries)
            if (!string.IsNullOrEmpty(entry.key) && !_dict.ContainsKey(entry.key))
                _dict[entry.key] = entry;
    }

    [ContextMenu("Refresh All Localizers")]
    public void RefreshAll()
    {
        RebuildDict();
        foreach (var l in Resources.FindObjectsOfTypeAll<LocalizedTextUI>())
            if (l.gameObject.scene.name != null)
                l.Refresh();
    }
}
