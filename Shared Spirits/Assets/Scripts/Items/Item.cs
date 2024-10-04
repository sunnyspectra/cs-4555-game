using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item
{
    public string itemName;
    public string description;
    public Sprite icon;
    public int quantity;

    public Item(string name, string desc, Sprite itemIcon, int qty)
    {
        itemName = name;
        description = desc;
        icon = itemIcon;
        quantity = qty;
    }
}

