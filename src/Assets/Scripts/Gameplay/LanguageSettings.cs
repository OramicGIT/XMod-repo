using UnityEngine;

[CreateAssetMenu(fileName = "LanguageSettings", menuName = "Localization/Language Settings")]
public class LanguageSettings : ScriptableObject
{
    public string defaultLanguage = "en";
    public string[] supportedLanguages = { "en", "ru" };
}
