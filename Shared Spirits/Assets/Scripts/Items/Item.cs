using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dont use", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [SerializeField] public string name;
    [SerializeField] public string description;
    [SerializeField] public Sprite icon;
    [SerializeField] public int quantity;


    public string Name => name;
    public string Description => description;
    public Sprite Icon => icon;
    public int Quantity => quantity;
}

