using UnityEngine;
using UnityEngine.UI;

public class LocalizedTextUI : MonoBehaviour
{
    public string key;
    private Text _textComponent;

    private void Start() => Refresh();

    private void OnDestroy() => _textComponent = null;

    public void Refresh()
    {
        if (_textComponent == null)
            _textComponent = GetComponent<Text>();
        if (_textComponent == null)
            return;

        if (string.IsNullOrEmpty(key))
            return;

        _textComponent.text =
            key == "LANGUAGE_DISPLAY"
                ? (LocaleManager.CurrentLang == "en" ? "English" : "Русский")
                : LocaleManager.GetValue(key);
    }
}
