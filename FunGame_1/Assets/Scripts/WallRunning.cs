using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WallRunning : MonoBehaviour
{
    [Header("Wall Running")]
    public LayerMask whatIsWall;
    public LayerMask whatIsGround;
    public float wallRunForce = 200f;
    public float wallRunJumpUpForce = 7f;
    public float wallRunJumpSideForce = 7f;
    public float maxWallRunTime = 1f;
    public float wallClimbSpeed = 3f;

    [Header("Detection")]
    public float wallCheckDistance = 0.7f;
    public float minJumpHeight = 1.5f;
    public float exitWallTime = 0.2f;

    [Header("Input")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode upwardsRunKey = KeyCode.LeftShift;
    public KeyCode downwardsRunKey = KeyCode.LeftControl;

    [Header("Gravity")]
    public bool useGravity = false;
    public float yDrossleSpeed = 5f;

    [Header("References")]
    public Transform orientation;

    private Rigidbody rb;
    private PlayerMovement pm;

    RaycastHit leftWallHit;
    RaycastHit rightWallHit;
    bool wallLeft;
    bool wallRight;

    bool exitingWall;
    float wallRunTimer;
    float exitWallTimer;

    float horizontalInput;
    float verticalInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        CheckForWall();
        StateMachine();
    }

    void FixedUpdate()
    {
        if (pm.wallrunning && !exitingWall)
            WallRunningMovement();
    }

    void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, whatIsWall);
    }

    bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    void StateMachine()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        bool upwardsRunning = Input.GetKey(upwardsRunKey);
        bool downwardsRunning = Input.GetKey(downwardsRunKey);

        // Start / Continue wallrun
        if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround() && !exitingWall)
        {
            if (!pm.wallrunning)
                StartWallRun();

            wallRunTimer -= Time.deltaTime;

            if (wallRunTimer <= 0f)
            {
                exitingWall = true;
                exitWallTimer = exitWallTime;
            }

            // Wall Jump
            if (Input.GetKeyDown(jumpKey))
                WallJump();
        }
        else if (exitingWall)
        {
            pm.restricted = true;

            if (pm.wallrunning)
                StopWallRun();

            exitWallTimer -= Time.deltaTime;
            if (exitWallTimer <= 0f)
                exitingWall = false;
        }
        else
        {
            if (pm.wallrunning)
                StopWallRun();
        }

        if (!exitingWall && pm.restricted)
            pm.restricted = false;
    }

    void StartWallRun()
    {
        pm.wallrunning = true;
        wallRunTimer = maxWallRunTime;
        rb.useGravity = useGravity;

        // nol-kan Y supaya stabil nempel dinding
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
    }

    void WallRunningMovement()
    {
        rb.useGravity = useGravity;

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);

        if ((orientation.forward - wallForward).magnitude > (orientation.forward + wallForward).magnitude)
            wallForward = -wallForward;

        // Dorong sepanjang tembok
        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        // Nempel ke tembok
        rb.AddForce(-wallNormal * 100f, ForceMode.Force);

        // Naik / turun
        if (Input.GetKey(upwardsRunKey))
            rb.velocity = new Vector3(rb.velocity.x, wallClimbSpeed, rb.velocity.z);
        if (Input.GetKey(downwardsRunKey))
            rb.velocity = new Vector3(rb.velocity.x, -wallClimbSpeed, rb.velocity.z);
    }

    void StopWallRun()
    {
        pm.wallrunning = false;
        rb.useGravity = true;
    }

    void WallJump()
    {
        exitingWall = true;
        exitWallTimer = exitWallTime;

        Vector3 wallNormal = wallLeft ? leftWallHit.normal : rightWallHit.normal;
        Vector3 forceToApply = transform.up * wallRunJumpUpForce + wallNormal * wallRunJumpSideForce;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);

        StopWallRun();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, orientation.right * wallCheckDistance);
        Gizmos.DrawRay(transform.position, -orientation.right * wallCheckDistance);
    }
}
