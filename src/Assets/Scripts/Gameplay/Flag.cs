using UnityEngine;

public class Flag : MonoBehaviour
{
    void Awake() => DontDestroyOnLoad(gameObject);
}
