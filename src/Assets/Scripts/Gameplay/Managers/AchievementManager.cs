using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Achievement
{
    public string id;
    public bool isUnlocked;
    public AchievementUIItem uiElement;
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Achievements")]
    public List<Achievement> achievements = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAchievements();
    }

    public void LoadAchievements()
    {
        foreach (var ach in achievements)
            ach.isUnlocked = PlayerPrefs.GetInt("ach_" + ach.id, 0) == 1;
        RefreshUI();
    }

    public void UnlockAchievement(string id)
    {
        var ach = achievements.Find(a => a.id == id);
        if (ach == null)
        {
            Debug.LogWarning($"[Achievements] '{id}' не найдена!");
            return;
        }
        if (ach.isUnlocked)
            return;

        ach.isUnlocked = true;
        PlayerPrefs.SetInt("ach_" + ach.id, 1);
        PlayerPrefs.Save();
        RefreshUI();
    }

    public void ResetAll()
    {
        foreach (var ach in achievements)
        {
            ach.isUnlocked = false;
            PlayerPrefs.DeleteKey("ach_" + ach.id);
        }
        PlayerPrefs.Save();
        RefreshUI();
    }

    private void RefreshUI()
    {
        foreach (var ach in achievements)
        {
            if (ach.uiElement != null)
                ach.uiElement.Setup(ach);
            else
                Debug.LogWarning($"[Achievements] '{ach.id}' не имеет UI элемента!");
        }
    }
}
