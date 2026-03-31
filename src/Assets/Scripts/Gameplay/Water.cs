using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Water : MonoBehaviour
{
    [Header("Water Physics")]
    public float floatStrength = 10f;
    public float waterDrag = 3f;
    public float maxMass = 5f;

    private Dictionary<Rigidbody2D, float> bodiesInWater = new Dictionary<Rigidbody2D, float>();

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null)
            return;
        if (!bodiesInWater.ContainsKey(rb))
        {
            bodiesInWater[rb] = rb.drag;
            rb.drag = waterDrag;
        }
    }

    private void FixedUpdate()
    {
        foreach (var kvp in bodiesInWater)
        {
            Rigidbody2D rb = kvp.Key;
            if (rb == null)
                continue;
            if (rb.mass <= maxMass)
                rb.AddForce(Vector2.up * floatStrength, ForceMode2D.Force);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null)
            return;
        if (bodiesInWater.TryGetValue(rb, out float originalDrag))
        {
            rb.drag = originalDrag;
            bodiesInWater.Remove(rb);
        }
    }
}
