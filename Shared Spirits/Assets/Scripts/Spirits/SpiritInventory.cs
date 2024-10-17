using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiritInventory : MonoBehaviour
{
    public List<SpiritDuos> spiritSlots;
}

public class SpiritDuos
{
    public SpiritInInventory spirit1;
    public SpiritInInventory spirit2;

    public SpiritInInventory Spirit1 => spirit1;
    public SpiritInInventory Spirit2 => spirit2;
}

