using System.Collections.Generic;
using UnityEngine;

public class SBox : MonoBehaviour
{
    public static List<Drag> selected = new List<Drag>();
    public bool isSelectPortal = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Drag d = other.GetComponent<Drag>();
        if (d == null)
            return;

        if (isSelectPortal)
        {
            if (!selected.Contains(d))
                selected.Add(d);
        }
        else
        {
            selected.Remove(d);
        }
    }
}
