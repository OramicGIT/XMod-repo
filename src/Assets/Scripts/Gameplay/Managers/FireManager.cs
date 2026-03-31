using Unity.Netcode;
using UnityEngine;

public enum MaterialType
{
    Matter, // default if someone asked
    Wood,
    Metal,
    Fabric,
    Plastic,
}

public class FireManager : NetworkBehaviour
{
    [Header("Material")]
    public MaterialType materialType = MaterialType.Matter;

    [Header("Fire Settings")]
    public float burnTime = 5f;
    public float spreadRadius = 1.5f;
    public float spreadInterval = 1f;
    public float igniteChance = 0.7f;

    private NetworkVariable<bool> isBurning = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> isBurned = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float burnTimer = 0f;
    private float spreadTimer = 0f;
    private SpriteRenderer sr;
    private Color originalColor;
    private GameObject fireVisual;

    public override void OnNetworkSpawn()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            originalColor = sr.color;

        isBurning.OnValueChanged += OnBurningChanged;
        isBurned.OnValueChanged += OnBurnedChanged;
    }

    public override void OnNetworkDespawn()
    {
        isBurning.OnValueChanged -= OnBurningChanged;
        isBurned.OnValueChanged -= OnBurnedChanged;
    }

    private void OnBurningChanged(bool _, bool burning)
    {
        if (burning)
            CreateFireVisual();
        else if (fireVisual != null)
            Destroy(fireVisual);
    }

    private void OnBurnedChanged(bool _, bool burned)
    {
        if (!burned)
            return;
        if (fireVisual != null)
            Destroy(fireVisual);
        if (sr != null)
            sr.color = Color.black;
    }

    void Update()
    {
        // Visual burning effect for clients
        if (isBurning.Value && !isBurned.Value && sr != null)
        {
            float t = Mathf.PingPong(Time.time * 3f, 1f);
            sr.color = Color.Lerp(originalColor, Color.red, t);
        }

        if (!IsServer || !isBurning.Value || isBurned.Value)
            return;

        burnTimer += Time.deltaTime;
        spreadTimer += Time.deltaTime;

        if (spreadTimer >= spreadInterval)
        {
            spreadTimer = 0f;
            SpreadFire();
        }

        if (burnTimer >= burnTime)
            FinishBurning();
    }

    [ServerRpc(RequireOwnership = false)]
    public void IgniteServerRpc()
    {
        if (isBurned.Value || isBurning.Value)
            return;
        isBurning.Value = true;
    }

    public void Ignite()
    {
        if (!IsServer || isBurned.Value || isBurning.Value)
            return;
        isBurning.Value = true;
    }

    private void SpreadFire()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, spreadRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject)
                continue;
            FireManager other = hit.GetComponent<FireManager>();
            if (other == null || other.isBurning.Value || other.isBurned.Value)
                continue;

            if (Random.value <= igniteChance)
                other.Ignite();
        }
    }

    private void FinishBurning()
    {
        isBurning.Value = false;
        isBurned.Value = true;
    }

    private void CreateFireVisual()
    {
        fireVisual = new GameObject("FireVisual");
        fireVisual.transform.SetParent(transform);
        fireVisual.transform.localPosition = Vector3.zero;

        SpriteRenderer fireSR = fireVisual.AddComponent<SpriteRenderer>();
        fireSR.color = new Color(1f, 0.5f, 0f, 0.8f);
        fireSR.sortingOrder = 20;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        fireSR.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        fireVisual.transform.localScale = new Vector3(0.5f, 0.8f, 1f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spreadRadius);
    }
}
