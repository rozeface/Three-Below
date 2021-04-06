using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    Rigidbody2D rb;

    [Header("Movement")]
    public bool canMove = true;
    [SerializeField] float walkSpeed;
    public bool walking = false;
    float x, y; // horiz and vert input

    [Header("Interaction")]
    [SerializeField] LayerMask interactLayer;
    [SerializeField] float interactDist; // how far player can interact with objects
    public bool switchedToMonitor = false;
    bool canInteract = true;
    [SerializeField] float interactCoolDown;
    [SerializeField] Animator dialogueAnimator;

    [Header("Jumping / Ground Check")]
    [SerializeField] Transform groundCheck;
    public bool grounded = false;
    [SerializeField] float groundDist; // how far are we checking for ground?
    [SerializeField] float jumpForce;
    [SerializeField] float jumpCoolDown;
    bool pressingJump = false;
    bool canJump = false;

    [Header("Misc")]
    [SerializeField] GameObject spriteObj;
    public Animator animator;
    bool flipped = false;

    [Header("Character Switching")]
    public bool activeCharacter = false;

    [Header("Life Number")]
    [SerializeField] Text lifeDisplay;
    public int currentLifeLeft;
    int startingLife = 3;
    bool initiatedDeath = false;

    private void Awake()
    {
        rb = this.GetComponent<Rigidbody2D>();
        animator = spriteObj.GetComponent<Animator>();
    }

    private void Start()
    {
        currentLifeLeft = startingLife;
    }

    private void Update()
    {
        if (activeCharacter && canMove)
        {
            PlayerInput();
            GroundCheck();
            AnimationState();
        }

        if (lifeDisplay) lifeDisplay.text = currentLifeLeft.ToString();

        DeathTasks();
    }

    private void FixedUpdate()
    {
        if (activeCharacter && canMove)
        {
            PlayerMovement();
        }
    }

    void PlayerInput()
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");

        // walking
        if (Mathf.Abs(x) > 0f) walking = true;
        else walking = false;

        // jumping
        if (Input.GetButton("Jump")) pressingJump = true;
        else pressingJump = false;

        // interacting
        if (Input.GetKey(KeyCode.E))
        {
            RaycastHit2D hitLeft = Physics2D.Raycast(transform.position, -transform.right, interactDist, interactLayer);
            RaycastHit2D hitRight = Physics2D.Raycast(transform.position, transform.right, interactDist, interactLayer);

            if (hitLeft.collider != null)
            {
                if (hitLeft.collider.CompareTag("Computer"))
                {
                    if (!switchedToMonitor)
                    {
                        GameManager.instance.ActivateMonitor(CurrentPlayer());
                        Debug.Log("Should switch to monitor now.");
                        switchedToMonitor = true;
                    }
                }

                if (hitLeft.collider.CompareTag("Door")) DoorInteraction();
            }

            if (hitRight.collider != null)
            { 
                if (hitRight.collider.CompareTag("Bed")) BedInteraction();
                if (hitRight.collider.CompareTag("Door")) DoorInteraction();
            }
        }
        // facing direction
        if (x > 0) FlipSprite(true);
        else if (x < 0) FlipSprite(false);
    }

    void ResetInteraction()
    {
        canInteract = true;
    }

    void BedInteraction()
    {
        if (canInteract)
        {
            if (CurrentPlayer() == 0) SoundManager.instance.PlaySound(SoundManager.instance.hmHmNoise, 2f, .8f);
            else if (CurrentPlayer() == 1) SoundManager.instance.PlaySound(SoundManager.instance.hmHmNoise, 2f, 1.8f);
            else if (CurrentPlayer() == 2) SoundManager.instance.PlaySound(SoundManager.instance.hmHmNoise, 2f, 1f);
            dialogueAnimator.SetTrigger("ShowText");
            dialogueAnimator.GetComponent<Text>().text = "I can't sleep... I need to find a way out of here.";
            canInteract = false;
            Invoke(nameof(ResetInteraction), 4f);
        }
    }

    void DoorInteraction()
    {
        if (canInteract)
        {
            if (CurrentPlayer() == 0) SoundManager.instance.PlaySound(SoundManager.instance.hmNoise, 2f, 1f);
            else if (CurrentPlayer() == 1) SoundManager.instance.PlaySound(SoundManager.instance.hmNoise, 2f, 1.8f);
            else if (CurrentPlayer() == 2) SoundManager.instance.PlaySound(SoundManager.instance.hmNoise, 2f, 1.2f);
            dialogueAnimator.GetComponent<Text>().text = "It's locked... just like always.";
            dialogueAnimator.SetTrigger("ShowText");
            canInteract = false;
            Invoke(nameof(ResetInteraction), 4f);
        }
    }

    void PlayerMovement()
    {
        if (grounded)
        {
            if (walking) rb.AddForce(x * Vector2.right * walkSpeed * Time.deltaTime);

            if (pressingJump && canJump)
            {
                // jump here
                rb.AddForce(jumpForce * Vector2.up, ForceMode2D.Impulse);
                canJump = false;

                Invoke(nameof(ResetJump), jumpCoolDown);
            }
        }
    }

    void AnimationState()
    {
        if (walking && this.enabled) animator.SetBool("Walking", true);
        else animator.SetBool("Walking", false);
    }

    void ResetJump()
    {
        canJump = true;
    }

    void GroundCheck()
    {
        grounded = Physics2D.Raycast(groundCheck.position, -Vector2.up, groundDist);
    }

    void DeathTasks()
    {
        if (currentLifeLeft <= 0 && ! initiatedDeath)
        {
            GameManager.instance.LostTheGame(CurrentPlayer());
            initiatedDeath = true;
        }
    }


    void FlipSprite(bool faceRight) // flip the sprite object instead of using .flipX which can interfere with rotation-related code
    {
        if (faceRight) spriteObj.transform.eulerAngles = new Vector3(0f, 0f);
        else spriteObj.transform.eulerAngles = new Vector3(0f, 180f);
    }

    public void LoseLife()
    {
        currentLifeLeft -= 1;
    }

    int CurrentPlayer()
    {
        if (this.CompareTag("Player1")) return 0;
        else if (this.CompareTag("Player2")) return 1;
        else return 2;
    }
}
