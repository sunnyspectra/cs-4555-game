using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Hold Item", menuName = "Inventory/Create new hold item")]
public class HoldItems : Item
{
    [SerializeField] int healAmount;
    [SerializeField] bool persistance;
    [SerializeField] int statusRestore;
    [SerializeField] float damageIncrease;
    [SerializeField] string type;
    [SerializeField] float statIncrease;
    [SerializeField] string statType;
    [SerializeField] string statType1;
    [SerializeField] string statType2;
    [SerializeField] string statType3;
    [SerializeField] string statType4;
}
