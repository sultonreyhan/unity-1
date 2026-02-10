using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 9f;
    public float crouchSpeed = 3f;
    public float groundDrag = 5f;

    [Header("Jumping")]
    public float jumpForce = 6.5f;     // turunkan sedikit biar nggak roket
    public float airMultiplier = 0.4f;

    [Header("Speed Limit")]
    public float maxGroundSpeed = 9f;  // batas speed di darat
    public float maxAirSpeed = 7f;     // batas speed di udara

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask whatIsGround;

    [Header("Crouch Settings")]
    public float standHeight = 2f;
    public float crouchHeight = 1f;
    public float cameraStandY = 0.8f;
    public float cameraCrouchY = 0.4f;

    [Header("References")]
    public Transform orientation;
    public Transform playerCamera;

    // private
    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;
    Rigidbody rb;
    CapsuleCollider capsule;

    bool grounded;
    bool jumpHeld;
    bool isCrouching;

    float moveSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        capsule = GetComponent<CapsuleCollider>();

        // set awal
        moveSpeed = walkSpeed;
        SetStand();
    }

    void Update()
    {
        // Ground check
        grounded = Physics.CheckSphere(groundCheck.position, groundDistance, whatIsGround);

        // Input
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        jumpHeld = Input.GetKey(jumpKey);

        HandleState();
        HandleJump();
        HandleDrag();
    }

    void FixedUpdate()
    {
        MovePlayer();
        SpeedControl();
    }

    void HandleState()
    {
        // Crouch
        if (Input.GetKey(crouchKey))
        {
            if (!isCrouching)
                SetCrouch();

            moveSpeed = crouchSpeed;
        }
        // Sprint
        else if (grounded && Input.GetKey(sprintKey))
        {
            if (isCrouching)
                SetStand();

            moveSpeed = sprintSpeed;
        }
        // Walk
        else
        {
            if (isCrouching)
                SetStand();

            moveSpeed = walkSpeed;
        }
    }

    void HandleJump()
    {
        // HOLD TO JUMP: kalau Space ditahan, lompat saat grounded
        if (grounded && jumpHeld && rb.velocity.y <= 0.01f)
        {
            DoJump();
        }
    }

    void DoJump()
    {
        // Batasi carry speed horizontal saat lompat (biar nggak roket)
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (flatVel.magnitude > maxAirSpeed)
        {
            flatVel = flatVel.normalized * maxAirSpeed;
        }

        // Reset Y velocity supaya tidak numpuk gaya
        rb.velocity = new Vector3(flatVel.x, 0f, flatVel.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
    }

    void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        float maxSpeed = grounded ? maxGroundSpeed : maxAirSpeed;

        if (flatVel.magnitude > maxSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * maxSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    void HandleDrag()
    {
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0f;
    }

    void SetCrouch()
    {
        isCrouching = true;

        capsule.height = crouchHeight;
        capsule.center = new Vector3(0f, crouchHeight / 2f, 0f);

        Vector3 camPos = playerCamera.localPosition;
        camPos.y = cameraCrouchY;
        playerCamera.localPosition = camPos;
    }

    void SetStand()
    {
        isCrouching = false;

        capsule.height = standHeight;
        capsule.center = new Vector3(0f, standHeight / 2f, 0f);

        Vector3 camPos = playerCamera.localPosition;
        camPos.y = cameraStandY;
        playerCamera.localPosition = camPos;
    }
}
