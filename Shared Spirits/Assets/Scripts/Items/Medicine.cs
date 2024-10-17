using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Medicine", menuName = "Inventory/Medicine")]
public class Medicine : Item
{
    [SerializeField] int healAmount;
    [SerializeField] bool restoreMaxHp;
    [SerializeField] int statusRestore; //set as in incase we want to make it like paralysis heal, awakening etc, otherwise can act as bool
    //specific type of item (to be used later for sorting so each item type hass its own tab)
}

