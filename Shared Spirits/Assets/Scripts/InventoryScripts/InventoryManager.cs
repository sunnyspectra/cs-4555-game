using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public Inventory playerInventory;
    public GameObject inventoryUI;
    public GameObject itemSlotPrefab;
    public Transform itemSlotParent;


    private bool isInventoryOpen = false;

    void Start()
    {
        playerInventory = new Inventory();
    }

    public void AddItemToInventory(Item item)
    {
        playerInventory.AddItem(item);
        if (isInventoryOpen)
        {
            UpdateUI();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryUI.SetActive(isInventoryOpen);

        if (isInventoryOpen)
        {
            UpdateUI();
        }
    }

    public void UpdateUI()
    {
        foreach (Item item in playerInventory.items)
        {
            GameObject newItemSlot = Instantiate(itemSlotPrefab, itemSlotParent);
            Image iconImage = newItemSlot.transform.GetChild(0).GetComponent<Image>(); 
            iconImage.sprite = item.icon; 

            Text quantityText = newItemSlot.transform.GetChild(1).GetComponent<Text>();
            quantityText.text = item.quantity.ToString();
        }
    }


}

