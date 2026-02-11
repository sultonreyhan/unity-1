using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LedgeGrabbing : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement pm;
    public Transform orientation;
    public Transform cam;

    private Rigidbody rb;

    [Header("Ledge Grabbing")]
    public float moveToLedgeSpeed = 5f;
    public float maxLedgeGrabDistance = 1.2f;
    public float minTimeOnLedge = 0.2f;

    float timeOnLedge;
    public bool holding;

    [Header("Ledge Jumping")]
    public KeyCode jumpKey = KeyCode.Space;
    public float ledgeJumpForwardForce = 5f;
    public float ledgeJumpUpwardForce = 6f;

    [Header("Ledge Detection")]
    public float ledgeDetectionLength = 1.5f;
    public float ledgeSphereCastRadius = 0.3f;
    public LayerMask whatIsLedge;

    RaycastHit ledgeHit;
    Transform lastLedge;
    Transform currLedge;

    [Header("Exiting")]
    public bool exitingLedge;
    public float exitLedgeTime = 0.2f;
    float exitLedgeTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        LedgeDetection();
        SubStateMachine();
    }

    void SubStateMachine()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool anyInput = h != 0 || v != 0;

        if (holding)
        {
            FreezeRigidbodyOnLedge();
            timeOnLedge += Time.deltaTime;

            if (timeOnLedge > minTimeOnLedge && anyInput)
                ExitLedgeHold();

            if (Input.GetKeyDown(jumpKey))
                LedgeJump();
        }
        else if (exitingLedge)
        {
            exitLedgeTimer -= Time.deltaTime;
            if (exitLedgeTimer <= 0)
                exitingLedge = false;
        }
    }

    void LedgeDetection()
    {
        bool ledgeDetected = Physics.SphereCast(transform.position, ledgeSphereCastRadius, cam.forward, out ledgeHit, ledgeDetectionLength, whatIsLedge);

        if (!ledgeDetected) return;

        float dist = Vector3.Distance(transform.position, ledgeHit.point);

        if (ledgeHit.transform == lastLedge) return;

        if (dist < maxLedgeGrabDistance && !holding && !exitingLedge)
            EnterLedgeHold();
    }

    void EnterLedgeHold()
    {
        holding = true;
        pm.holdingLedge = true;
        pm.restricted = true;

        currLedge = ledgeHit.transform;
        lastLedge = ledgeHit.transform;

        rb.useGravity = false;
        rb.velocity = Vector3.zero;
    }

    void FreezeRigidbodyOnLedge()
    {
        rb.useGravity = false;

        Vector3 dir = currLedge.position - transform.position;
        float dist = dir.magnitude;

        if (dist > 1f)
        {
            rb.AddForce(dir.normalized * moveToLedgeSpeed * 50f * Time.deltaTime, ForceMode.VelocityChange);
        }
    }

    void ExitLedgeHold()
    {
        exitingLedge = true;
        exitLedgeTimer = exitLedgeTime;

        holding = false;
        pm.holdingLedge = false;
        pm.restricted = false;

        timeOnLedge = 0f;
        rb.useGravity = true;

        Invoke(nameof(ResetLastLedge), 1f);
    }

    void ResetLastLedge()
    {
        lastLedge = null;
    }

    void LedgeJump()
    {
        ExitLedgeHold();
        Invoke(nameof(DelayedJumpForce), 0.05f);
    }

    void DelayedJumpForce()
    {
        Vector3 force = cam.forward * ledgeJumpForwardForce + Vector3.up * ledgeJumpUpwardForce;
        rb.velocity = Vector3.zero;
        rb.AddForce(force, ForceMode.Impulse);
    }
}
