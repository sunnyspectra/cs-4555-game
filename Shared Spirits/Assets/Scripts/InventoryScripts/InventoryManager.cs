using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public GameObject inventoryUI;
    public GameObject itemSlotPrefab;
    public Transform itemSlotParent;
    public Inventory inventory;

    public GameObject spiritUI;
    public GameObject spiritSlotPrefab;
    public Transform spiritSlotParent;

    public SpiritInventory spiritInventory;




    private bool isInventoryOpen = true;
    private bool isSpiritsOpen = true;

    void Start()
    {
        inventory = new Inventory { items = new List<ItemSlot>() };
        ToggleInventory(); //when loading game inven is open, so toggles it off
        spiritInventory = new SpiritInventory { spiritSlots = new List<SpiritDuos>() };
        ToggleSpiritInventory();
    }


    public void AddItemToInventory(Item item)
    {

        ItemSlot existingSlot = inventory.items.Find(slot => slot.Item.name == item.name);
        if (existingSlot != null)
        {
            existingSlot.AddToCount(item.quantity);
        }
        else
        {
            inventory.items.Add(new ItemSlot { item = item, count = item.quantity });
        }

        if (isInventoryOpen)
        {
            UpdateUI();
        }
    }

    public void AddSpiritToInventory(SpiritInInventory spirit)
    {

            spiritInventory.spiritSlots.Add(new SpiritDuos { spirit1 = spirit, spirit2 = spirit });

    }
   

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
        else if (Input.GetKeyDown(KeyCode.O))
        {
            ToggleSpiritInventory();
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

    public void ToggleSpiritInventory()
    {
        isSpiritsOpen = !isSpiritsOpen;
        spiritUI.SetActive(isSpiritsOpen);

        if (isSpiritsOpen)
        {
            UpdateSpiritUI();
        }
    }

    public void UpdateUI()
    {
        foreach (Transform child in itemSlotParent)
        {
            Destroy(child.gameObject);
        } //removes existing displayed items from screen 

        foreach (ItemSlot slot in inventory.items)
        {
            GameObject newItemSlot = Instantiate(itemSlotPrefab, itemSlotParent);
            Image iconImage = newItemSlot.transform.GetChild(0).GetComponent<Image>();
            iconImage.sprite = slot.Item.icon;
                                               
            TextMeshProUGUI itemNameText = newItemSlot.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            itemNameText.text = slot.Item.name;
            TextMeshProUGUI quantityText = newItemSlot.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
            quantityText.text = slot.Count.ToString();
        }
    }

    public void UpdateSpiritUI()
    {
        foreach (Transform child in spiritSlotParent)
        {
            Destroy(child.gameObject);
        } //removes existing displayed items from screen 

        foreach (SpiritDuos duo in spiritInventory.spiritSlots)
        {
            GameObject newSpiritDuo = Instantiate(spiritSlotPrefab, spiritSlotParent);
            Image iconImage = newSpiritDuo.transform.GetChild(0).GetComponent<Image>();
            iconImage.sprite = duo.spirit1.icon;
            TextMeshProUGUI spiritNameText = newSpiritDuo.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            spiritNameText.text = duo.spirit1.name;

            Image iconImage1 = newSpiritDuo.transform.GetChild(2).GetComponent<Image>();
            iconImage1.sprite = duo.spirit2.icon;
            TextMeshProUGUI spiritNameText1 = newSpiritDuo.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
            spiritNameText1.text = duo.spirit2.name;
        }
    }
}

