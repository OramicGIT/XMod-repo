using UnityEngine;
using UnityEngine.UI;

public class AchievementUIItem : MonoBehaviour
{
    public GameObject lockedOverlay;

    public void Setup(Achievement ach)
    {
        if (lockedOverlay != null)
            lockedOverlay.SetActive(!ach.isUnlocked);
    }
}
