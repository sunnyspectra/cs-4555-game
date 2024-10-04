using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Medicine : Item
{
    public Medicine(string name, string desc, Sprite itemIcon, int qty) : base(name, desc, itemIcon, qty)
    {
    }
}

