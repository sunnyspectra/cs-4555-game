using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Spirit", menuName = "Spirit/spirit")]
public class SpiritInInventory : Spirit
{
    [SerializeField] public int HPcurrent;
    [SerializeField] public int exp;
    [SerializeField] public int level;
  
    public int HPCurrent => HPCurrent;
    public int Exp => exp;
    public int Level => level;
}
