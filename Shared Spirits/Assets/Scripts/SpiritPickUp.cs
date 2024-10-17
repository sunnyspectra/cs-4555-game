using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiritPickUp : MonoBehaviour
{
    public SpiritInInventory spirit;

    private void OnTriggerEnter(Collider other)
    {
        //item focused version rather than checking palyer collision, will work simialr for AI and grass 

        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Checking for InventoryManager on player.");
            InventoryManager inventoryManager = other.GetComponent<InventoryManager>();
            if (inventoryManager != null)
            {      
        
                    inventoryManager.AddSpiritToInventory(spirit);
                    Destroy(gameObject);
            }
            else
            {
                Debug.LogError("InventoryManager not found on player.");
            }
        }
        else
        {
            Debug.Log("Collided with non-player object.");
        }
    }
}
