using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed;
    public Rigidbody rb;
    private Animator anim;
    private Vector3 localScale;
    private InventoryManager inventoryManager;

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        localScale = transform.localScale;
        anim = GetComponent<Animator>();
        inventoryManager = FindObjectOfType<InventoryManager>();
    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        Vector3 moveDir = new Vector3(x, 0, y);
        rb.velocity = moveDir * speed;
        anim.SetBool("isRun", true);

        if (x > 0) 
        {
            transform.localScale = new Vector3(Mathf.Abs(localScale.x), localScale.y, localScale.z);
        }
        else if (x < 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(localScale.x), localScale.y, localScale.z);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Item"))
        {
            Item item = other.gameObject.GetComponent<ItemPickup>().GetItem();
            inventoryManager.AddItemToInventory(item);
            Destroy(other.gameObject);
        }
    }
}
