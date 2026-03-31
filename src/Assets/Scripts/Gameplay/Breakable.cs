using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Breakable : NetworkBehaviour
{
    [Header("Break Settings")]
    public float breakForce = 10f;
    public int fragmentCount = 6;
    public float fragmentLifetime = 5f;
    public float fragmentForce = 5f;

    [Header("Fragment Appearance")]
    public float fragmentSize = 0.9f;

    private SpriteRenderer sr;
    private NetworkVariable<bool> broken = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        sr = GetComponent<SpriteRenderer>();
        broken.OnValueChanged += OnBrokenChanged;
    }

    public override void OnNetworkDespawn()
    {
        broken.OnValueChanged -= OnBrokenChanged;
    }

    private void OnBrokenChanged(bool oldValue, bool newValue)
    {
        if (newValue)
            SpawnFragments();
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!IsServer)
            return;
        if (broken.Value)
            return;

        float force = col.relativeVelocity.magnitude;
        if (force >= breakForce)
            Break();
    }

    public void Break()
    {
        if (!IsServer)
            return;
        if (broken.Value)
            return;
        broken.Value = true;
    }

    private void SpawnFragments()
    {
        if (sr == null || sr.sprite == null)
            return;

        Texture2D originalTex = sr.sprite.texture;
        Color spriteColor = sr.color;

        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject fragment = new GameObject("Fragment");
            fragment.transform.position =
                transform.position + (Vector3)Random.insideUnitCircle * 0.3f;
            fragment.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            float size = fragmentSize * Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            fragment.transform.localScale = new Vector3(size, size, 1f);

            SpriteRenderer fragSR = fragment.AddComponent<SpriteRenderer>();
            fragSR.sprite = CreateFragmentSpriteFromOriginal(originalTex);
            fragSR.color = spriteColor;
            fragSR.sortingOrder = 10;

            Rigidbody2D rb = fragment.AddComponent<Rigidbody2D>();
            fragment.AddComponent<PolygonCollider2D>();

            Vector2 dir = Random.insideUnitCircle.normalized;
            rb.AddForce(dir * fragmentForce, ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(-10f, 10f), ForceMode2D.Impulse);

            StartCoroutine(ShrinkAndDestroy(fragment, fragmentLifetime, 5f));
        }

        if (IsServer)
            GetComponent<NetworkObject>().Despawn();
    }

    private Sprite CreateFragmentSpriteFromOriginal(Texture2D original)
    {
        int w = Random.Range(4, 16); // small random fragment width
        int h = Random.Range(4, 16); // small random fragment height

        w = Mathf.Min(w, original.width);
        h = Mathf.Min(h, original.height);

        int x = Random.Range(0, original.width - w + 1);
        int y = Random.Range(0, original.height - h + 1);

        Color[] pixels = original.GetPixels(x, y, w, h);
        Texture2D tex = new Texture2D(w, h);
        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    private IEnumerator ShrinkAndDestroy(
        GameObject fragment,
        float lifetime,
        float delayBeforeShrink
    )
    {
        yield return new WaitForSeconds(delayBeforeShrink);

        float shrinkTime = lifetime - delayBeforeShrink;
        float elapsed = 0f;
        Vector3 originalScale = fragment.transform.localScale;

        while (elapsed < shrinkTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkTime;
            fragment.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(fragment);
    }
}
