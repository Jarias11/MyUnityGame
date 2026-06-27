using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 4f;
    public float runSpeed = 7f;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = new Vector2(0, -1); // starts facing down

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        moveInput = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
        if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
        if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;
        if (Keyboard.current.dKey.isPressed) moveInput.x += 1;

        moveInput = moveInput.normalized;

        bool isMoving = moveInput.sqrMagnitude > 0;
        bool isRunning = Keyboard.current.leftShiftKey.isPressed && isMoving;

        if (isMoving)
        {
            lastMoveDirection = moveInput;
        }

        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", moveInput.y);

        animator.SetFloat("LastMoveX", lastMoveDirection.x);
        animator.SetFloat("LastMoveY", lastMoveDirection.y);

        animator.SetFloat("Speed", moveInput.sqrMagnitude);
        animator.SetBool("IsRunning", isRunning);
    }

    void FixedUpdate()
    {
        float speed = animator.GetBool("IsRunning") ? runSpeed : walkSpeed;
        rb.linearVelocity = moveInput * speed;
    }
}