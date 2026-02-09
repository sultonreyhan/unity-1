using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;

    public float groundDrag;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump = true;

    [HideInInspector] public float walkSpeed;
    [HideInInspector] public float sprintSpeed;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    // small radius used for a secondary ground check at the feet
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float groundCheckOriginOffset = 0.1f;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerMovement requires a Rigidbody on the same GameObject.");
            enabled = false;
            return;
        }

        rb.freezeRotation = true;

        readyToJump = true;
    }

    private void Update()
    {
        // improved ground check: cast from a small offset above transform, plus a sphere check at the feet
        float rayLength = playerHeight * 0.5f + 0.3f;
        Vector3 rayOrigin = transform.position + Vector3.up * groundCheckOriginOffset;
        bool rayHit = Physics.Raycast(rayOrigin, Vector3.down, rayLength + groundCheckOriginOffset, whatIsGround);

        Vector3 feetPosition = transform.position + Vector3.down * (playerHeight * 0.5f);
        bool sphereHit = Physics.CheckSphere(feetPosition, groundCheckRadius, whatIsGround);

        grounded = rayHit || sphereHit;

        // draw the ray and feet sphere so you can see it in the Scene view
        Debug.DrawRay(rayOrigin, Vector3.down * (rayLength + groundCheckOriginOffset), grounded ? Color.green : Color.red);
        Debug.DrawLine(feetPosition, feetPosition + Vector3.up * 0.01f, grounded ? Color.green : Color.red);

        MyInput();
        SpeedControl();

        // handle drag
        rb.drag = grounded ? groundDrag : 0f;

        // quick runtime info to Console (remove when done)
        if (Input.GetKeyDown(KeyCode.F1)) // press F1 in Play mode to dump state once
        {
            Debug.Log($"grounded={grounded}, rayHit={rayHit}, sphereHit={sphereHit}, readyToJump={readyToJump}, rb.isKinematic={rb.isKinematic}, jumpForce={jumpForce}, rayLength={rayLength}");
            Debug.Log($"rb.constraints={rb.constraints}, rb.velocity={rb.velocity}");
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // when to jump: use GetKeyDown so one press triggers a single jump
        if (Input.GetKeyDown(jumpKey))
        {
            if (!readyToJump)
            {
                Debug.Log("Jump pressed but not readyToJump.");
                return;
            }

            if (!grounded)
            {
                Debug.Log("Jump pressed but player not grounded.");
                return;
            }

            if (jumpForce <= 0f)
            {
                Debug.LogWarning("jumpForce is <= 0. Set jumpForce to a positive value to enable jumping.");
                return;
            }

            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // on ground
        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        // in air
        else
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // limit velocity if needed
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // use world up to avoid accidental local-rotation issues
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    // visualize the feet check sphere in the editor
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        Gizmos.color = grounded ? Color.green : Color.red;
        Vector3 feetPosition = transform.position + Vector3.down * (playerHeight * 0.5f);
        Gizmos.DrawWireSphere(feetPosition, groundCheckRadius);
    }
}