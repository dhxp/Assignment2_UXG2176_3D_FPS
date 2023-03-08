using System;
using UnityEngine;

public class playerMovement : MonoBehaviour
{
    //Assingables
    public Transform playerCam;
    public Transform orientation;

    //Other
    private Rigidbody rb;

    //Rotation and look
    public bool lockLook;
    private float xRotation;
    public float sensitivity = 50f;
    public float sensMultiplier = 1f;
    //Movement
    public float moveSpeed = 4500;
    public float baseSpeed = 20;
    private float startBaseSpeed;
    public bool grounded;
    public LayerMask whatIsGround;

    public float counterMovement = 0.175f;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;

    //Crouch & Slide
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;
    public float slideForce = 400;
    public float slideCounterMovement = 0.2f;
    public float crouchGravityMultiplier;

    //Jumping
    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;
    public float jumpForce = 550f;
    private float desiredX;
    public int startDoubleJumps = 1;
    int doubleJumpsLeft;

    //Input

    Vector3 moveDirection;
    bool jumping, sprinting, crouching;

    //Air contorl
    public float airForwardForce;

    //AirDash
    public float dashForce;
    public float dashTime;
    bool allowDashForceCounter;
    bool readyToDash;
    int dashTappedTimes = 0;
    Vector3 dashStartVector;

    //Sliding
    private Vector3 normalVector = Vector3.up;

