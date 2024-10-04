using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public Item item;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            InventoryManager inventoryManager = other.GetComponent<InventoryManager>();
            if (inventoryManager != null)
            {
                inventoryManager.AddItemToInventory(item);
                Destroy(gameObject);
            }
        }
    }

   
    public Item GetItem()
    {
        return item;
    }
}
