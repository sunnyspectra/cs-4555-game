using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HoldItems : Item
{
    public HoldItems(string name, string desc, Sprite itemIcon, int qty)
        : base(name, desc, itemIcon, qty)
    {
    }
}