    //Climbing
    public float climbForce, maxClimbSpeed;
    public LayerMask whatIsLadder;
    bool alreadyStoppedAtLadder;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startBaseSpeed = baseSpeed;
    }
    void Start()
    {
        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        Movement();
    }
    private void Update()
    {
        MyInput();
        if (!lockLook) Look();
    }

    private void MyInput()
    {

        moveDirection.x = Input.GetAxisRaw("Horizontal");
        moveDirection.y = Input.GetAxisRaw("Vertical");
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1);
        jumping = Input.GetKey(KeyCode.Space);
        crouching = Input.GetKey(KeyCode.LeftShift);

        //Calculating input vector
        //inputVector = new Vector3(x, 0, y);

        //Crouching
        if (Input.GetKeyDown(KeyCode.LeftShift))
            StartCrouch();
        if (Input.GetKeyUp(KeyCode.LeftShift))
            StopCrouch();

        //Double Jumping
        if (Input.GetButtonDown("Jump") && !grounded && doubleJumpsLeft >= 0)
        {
            Jump();
            doubleJumpsLeft--;
        }

        //AirDash
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D))
        && dashTappedTimes <= 1)
        {
            dashTappedTimes++;
            Invoke("ResetTapTimes", 0.3f);
        }
        if (dashTappedTimes == 2 && readyToDash) Dash();

        //Climbing
        if (Physics.Raycast(transform.position, orientation.forward, 1, whatIsLadder) && moveDirection.y > .9f)
        {
            Debug.Log("trying to climb");
            Climb();
        }
        else alreadyStoppedAtLadder = false;
    }

    private void ResetTapTimes()
    {
        dashTappedTimes = 0;
    }

    private void StartCrouch()
    {
        transform.localScale = crouchScale;
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
        if (rb.velocity.magnitude > 0.5f)
        {
            if (grounded)
            {
                rb.AddForce(orientation.transform.forward * slideForce);
            }
        }
    }

    private void StopCrouch()
    {
        transform.localScale = playerScale;
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
    }

    private void Movement()
    {
        Debug.Log("moving");
        float gravityMultiplier = 10f;

        if (crouching) gravityMultiplier = crouchGravityMultiplier;

        rb.AddForce(Vector3.down * Time.deltaTime * gravityMultiplier);

        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        FixMovement(moveDirection.x, moveDirection.y, mag);

        if (readyToJump && jumping && grounded) Jump();

        //ResetStuff when touching ground
        if (grounded)
        {
            readyToDash = true;
            doubleJumpsLeft = startDoubleJumps;
        }

        float maxSpeed = this.baseSpeed;

        if (moveDirection.x > 0 && xMag > maxSpeed) moveDirection.x = 0;
        if (moveDirection.x < 0 && xMag < -maxSpeed) moveDirection.x = 0;
        if (moveDirection.y > 0 && yMag > maxSpeed) moveDirection.y = 0;
        if (moveDirection.y < 0 && yMag < -maxSpeed) moveDirection.y = 0;

        float multiplier = 1f, multiplierV = 1f;

        // Movement in air
        if (!grounded)
        {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }
        if (grounded && crouching) multiplierV = 0f;

        //Apply forces to move player
        rb.AddForce(orientation.transform.forward * moveDirection.y * moveSpeed * 10 * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * moveDirection.x * moveSpeed * 10 * Time.deltaTime * multiplier);

    }

    private void Jump()
    {
        if (grounded)
        {
            readyToJump = false;

            //Add jump forces
            rb.AddForce(Vector3.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);

            //If jumping while falling, reset y velocity.
            Vector3 vel = rb.velocity;
            if (rb.velocity.y < 0.5f)
                rb.velocity = new Vector3(vel.x, 0, vel.z);
            else if (rb.velocity.y > 0)
                rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);

            Invoke(nameof(ResetJump), jumpCooldown);
        }
        if (!grounded)
        {
            readyToJump = false;

            //Add jump forces
            rb.AddForce(orientation.forward * jumpForce * 1f);
            rb.AddForce(Vector2.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);

            //Reset Velocity
            rb.velocity = Vector3.zero;

            //Disable dashForceCounter if doublejumping while dashing
            allowDashForceCounter = false;

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void Dash()
    {
        //our velocity
        dashStartVector = orientation.forward;

        allowDashForceCounter = true;

        readyToDash = false;
        dashTappedTimes = 0;

        //Deactivate gravity
        rb.useGravity = false;

        //Add force
        rb.velocity = Vector3.zero;
        rb.AddForce(orientation.forward * dashForce);

        Invoke("ActivateGravity", dashTime);
    }
    private void ActivateGravity()
    {
        rb.useGravity = true;

        //Counter currentForce
        if (allowDashForceCounter)
        {
            rb.AddForce(dashStartVector * -dashForce * 0.5f);
        }
    }

    private void Climb()
    {
        Vector3 vel = rb.velocity;
        if (rb.velocity.y < 0.5f && !alreadyStoppedAtLadder)
        {
            rb.velocity = new Vector3(vel.x, 0, vel.z);
            alreadyStoppedAtLadder = true;
            rb.AddForce(orientation.forward * 500 * Time.deltaTime);
        }


        if (rb.velocity.magnitude < maxClimbSpeed)
        {
            rb.AddForce(orientation.up * climbForce*10 * Time.deltaTime);
        }

        if (!Input.GetKey(KeyCode.S)) moveDirection.y = 0;
    }


    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * -sensitivity * Time.fixedDeltaTime * sensMultiplier;

        //Find current look rotation
        Vector3 rot = playerCam.transform.localRotation.eulerAngles;
        desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Perform the rotations
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);
        orientation.transform.localRotation = Quaternion.Euler(0, desiredX, 0);
    }
    private void FixMovement(float x, float y, Vector2 mag)
    {
        if (!grounded) return;

        //Slow down sliding
        if (crouching)
        {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * slideCounterMovement);
            return;
        }
        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }

        if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > baseSpeed)
        {
            float fallspeed = rb.velocity.y;
            Vector3 n = rb.velocity.normalized * baseSpeed;
            rb.velocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }

    private bool cancellingGrounded;

    // Handle ground detection
    private void OnCollisionStay(Collision other)
    {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            //FLOOR
            if (IsFloor(normal))
            {
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
        }

        float delay = 3f;
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    private void StopGrounded()
    {
        grounded = false;
    }

}
