using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogicAnimator : MonoBehaviour
{
    [System.Serializable]
    public class Keyframe
    {
        public string id;
        public Vector3 position;
        public float rotation;
        public Sprite sprite;
        public float duration = 1.0f;

        [Header("Sequencing")]
        public string nextId;
        public bool loop;

        [Header("Auto Play")]
        public bool playOnStart = false; // <-- New flag
    }

    public List<Keyframe> keyframes = new List<Keyframe>();

    private Rigidbody2D rb2d;
    private SpriteRenderer sr;
    private Coroutine activeRoutine;
    private Coroutine modAnimRoutine;

    void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (rb2d != null)
        {
            rb2d.bodyType = RigidbodyType2D.Kinematic;
            rb2d.useFullKinematicContacts = true;
        }
    }

    void Start()
    {
        // Automatically play keyframes flagged with playOnStart
        foreach (var key in keyframes)
        {
            if (key.playOnStart)
            {
                Play(key.id);
                break; // Only start the first flagged keyframe
            }
        }
    }

    public void PlayModAnimation(Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0)
            return;
        if (modAnimRoutine != null)
            StopCoroutine(modAnimRoutine);
        modAnimRoutine = StartCoroutine(ModAnimRoutine(frames, fps));
    }

    private IEnumerator ModAnimRoutine(Sprite[] frames, float fps)
    {
        int currentFrame = 0;
        float delay = 1f / fps;
        while (true)
        {
            if (sr != null)
                sr.sprite = frames[currentFrame];
            currentFrame = (currentFrame + 1) % frames.Length;
            yield return new WaitForSeconds(delay);
        }
    }

    public void Play(string id)
    {
        Keyframe key = keyframes.Find(k => k.id == id);
        if (key != null)
        {
            if (activeRoutine != null)
                StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(Animate(key));
        }
    }

    private IEnumerator Animate(Keyframe key)
    {
        if (key.sprite != null && sr != null)
            sr.sprite = key.sprite;
        Vector3 startPos = transform.position;
        float startRot = rb2d != null ? rb2d.rotation : transform.eulerAngles.z;
        float elapsed = 0;
        while (elapsed < key.duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / key.duration);
            Vector3 nextPos = Vector3.Lerp(startPos, key.position, t);
            float nextRot = Mathf.LerpAngle(startRot, key.rotation, t);
            if (rb2d != null)
            {
                rb2d.MovePosition(nextPos);
                rb2d.MoveRotation(nextRot);
            }
            else
            {
                transform.position = nextPos;
                transform.eulerAngles = new Vector3(0, 0, nextRot);
            }
            yield return null;
        }

        if (key.loop)
            Play(key.id);
        else if (!string.IsNullOrEmpty(key.nextId))
            Play(key.nextId);
    }
}
