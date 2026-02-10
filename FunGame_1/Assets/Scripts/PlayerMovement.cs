using UnityEngine;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 7f;
    public float crouchSpeed = 3f;
    public float groundDrag = 5f;

    [Header("Jumping")]
    public float jumpForce = 1.5f;
    public float airMultiplier = 0.4f;

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

    [Header("Slope Handling")]
    public float maxSlopeAngle = 45f;
    private RaycastHit slopeHit;

    [Header("Slope Speed")]
    public float slopeSpeedMultiplier = 1.3f;   // lebih cepat saat TURUN
    public float slopeSlowMultiplier = 0.8f;    // lebih lambat saat NAIK

    [Header("Slide")]
    public float slideForce = 12f;
    public float slideTime = 0.75f;
    public float slideCooldown = 0.5f;

    [Header("Crouch Settings")]
    public float standHeight = 2f;
    public float crouchHeight = 1f;
    public float cameraStandY = 0.8f;
    public float cameraCrouchY = 0.4f;

    [Header("References")]
    public Transform orientation;
    public Transform playerCamera;

    [Header("UI")]
    public TextMeshProUGUI speedText;

    // private
    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;
    Rigidbody rb;
    CapsuleCollider capsule;

    bool grounded;
    bool jumpHeld;
    bool isCrouching;

    // Slide state
    bool isSliding;
    float slideTimer;
    float slideCooldownTimer;

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

        HandleState();
        HandleJump();
        HandleSlide();
        HandleDrag();
        UpdateSpeedUI();

        if (slideCooldownTimer > 0f)
            slideCooldownTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        MovePlayer();
        SpeedControl();
    }

    void HandleState()
    {
        if (isSliding)
            return;

        if (Input.GetKey(crouchKey))
        {
            if (!isCrouching)
                SetCrouch();

            moveSpeed = crouchSpeed;
        }
        else if (grounded && Input.GetKey(sprintKey))
        {
            // Mau berdiri dulu kalau sebelumnya crouch
            if (isCrouching)
            {
                if (CanStandUp())
                    SetStand();
                else
                {
                    // Masih kepentok atas → tetap crouch
                    moveSpeed = crouchSpeed;
                    return;
                }
            }

            moveSpeed = sprintSpeed;
        }
        else
        {
            if (isCrouching)
            {
                if (CanStandUp())
                    SetStand();
                else
                {
                    // Masih nggak muat berdiri
                    moveSpeed = crouchSpeed;
                    return;
                }
            }

            moveSpeed = walkSpeed;
        }
    }

    void HandleJump()
    {
        if (grounded && jumpHeld && rb.velocity.y <= 0.01f && !isSliding)
        {
            DoJump();
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

    void HandleSlide()
    {
        // Mulai slide: harus grounded, sprint, tekan crouch, dan tidak cooldown
        if (!isSliding && grounded && Input.GetKeyDown(crouchKey) && Input.GetKey(sprintKey) && slideCooldownTimer <= 0f)
        {
            StartSlide();
        }

        // Update slide
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f)
            {
                StopSlide();
            }
        }
    }

    void StartSlide()
    {
        isSliding = true;
        slideTimer = slideTime;

        // Kecilkan collider (seperti crouch)
        SetCrouch();

        // Dorong ke depan sesuai arah pandang
        Vector3 slideDir = orientation.forward;
        rb.AddForce(slideDir.normalized * slideForce, ForceMode.Impulse);
    }

    void StopSlide()
    {
        isSliding = false;
        slideCooldownTimer = slideCooldown;

        // Coba berdiri, kalau tidak muat, tetap crouch
        if (CanStandUp())
            SetStand();
        else
            SetCrouch(); // pastikan tetap crouch, jangan maksa berdiri
    }

    void MovePlayer()
    {
        if (isSliding)
        {
            // Selama slide, tetap dorong sedikit ke depan (biar meluncur)
            Vector3 slideDir = orientation.forward;
            rb.AddForce(slideDir.normalized * slideForce * 0.5f, ForceMode.Force);
            return;
        }

        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (OnSlope())
        {
            Vector3 slopeMoveDir = GetSlopeMoveDirection(moveDirection);
            rb.AddForce(slopeMoveDir.normalized * moveSpeed * 10f, ForceMode.Force);
            rb.useGravity = false;
        }
        else if (grounded)
        {
            rb.useGravity = true;
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            rb.useGravity = true;
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        float maxSpeed = grounded ? maxGroundSpeed : maxAirSpeed;

        if (OnSlope())
        {
            float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
            float t = slopeAngle / maxSlopeAngle; // 0..1

            Vector3 downSlopeDir = Vector3.ProjectOnPlane(Vector3.down, slopeHit.normal).normalized;
            Vector3 moveDir = flatVel.sqrMagnitude > 0.001f ? flatVel.normalized : Vector3.zero;
            float dirDot = Vector3.Dot(moveDir, downSlopeDir);

            if (dirDot > 0.1f)
            {
                maxSpeed *= Mathf.Lerp(1f, slopeSpeedMultiplier, t);
            }
            else if (dirDot < -0.1f)
            {
                maxSpeed *= Mathf.Lerp(1f, slopeSlowMultiplier, t);
            }
        }

        if (flatVel.magnitude > maxSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * maxSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    void HandleDrag()
    {
        if (grounded && !isSliding)
            rb.drag = groundDrag;
        else
            rb.drag = 0f;
    }

    bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, 1.2f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle > 0f && angle <= maxSlopeAngle;
        }
        return false;
    }

    Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
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

    bool CanStandUp()
    {
        float extraHeight = standHeight - crouchHeight;

        // Titik dari tengah collider crouch
        Vector3 origin = transform.position + Vector3.up * (crouchHeight / 2f);

        // Cek ke atas apakah ada halangan
        return !Physics.Raycast(origin, Vector3.up, extraHeight + 0.05f, whatIsGround);
    }


    void UpdateSpeedUI()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float speed = flatVel.magnitude;

        if (speedText != null)
            speedText.text = "Speed: " + speed.ToString("F1");
    }
}
