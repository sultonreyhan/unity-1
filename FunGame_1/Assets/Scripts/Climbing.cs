using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Climbing : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public PlayerMovement pm;
    public LedgeGrabbing lg;
    public LayerMask whatIsWall;

    private Rigidbody rb;

    [Header("Climbing")]
    public float climbSpeed = 3f;
    public float maxClimbTime = 2f;
    float climbTimer;

    bool climbing;

    [Header("Climb Jumping")]
    public float climbJumpUpForce = 6f;
    public float climbJumpBackForce = 4f;
    public KeyCode jumpKey = KeyCode.Space;
    public int climbJumps = 1;
    int climbJumpsLeft;

    [Header("Detection")]
    public float detectionLength = 1f;
    public float sphereCastRadius = 0.3f;
    public float maxWallLookAngle = 60f;

    RaycastHit frontWallHit;
    bool wallFront;

    [Header("Exiting")]
    public bool exitingWall;
    public float exitWallTime = 0.2f;
    float exitWallTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (lg == null) lg = GetComponent<LedgeGrabbing>();
    }

    void Update()
    {
        WallCheck();
        StateMachine();

        if (climbing && !exitingWall)
            ClimbingMovement();
    }

    void WallCheck()
    {
        wallFront = Physics.SphereCast(transform.position, sphereCastRadius, orientation.forward, out frontWallHit, detectionLength, whatIsWall);

        if (wallFront || pm.grounded)
        {
            climbTimer = maxClimbTime;
            climbJumpsLeft = climbJumps;
        }
    }

    void StateMachine()
    {
        if (lg != null && lg.holding)
        {
            if (climbing) StopClimbing();
            return;
        }

        float lookAngle = wallFront ? Vector3.Angle(orientation.forward, -frontWallHit.normal) : 999f;

        if (wallFront && Input.GetKey(KeyCode.W) && lookAngle < maxWallLookAngle && !exitingWall)
        {
            if (!climbing && climbTimer > 0)
                StartClimbing();

            climbTimer -= Time.deltaTime;
            if (climbTimer <= 0)
                StopClimbing();
        }
        else if (exitingWall)
        {
            if (climbing) StopClimbing();

            exitWallTimer -= Time.deltaTime;
            if (exitWallTimer <= 0)
                exitingWall = false;
        }
        else
        {
            if (climbing) StopClimbing();
        }

        if (wallFront && Input.GetKeyDown(jumpKey) && climbJumpsLeft > 0)
            ClimbJump();
    }

    void StartClimbing()
    {
        climbing = true;
        pm.climbing = true;

        rb.useGravity = false;
    }

    void ClimbingMovement()
    {
        rb.velocity = new Vector3(rb.velocity.x, climbSpeed, rb.velocity.z);
    }

    void StopClimbing()
    {
        climbing = false;
        pm.climbing = false;

        rb.useGravity = true;
    }

    void ClimbJump()
    {
        if (pm.grounded) return;
        if (lg != null && (lg.holding || lg.exitingLedge)) return;

        exitingWall = true;
        exitWallTimer = exitWallTime;

        Vector3 force = Vector3.up * climbJumpUpForce + frontWallHit.normal * climbJumpBackForce;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(force, ForceMode.Impulse);

        climbJumpsLeft--;
    }
}
