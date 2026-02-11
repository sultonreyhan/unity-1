using UnityEngine;

public class PlayerSlide : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    private Rigidbody rb;
    private PlayerMovement pm;

    [Header("Slide Settings")]
    public float slideForce = 20f;
    public float slideDuration = 0.75f;
    public float slideCooldown = 1.0f;
    public float maxSlideSpeed = 12f;
    public float steeringMultiplier = 0.3f; // seberapa kuat belok saat slide (0 = lurus, 1 = normal)

    [Header("Input")]
    public KeyCode slideKey = KeyCode.LeftControl;

    float slideTimer;
    float cooldownTimer;

    bool sliding;

    Vector3 slideDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (cooldownTimer > 0)
            cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(slideKey))
            TryStartSlide();
    }

    void FixedUpdate()
    {
        if (sliding)
            SlidingMovement();
    }

    void TryStartSlide()
    {
        if (sliding) return;
        if (cooldownTimer > 0) return;
        if (!pm.grounded) return;

        // hanya bisa slide kalau lagi lari (speed cukup)
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (flatVel.magnitude < pm.sprintSpeed - 0.5f)
            return;

        StartSlide();
    }

    void StartSlide()
    {
        sliding = true;
        slideTimer = slideDuration;

        // kunci arah awal slide
        slideDirection = orientation.forward;

        // kasih dorongan awal
        rb.AddForce(slideDirection * slideForce, ForceMode.Impulse);
    }

    void SlidingMovement()
    {
        slideTimer -= Time.fixedDeltaTime;

        // ambil input belok sedikit
        float h = Input.GetAxisRaw("Horizontal");
        Vector3 steerDir = (orientation.forward + orientation.right * h * steeringMultiplier).normalized;

        // dorong ke arah slide + sedikit steering
        rb.AddForce(steerDir * slideForce, ForceMode.Force);

        // batasi kecepatan maksimum saat slide
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (flatVel.magnitude > maxSlideSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * maxSlideSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }

        if (slideTimer <= 0f)
            StopSlide();
    }

    void StopSlide()
    {
        sliding = false;
        cooldownTimer = slideCooldown;
    }
}
