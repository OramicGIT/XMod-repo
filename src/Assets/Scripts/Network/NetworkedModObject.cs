using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkedModObject : NetworkBehaviour
{
    private NetworkVariable<FixedString32Bytes> modID = new NetworkVariable<FixedString32Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        ApplyModVisuals(modID.Value.ToString());
        modID.OnValueChanged += (oldVal, newVal) => ApplyModVisuals(newVal.ToString());
    }

    // ----------------------------------------
    //  Public API
    // ----------------------------------------
    public void SetModID(string id)
    {
        if (IsServer)
            modID.Value = id;
    }

    public string GetActiveModID()
    {
        return modID.Value.ToString();
    }

    // ----------------------------------------
    //  Core Visual & Physics Application
    // ----------------------------------------
    private void ApplyModVisuals(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        var data = ModManager.Instance.GetMod(id);
        var settings = data.settings;

        // Sprite
        if (TryGetComponent(out SpriteRenderer sr))
        {
            sr.sprite = data.sprite;
            sr.enabled = true;
        }

        // Collider
        if (TryGetComponent(out BoxCollider2D col))
        {
            col.enabled = settings.isSolid;
            col.size = data.sprite.bounds.size;
        }

        // Rigidbody
        if (TryGetComponent(out Rigidbody2D rb))
        {
            rb.bodyType =
                (settings.isPhysics || settings.isAlive)
                    ? RigidbodyType2D.Dynamic
                    : RigidbodyType2D.Static;

            if (rb.bodyType == RigidbodyType2D.Dynamic)
            {
                rb.mass = settings.mass;
                rb.sharedMaterial = new PhysicsMaterial2D
                {
                    friction = settings.friction,
                    bounciness = settings.bounciness,
                };
            }
        }

        // Character control for alive mods
        if (settings.isAlive && !GetComponent<CharacterControl2D>())
            gameObject.AddComponent<CharacterControl2D>();

        // Animation
        if (data.animationFrames != null && data.animationFrames.Length > 0)
        {
            var anim = GetComponent<LogicAnimator>() ?? gameObject.AddComponent<LogicAnimator>();
            anim.PlayModAnimation(data.animationFrames, settings.animationSpeed);
        }
    }
}
