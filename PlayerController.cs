using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //General settings
    public float gravityScale = 2f;
    public float speed = 8f;
    public float jumpForce = 10f;
    public float nearObjectSensitivity = 0.23f;

    //Camera settings
    public Camera playerCamera;
    public bool cameraFollowX = true;
    public bool cameraFollowY = true;
    public bool smoothCamera = true;
    public float cameraDamping = 2f;

    //Double Jump settings
    public bool useDoubleJump = false;
    public int maxDoubleJumps = 2;

    //Wall Climb settings
    public bool useWallClimb = false;
    public float wallSlideVelocity = 0.6f;
    public float wallJumpPushAwayForce = 40f;
    public float wallJumpAwayDuration = 0.1f;

    //Dash settings
    public bool useDash = false;
    public float dashForce = 40f;
    public float dashDuration = 0.15f;
    public bool dashLocksY = false;
    public float dashCooldown = 0.8f;

    //Ground Pounch Settings
    public bool useGroundPound = false;
    public float groundPoundVelocity = 20f;
    public bool groundPoundLocksX = true;
    public float groundPoundCooldown = 0.5f;

    //Local variables
    float horizontalMovement = 0f;
    SpriteRenderer sprite;
    float velocityX = 0f;
    float velocityY = 0f;
    Vector2 velocityBoost;
    float velocityBoostCooldown = 0f;

    Vector3 wallHugCheckLeftPos;
    Vector3 wallHugCheckRightPos;

    bool isFacingRight = true;
    bool isGrounded = false;
    bool wasGrounded = true;
    bool abilitiesLocked = false;
    bool hitHead = false;
    bool killVelocityBoostOnLanding = false;
    bool isWallHugging = false;
    bool isCrouching = false;
    bool dashTriggered = false;
    float dashCooldownClock = 0f;
    bool groundPoundTriggered = false;
    float groundPoundCooldownClock = 0f;

    bool lockVelocityX = false;
    bool lockVelocityY = false;

    bool jumpTriggered = false;

    int doubleJumpsDone = 0;

    new Rigidbody2D rigidbody;
    Collider2D hitBox;
    Transform groundCheck;
    Transform ceilingCheck;
    float cameraX = 0f;
    float cameraY = 0f;

    // Check every collider except Player and Ignore Raycast
    LayerMask layerMask = ~(1 << 2 | 1 << 8);

    // Use this for initialization
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        rigidbody = GetComponent<Rigidbody2D>();
        hitBox = GetComponent<Collider2D>();

        groundCheck = transform.Find("GroundCheck").transform;
        ceilingCheck = transform.Find("CeilingCheck").transform;

        rigidbody.freezeRotation = true;
        rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rigidbody.gravityScale = gravityScale;

        isFacingRight = 0 < transform.localScale.x;
        velocityBoost = new Vector2(0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        // Movement
        horizontalMovement = Input.GetAxisRaw("Horizontal");
        if (horizontalMovement < 0 && isFacingRight)
        {
            FlipCharacter();
        }
        else if (0 < horizontalMovement && !isFacingRight)
        {
            FlipCharacter();
        }

        if (Input.GetButtonDown("Jump") && CanJump())
        {
            jumpTriggered = true;
        }

        if (isGrounded && Input.GetAxisRaw("Vertical") < -0.45f)
        {
            isCrouching = true;
        }
        else if (!isGrounded && Input.GetAxisRaw("Vertical") < 0 && useGroundPound && groundPoundCooldownClock <= 0 && !abilitiesLocked)
        {
            groundPoundTriggered = true;
        }
        else
        {
            isCrouching = false;
        }

        if (Input.GetButtonDown("Fire1") && useDash && dashCooldownClock <= 0 && !abilitiesLocked)
        {
            Debug.Log("Fire");
            dashTriggered = true;
        }
    }

    void FixedUpdate()
    {
        //Vector3 groundCheckPos = colliderBounds.min + new Vector3(colliderBounds.size.x * 0.5f, 0.1f, 0);
        wallHugCheckLeftPos = hitBox.bounds.min + new Vector3(0.1f, hitBox.bounds.size.y * 0.5f, 0);
        wallHugCheckRightPos = hitBox.bounds.max - new Vector3(0.1f, hitBox.bounds.size.y * 0.5f, 0);
        wasGrounded = isGrounded;
        isGrounded = HasContactWithObject(groundCheck.position);
        hitHead = HasContactWithObject(ceilingCheck.position);
        isWallHugging = HasContactWithObject(wallHugCheckLeftPos) || HasContactWithObject(wallHugCheckRightPos);

        if (isGrounded && !wasGrounded)
        {
            OnLanding();
        }

        // Apply movement velocity
        if (hitHead && 0 < rigidbody.velocity.y)
        {
            rigidbody.velocity = new Vector2(horizontalMovement * speed, 0);
        }

        velocityX = 0 < Mathf.Abs(velocityBoost.x) ? velocityBoost.x : (horizontalMovement * speed);
        velocityY = 0 < Mathf.Abs(velocityBoost.y) ? velocityBoost.y : rigidbody.velocity.y;
        rigidbody.velocity = new Vector2(lockVelocityX ? 0 : velocityX, lockVelocityY ? 0 : velocityY);

        //Crouching
        if (isCrouching)
        {
            Debug.Log("Crouching");
            rigidbody.velocity = new Vector2(0, 0);
            hitBox.bounds.Expand(new Vector3(hitBox.bounds.size.x + 100, hitBox.bounds.size.y * 0.5f, 0)); //Not working
        }

        // Wall movement
        if (useWallClimb && isWallHugging && rigidbody.velocity.y <= 0 && (HasContactWithObject(wallHugCheckLeftPos) ? Input.GetAxisRaw("Horizontal") < 0 : 0 < Input.GetAxisRaw("Horizontal")))
        {
            OnLanding();
            rigidbody.velocity = new Vector2(0, -wallSlideVelocity);
        }

        // Jumping
        if (jumpTriggered && !isCrouching)
        {
            if (useWallClimb && isWallHugging)
            {
                horizontalMovement = HasContactWithObject(wallHugCheckLeftPos) ? wallJumpPushAwayForce : -wallJumpPushAwayForce;

                velocityBoostCooldown = wallJumpAwayDuration;
                velocityBoost = new Vector2(horizontalMovement, 0);
            }
            doubleJumpsDone++;
            rigidbody.velocity = new Vector2(rigidbody.velocity.x, jumpForce);
            jumpTriggered = false;
        }

        //Dashing
        if (dashTriggered && !abilitiesLocked)
        {
            abilitiesLocked = true;
            velocityBoostCooldown = dashDuration;

            velocityBoost = new Vector2(isFacingRight ? dashForce : dashForce * -1f, 0f);
            lockVelocityY = dashLocksY;
            dashCooldownClock = dashCooldown;
            dashTriggered = false;
        }

        //Ground Pound
        if (groundPoundTriggered && !abilitiesLocked)
        {
            abilitiesLocked = true;
            killVelocityBoostOnLanding = true;
            velocityBoostCooldown = 3f;

            velocityBoost = new Vector2(0, -groundPoundVelocity);
            lockVelocityX = groundPoundLocksX;
            groundPoundCooldownClock = groundPoundCooldown;
            groundPoundTriggered = false;
        }

        if (playerCamera)
        {
            CameraUpdate();
        }

        AbilityCooldown();

        // Debug lines
        DrawDebugLines(wallHugCheckLeftPos, wallHugCheckRightPos);

        //Velocity Boost
        if (0 < velocityBoostCooldown)
        {
            velocityBoostCooldown -= Time.fixedDeltaTime;
            velocityBoost = 0 < velocityBoostCooldown ? velocityBoost : new Vector2(0, 0);
        }
        else
        {
            velocityBoost = new Vector2(0, 0);
            lockVelocityX = false;
            lockVelocityY = false;
            abilitiesLocked = false;
        }
    }

    private void AbilityCooldown()
    {
        dashCooldownClock = useDash && 0 < dashCooldownClock ? dashCooldownClock - Time.fixedDeltaTime : 0f;
        groundPoundCooldownClock = useGroundPound && 0 < groundPoundCooldownClock ? groundPoundCooldownClock - Time.fixedDeltaTime : 0f;
    }

    private void DrawDebugLines(Vector3 wallHugCheckLeftPos, Vector3 wallHugCheckRightPos)
    {
        Debug.DrawLine(groundCheck.position, groundCheck.position - new Vector3(0, nearObjectSensitivity, 0), isGrounded ? Color.green : Color.red);
        Debug.DrawLine(ceilingCheck.position, ceilingCheck.position + new Vector3(0, nearObjectSensitivity, 0), hitHead ? Color.green : Color.red);
        Debug.DrawLine(wallHugCheckLeftPos, wallHugCheckLeftPos - new Vector3(nearObjectSensitivity, 0, 0), isWallHugging ? Color.green : Color.red);
        Debug.DrawLine(wallHugCheckRightPos, wallHugCheckRightPos + new Vector3(nearObjectSensitivity, 0, 0), isWallHugging ? Color.green : Color.red);
    }

    void CameraUpdate()
    {
        cameraX = smoothCamera ? Mathf.Lerp(playerCamera.transform.position.x, transform.position.x, cameraDamping * Time.fixedDeltaTime) : transform.position.x;
        cameraY = smoothCamera ? Mathf.Lerp(playerCamera.transform.position.y, transform.position.y, cameraDamping * Time.fixedDeltaTime) : transform.position.y;

        playerCamera.transform.position = new Vector3(cameraFollowX ? cameraX : playerCamera.transform.position.x, cameraFollowY ? cameraY : playerCamera.transform.position.y, -1f);
    }

    void FlipCharacter()
    {
        isFacingRight = !isFacingRight;
        sprite.flipX = !sprite.flipX;
    }

    bool HasContactWithObject(Vector3 positionToCheck)
    {
        return Physics2D.OverlapCircle(positionToCheck, nearObjectSensitivity, layerMask);
    }

    void OnLanding()
    {
        if (killVelocityBoostOnLanding)
        {
            velocityBoostCooldown = 0f;
            killVelocityBoostOnLanding = false;
        }

        if (useDoubleJump)
        {
            doubleJumpsDone = 0;
        }
    }

    bool CanJump()
    {
        var returnValue = isGrounded;
        if (useWallClimb)
        {
            returnValue = isGrounded || isWallHugging;
        }
        if (useDoubleJump)
        {
            returnValue = isGrounded ? isGrounded : doubleJumpsDone < maxDoubleJumps;
        }

        return returnValue;
    }

    private void OnBecameInvisible()
    {
        if (playerCamera)
        {
            playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y, -1f);
        }
    }
}