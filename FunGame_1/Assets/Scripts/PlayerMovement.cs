using UnityEngine;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 9f;
    public float crouchSpeed = 3f;
    public float groundDrag = 5f;

    [Header("Jumping")]
    public float jumpForce = 6.5f;
    public float airMultiplier = 0.4f;

    [Header("Speed Limit")]
    public float maxGroundSpeed = 9f;
    public float maxAirSpeed = 7f;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl; // crouch & slide

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask whatIsGround;

    [Header("Crouch Settings")]
    public float standHeight = 2f;
    public float crouchHeight = 1f;
    public float cameraStandY = 0.8f;
    public float cameraCrouchY = 0.4f;

    [Header("Slide Settings")]
    public float minSpeedToSlide = 9f;        // harus sudah ngebut
    public float slideBoostMultiplier = 1.2f; // boost awal kecil
    public float slideForce = 8f;              // dorongan kecil tiap frame
    public float slideDuration = 0.8f;         // durasi minimum
    public float slideCooldown = 1.0f;
    public float slideDrag = 0.5f;              // drag kecil saat slide (biar meluncur)
    public float slideSteerStrength = 3f;   // seberapa kuat belok saat slide (kecil = berat)
    public float slideBrakeStrength = 4f;   // seberapa kuat ngerem pakai S

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
    bool isCrouching;
    bool isSliding;

    float moveSpeed;
    float slideTimer;
    float lastSlideTime;

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

        HandleSlideAndCrouchInput();
        HandleState();
        HandleJump();
        HandleDrag();
        UpdateSpeedUI();
    }

    void FixedUpdate()
    {
        HandleSliding();
        MovePlayer();
        SpeedControl();
    }

    // ================= INPUT =================

    void HandleSlideAndCrouchInput()
    {
        float currentSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;

        if (Input.GetKeyDown(crouchKey))
        {
            // Coba slide
            if (grounded && !isSliding && Time.time > lastSlideTime + slideCooldown && currentSpeed >= minSpeedToSlide)
            {
                StartSlide();
            }
            else
            {
                // Kalau gagal slide -> crouch biasa
                if (!isCrouching)
                    SetCrouch();
            }
        }

        if (Input.GetKeyUp(crouchKey))
        {
            // Kalau lagi slide, kita biarkan stop secara alami (timer/speed)
            // Kalau crouch biasa, coba berdiri
            if (!isSliding && isCrouching && CanStandUp())
            {
                SetStand();
            }
        }
    }

    void HandleState()
    {
        if (isSliding)
            return;

        if (isCrouching)
            moveSpeed = crouchSpeed;
        else if (grounded && Input.GetKey(sprintKey))
            moveSpeed = sprintSpeed;
        else
            moveSpeed = walkSpeed;
    }

    void HandleJump()
    {
        if (isSliding) return;

        if (grounded && Input.GetKey(jumpKey))
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

    // ================= SLIDE =================

    void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        lastSlideTime = Time.time;

        SetCrouch();

        // Boost awal
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        Vector3 dir = flatVel.magnitude > 0.1f ? flatVel.normalized : orientation.forward;

        float boostedSpeed = Mathf.Max(flatVel.magnitude, minSpeedToSlide) * slideBoostMultiplier;
        rb.velocity = new Vector3(dir.x * boostedSpeed, rb.velocity.y, dir.z * boostedSpeed);
    }

    void HandleSliding()
    {
        if (!isSliding) return;

        slideTimer -= Time.fixedDeltaTime;

        // Arah current slide (berdasarkan velocity sekarang)
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (flatVel.magnitude < 0.1f)
        {
            StopSlide();
            return;
        }

        Vector3 slideDir = flatVel.normalized;

        // === LIMITED STEERING ===
        // Input kiri/kanan
        float steerInput = horizontalInput; // A/D

        if (Mathf.Abs(steerInput) > 0.01f)
        {
            // Arah kanan player
            Vector3 right = orientation.right;

            // Tambah gaya kecil ke samping (belok berat)
            rb.AddForce(right * steerInput * slideSteerStrength, ForceMode.Force);
        }

        // === BRAKE DENGAN S ===
        if (verticalInput < -0.1f) // tekan S
        {
            rb.AddForce(-slideDir * slideBrakeStrength, ForceMode.Force);
        }

        // Dorongan kecil ke depan biar tetap meluncur
        rb.AddForce(slideDir * slideForce, ForceMode.Force);

        float currentSpeed = flatVel.magnitude;
        bool holdingKey = Input.GetKey(crouchKey);

        // Stop slide kalau:
        // 1) Timer habis & tidak ditahan
        // 2) Speed terlalu kecil
        if ((slideTimer <= 0f && !holdingKey) || currentSpeed < minSpeedToSlide * 0.6f)
        {
            StopSlide();
        }
    }

    void StopSlide()
    {
        isSliding = false;

        if (CanStandUp())
            SetStand();
    }

    // ================= MOVEMENT =================

    void MovePlayer()
    {
        if (isSliding)
            return;

        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
    }

    void SpeedControl()
    {
        if (isSliding) return; // jangan potong speed saat slide

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
        if (isSliding)
            rb.drag = slideDrag; // drag kecil saat slide
        else if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0f;
    }

    // ================= CROUCH / STAND =================

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

    // ================= UI =================

    void UpdateSpeedUI()
    {
        if (speedText == null) return;

        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        speedText.text = "Speed: " + flatVel.magnitude.ToString("F1");
    }
}
