using UnityEngine;

public class OnLaunch : MonoBehaviour
{
    void Start()
    {
        AchievementManager.Instance.UnlockAchievement("PLAYED");
    }
}
