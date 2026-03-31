using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    private const string QUALITY_KEY = "quality_level";
    private const string VOLUME_KEY = "master_volume";

    // first is Low, second is Medium, third is High
    private int[] qualityMap = new int[] { 0, 2, 5 };

    [Header("UI References")]
    public Slider qualityDropdown;
    public Slider volumeSlider;

    private void Awake()
    {
        LoadSettings();
    }

    private void Start()
    {
        int savedQuality = PlayerPrefs.GetInt(QUALITY_KEY, 1);
        if (qualityDropdown != null)
            qualityDropdown.value = savedQuality;

        float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
        if (volumeSlider != null)
            volumeSlider.value = savedVolume;
    }

    public void OpenScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetQuality(int optionIndex)
    {
        int unityQuality = qualityMap[optionIndex];
        QualitySettings.SetQualityLevel(unityQuality);
        PlayerPrefs.SetInt(QUALITY_KEY, optionIndex);
        PlayerPrefs.Save();
    }

    public void SetVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        int savedQuality = PlayerPrefs.GetInt(QUALITY_KEY, 1);
        float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
        SetQuality(savedQuality);
        AudioListener.volume = savedVolume;
    }
}
