using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LearnableMove : Item
{
    public LearnableMove(string name, string desc, Sprite itemIcon, int qty)
        : base(name, desc, itemIcon, qty)
    {
    }
}

