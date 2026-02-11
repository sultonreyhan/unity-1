using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 7f;
    public float crouchSpeed = 3f;
    public float groundDrag = 5f;

    [Header("Jumping")]
    public float jumpForce = 5.5f;
    public float airMultiplier = 0.4f;

    [Header("Better Gravity")]
    public float fallMultiplier = 2.8f;
    public float lowJumpMultiplier = 2.0f;

    [Header("Speed Limit")]
    public float maxGroundSpeed = 9f;
    public float maxAirSpeed = 7f;

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

    [Header("UI (Optional)")]
    public TextMeshProUGUI speedText;

    // ===== STATE FLAGS (dipakai script lain) =====
    [HideInInspector] public bool grounded;
    [HideInInspector] public bool wallrunning;
    [HideInInspector] public bool climbing;
    [HideInInspector] public bool holdingLedge;
    [HideInInspector] public bool restricted;

    // ===== PRIVATE =====
    float horizontalInput;
    float verticalInput;
    bool jumpHeld;
    bool isCrouching;
    bool jumpConsumed;

    Vector3 moveDirection;
    Rigidbody rb;
    CapsuleCollider capsule;

    float moveSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        capsule = GetComponent<CapsuleCollider>();

        moveSpeed = walkSpeed;
        SetStand();
    }

    void Update()
    {
        grounded = Physics.CheckSphere(groundCheck.position, groundDistance, whatIsGround);

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        jumpHeld = Input.GetKey(jumpKey);

        // reset izin lompat saat benar-benar mendarat
        if (grounded && rb.velocity.y <= 0.01f)
            jumpConsumed = false;

        HandleState();
        HandleJump();
        HandleDrag();
        UpdateSpeedUI();
    }

    void FixedUpdate()
    {
        if (!restricted && !holdingLedge && !climbing)
        {
            MovePlayer();
            SpeedControl();
        }

        ApplyBetterGravity();
    }

    void HandleState()
    {
        if (Input.GetKey(crouchKey))
        {
            if (!isCrouching)
                SetCrouch();

            moveSpeed = crouchSpeed;
        }
        else if (grounded && Input.GetKey(sprintKey))
        {
            if (isCrouching && CanStandUp())
                SetStand();

            moveSpeed = sprintSpeed;
        }
        else
        {
            if (isCrouching && CanStandUp())
                SetStand();

            moveSpeed = walkSpeed;
        }
    }

    void HandleJump()
    {
        if (grounded && jumpHeld && !jumpConsumed && rb.velocity.y <= 0.01f && !climbing && !holdingLedge)
        {
            DoJump();
            jumpConsumed = true;
        }
    }

    void DoJump()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (flatVel.magnitude > maxAirSpeed)
            flatVel = flatVel.normalized * maxAirSpeed;

        rb.velocity = new Vector3(flatVel.x, 0f, flatVel.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (grounded && !wallrunning)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else if (!grounded && !wallrunning)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
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
        if (grounded && !wallrunning && !climbing && !holdingLedge)
            rb.drag = groundDrag;
        else
            rb.drag = 0f;
    }

    void ApplyBetterGravity()
    {
        if (wallrunning || climbing || holdingLedge) return;

        if (rb.velocity.y < 0)
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * (fallMultiplier - 1f), ForceMode.Acceleration);
        }
        else if (rb.velocity.y > 0 && !Input.GetKey(jumpKey))
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    // ===== CROUCH =====
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

    bool CanStandUp()
    {
        float extraHeight = standHeight - crouchHeight;
        Vector3 origin = transform.position + Vector3.up * (crouchHeight / 2f);
        return !Physics.Raycast(origin, Vector3.up, extraHeight + 0.05f, whatIsGround);
    }

    void UpdateSpeedUI()
    {
        if (speedText == null) return;

        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        speedText.text = "Speed: " + flatVel.magnitude.ToString("F1");
    }
}
