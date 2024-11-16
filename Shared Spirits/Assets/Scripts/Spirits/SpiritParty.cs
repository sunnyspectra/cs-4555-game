using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpiritParty : MonoBehaviour
{
    [SerializeField] List<Spirit> spirits;

    public event Action OnUpdated;

    public List<Spirit> Spirits
    {
        get {
            return spirits;
        }
        set {
            spirits = value;
            OnUpdated?.Invoke();
        }
    }

    private void Awake()
    {
        foreach (var spirit in spirits)
        {
            spirit.Init();
        }
    }

    private void Start()
    {

    }

    public Spirit GetHealthySpirit()
    {
        return spirits.Where(x => x.HP > 0).FirstOrDefault();
    }

    public void AddSpirit(Spirit newSpirit)
    {
        if (spirits.Count < 6)
        {
            spirits.Add(newSpirit);
            OnUpdated?.Invoke();
        }
        else
        {
            
        }
    }

    public bool CheckForEvolutions()
    {
        return spirits.Any(p => p.CheckForEvolution() != null);
    }

    public IEnumerator RunEvolutions()
    {
        foreach (var spirit in spirits)
        {
            var evoution = spirit.CheckForEvolution();
            if (evoution != null)
            {
                yield return EvolutionManager.i.Evolve(spirit, evoution);
            }
        }
    }

    public void PartyUpdated()
    {
        OnUpdated?.Invoke();
    }

    public static SpiritParty GetPlayerParty()
    {
        return FindObjectOfType<PlayerController>().GetComponent<SpiritParty>();
    }
}
