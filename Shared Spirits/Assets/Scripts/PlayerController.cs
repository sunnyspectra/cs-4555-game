using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Player1Controls control;

    Sprite sprite;
    public float speed;
    public Rigidbody rb;
    private Animator anim;
    private Vector3 localScale;
    private Vector3 moveDirection;

    //private Character character;

    private void Awake()
    {
        //character = GetComponent<Character>();
        control = new Player1Controls();
    }

    private void OnEnable()
    {
        control.Enable();
    }

    private void OnDisable()
    {
        control.Disable();
    }

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        localScale = transform.localScale;
        anim = GetComponent<Animator>();
    }

    public void HandleUpdate()
    {
        // Read input from the control system
        Vector2 input = control.Player.Move.ReadValue<Vector2>();
        moveDirection = new Vector3(input.x, 0, input.y).normalized;

        // Handle animation state
        anim.SetBool("isRun", moveDirection.magnitude > 0);

        // Handle sprite flipping based on left/right movement
        if (input.x != 0)
        {
            float facingDirection = Mathf.Sign(input.x); // 1 for right, -1 for left
            transform.localScale = new Vector3(
                Mathf.Abs(localScale.x) * facingDirection, localScale.y, localScale.z);
        }

        if (Input.GetKeyDown(KeyCode.Z))
            StartCoroutine(Interact());
    }

    private void FixedUpdate()
    {
        // Move the player
        rb.MovePosition(transform.position + moveDirection * speed * Time.fixedDeltaTime);
    }



    IPlayerTriggerable currentlyInTrigger;
    private void OnMoveOver()
    {

    }

    IEnumerator Interact()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            yield return collider.GetComponent<Interactable>()?.Interact(transform);
        }

    }

    /*public object CaptureState()
    {

        return saveData;
    }*/

    public string Name
    {
        get => name;
    }

    public Sprite Sprite
    {
        get => sprite;
    }

    //public Character Character => character;
}

public class PlayerSaveData
{
    public List<SpiritSaveData> spirits;
}