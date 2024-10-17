using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spirit", menuName = "Spirit/new spirit")]
public class Spirit : ScriptableObject//per level stats or level 100 stats or whatever max is
{
    [SerializeField] public string name;
    [SerializeField] public string type1;
    [SerializeField] public string type2;
    [SerializeField] public Sprite icon;
    [SerializeField] public int HPmax;
    [SerializeField] public int attack; //level 100 - 200 2 attack per level attack = 2
    [SerializeField] public int defense;
    [SerializeField] public int specialAttack;
    [SerializeField] public int specialDefense;
    [SerializeField] public int speed;
    [SerializeField] public int stage;
    //these are currently ints but probably should be doubles so that if a monster has 250 hp at level 100 we can represent it as 2.5

    public string Name => name;
    public string Type1 => type1;
    public string Type2 => type2;
    public Sprite Icon => icon;
    public int HPMax => HPmax;
    public int Attack => attack;
    public int Defense => defense;
    public int SpecialAttack => specialAttack;
    public int SpecialDefense => specialDefense;
    public int Speed => speed;
    public int Stage => stage; 
}
//if we want to make an encounter or change spirits level we would use these stats to calc the new spirit or its new stats.