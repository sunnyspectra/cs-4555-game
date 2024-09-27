using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour, Interactable
{
    public IEnumerator Interact(Transform initiator)
    {
        Debug.Log("Working");
        yield break;
    }
}
