using UnityEngine;

public class MenuToggle : MonoBehaviour
{
    public GameObject menuPanel; // Перетащи сюда саму панель меню
    private bool isOpen = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) // Или любая другая клавиша
        {
            isOpen = !isOpen;
            menuPanel.SetActive(isOpen);
        }
    }
}
