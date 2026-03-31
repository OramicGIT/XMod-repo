using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DestroyTrigger : NetworkBehaviour
{
    public float fadeDuration = 1f;

    private HashSet<GameObject> processingObjects = new HashSet<GameObject>();

    private bool HasNetworking =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger)
            return;
        if (HasNetworking && !IsServer)
            return;

        GameObject target =
            other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;

        if (processingObjects.Contains(target))
            return;

        Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
        if (rb == null || rb.isKinematic)
            return;

        processingObjects.Add(target);
        StartCoroutine(FadeOutAndDestroy(target, target.GetComponent<NetworkObject>()));
    }

    private IEnumerator FadeOutAndDestroy(GameObject obj, NetworkObject netObj)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            Color originalColor = sr.color;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                if (obj == null)
                    yield break;
                elapsed += Time.deltaTime;
                sr.color = new Color(
                    originalColor.r,
                    originalColor.g,
                    originalColor.b,
                    Mathf.Lerp(originalColor.a, 0f, elapsed / fadeDuration)
                );
                yield return null;
            }

            if (sr != null)
                sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        }

        if (obj == null)
            yield break;

        if (HasNetworking && netObj != null && netObj.IsSpawned)
            netObj.Despawn();
        else
            Destroy(obj);
    }
}
