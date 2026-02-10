using UnityEngine;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float groundDrag = 5f;

    [Header("Jumping")]
    public float jumpForce = 5f;
    public float airMultiplier = 0.4f;
    bool readyToJump = true;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    public Transform groundCheck;          // empty object di bawah kaki
    public float groundDistance = 0.35f;
    public LayerMask whatIsGround;
    bool grounded;
    bool wasGrounded;

    // Jump buffer / hold-to-jump
    bool jumpHeld;       // true kalau Space sedang ditekan
    bool jumpRequested;  // buffer permintaan lompat

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;
    Rigidbody rb;

    [HideInInspector] public TextMeshProUGUI text_speed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // cegah miring karena tabrakan
    }

    private void Update()
    {
        // Ground check
        grounded = Physics.CheckSphere(groundCheck.position, groundDistance, whatIsGround);

        // Input dasar
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Apakah tombol lompat sedang ditahan
        jumpHeld = Input.GetKey(jumpKey);

        // Simpan request lompat (buffer)
        if (Input.GetKeyDown(jumpKey) || jumpHeld)
        {
            jumpRequested = true;
        }

        // Deteksi mendarat (udara -> tanah)
        if (grounded && !wasGrounded)
        {
            // Nol-kan Y velocity supaya tidak ada sisa momentum pantulan
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            readyToJump = true;
        }

        // Eksekusi lompat jika memungkinkan
        if (jumpRequested && grounded && readyToJump)
        {
            DoJump();
            jumpRequested = false;
        }

        wasGrounded = grounded;

        SpeedControl();

        // handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0f;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }

        if (text_speed != null)
            text_speed.SetText("Speed: " + flatVel.magnitude.ToString("F1"));
    }

    private void DoJump()
    {
        readyToJump = false;

        // reset y velocity (biar tinggi lompatan konsisten)
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
}
