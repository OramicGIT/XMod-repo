using UnityEngine;

public class ObjectControl : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip hitSound;

    [Range(0f, 1f)]
    public float volume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayHitSound();
    }

    private void PlayHitSound()
    {
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound, volume);
        }
    }
}
