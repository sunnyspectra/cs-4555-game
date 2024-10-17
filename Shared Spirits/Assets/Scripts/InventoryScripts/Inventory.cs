using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Inventory
{
    public List<ItemSlot> items;
}

public class ItemSlot
{
    public Item item;
    public int count;

    public Item Item => item;
    public int Count => count;

    public void AddToCount(int amount)
    {
        count += amount;
    }

    public void RemoveFromCount(int amount)
    {
        count = Mathf.Max(0, count - amount);
    }
}