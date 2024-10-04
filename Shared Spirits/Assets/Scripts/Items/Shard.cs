using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Shard : Item
{
    public Shard(string name, string desc, Sprite itemIcon, int qty)
        : base(name, desc, itemIcon, qty)
    {
    }
}

