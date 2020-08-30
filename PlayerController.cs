using System;
using UnityEngine;

public enum MovementState
{
    Idle,
    Running,
    Jumping,
    Falling,
    WallHugging,
    Dashing,
    GroundPounding,
    Crouching
}

public class CharacterModel
{
    public MovementState MovementState { get; set; }
    public float HorizontalMovement = 0f;
    public float VelocityX = 0f;
    public float VelocityY = 0f;
    public Vector2 VelocityBoost;
    public float VelocityBoostCooldown = 0f;
    public Vector3 WallHugCheckLeftPos;
    public Vector3 WallHugCheckRightPos;
    public bool IsFacingRight = true;
    public bool IsGrounded = false;
    public bool WasGrounded = true;
    public bool AbilitiesLocked = false;
    public bool HitHead = false;
    public bool KillVelocityBoostOnHittingWall = false;
    public bool KillVelocityBoostOnLanding = false;
    public bool IsWallHugging = false;
    public bool IsCrouching = false;
    public float DashCooldownClock = 0f;
    public float GroundPoundCooldownClock = 0f;
    public bool LockVelocityX = false;
    public bool LockVelocityY = false;
    public int DoubleJumpsDone = 0;
    public Transform GroundCheck;
    public Transform CeilingCheck;
}

public class TriggerModel
{
    public bool Jump = false;
    public bool Dash = false;
    public bool GroundPound = false;
}

public class CameraModel
{
    public float CameraX = 0f;
    public float CameraY = 0f;
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    //General settings
    public float gravityScale = 2f;
    public float speed = 8f;
    public float jumpForce = 10f;
    public float nearObjectSensitivity = 0.23f;

    //Camera settings
    [Serializable]
    protected class CameraSettings
    {
        public Camera PlayerCamera;
        public bool CameraFollowX = true;
        public bool CameraFollowY = true;
        public bool SmoothCamera = false;
        public float CameraDamping = 2f;
    }

    //Double Jump settings
    [Serializable]
    protected class DoubleJumpSettings
    {
        public bool UseDoubleJump = false;
        public int MaxDoubleJumps = 2;
    }

    //Wall Climb settings
    [Serializable]
    protected class WallClimbSettings
    {
        public bool UseWallClimb = false;
        public float WallSlideVelocity = 0.6f;
        public float WallJumpPushAwayForce = 5f;
        public float WallJumpAwayDuration = 0.1f;
    }

    //Dash settings
    [Serializable]
    protected class DashSettings
    {
        public bool UseDash = false;
        public float DashForce = 40f;
        public float DashDuration = 0.15f;
        public bool DashLocksY = true;
        public float DashCooldown = 0.8f;
    }

    //Ground Pounch Settings
    [Serializable]
    protected class GroundPoundSettings
    {
        public bool UseGroundPound = false;
        public float GroundPoundVelocity = 20f;
        public bool GroundPoundLocksX = true;
        public float GroundPoundCooldown = 0.5f;
    }

    //Local variables
    private CharacterModel _character;
    private CameraModel _camera;
    private TriggerModel _trigger;

    [SerializeField] private CameraSettings _cameraSettings = null;
    [SerializeField] private DoubleJumpSettings _doubleJumpSettings = null;
    [SerializeField] private WallClimbSettings _wallClimbSettings = null;
    [SerializeField] private DashSettings _dashSettings = null;
    [SerializeField] private GroundPoundSettings _groundPoundSettings = null;

    private SpriteRenderer sprite;
    private Animator animator;
    private new Rigidbody2D rigidbody;
    private Collider2D hitBox;

    // Check every collider except Player and Ignore Raycast
    LayerMask layerMask = ~(1 << 2 | 1 << 8);

    private void Awake()
    {
        _character = new CharacterModel
        {
            //If you want to set start variables that differ from model's initial values, do it here.
        };

        _camera = new CameraModel
        {
            //Same goes for this one etc. etc.
        };

        _doubleJumpSettings = new DoubleJumpSettings() { UseDoubleJump = true };
        _wallClimbSettings = new WallClimbSettings() { UseWallClimb = true };
        _dashSettings = new DashSettings() { UseDash = true };
        _groundPoundSettings = new GroundPoundSettings() { UseGroundPound = true };
        _trigger = new TriggerModel();
    }

    // Use this for initialization
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rigidbody = GetComponent<Rigidbody2D>();
        hitBox = GetComponent<Collider2D>();

        var groundCheckName = "GroundCheck";
        var ceilingCheckName = "CeilingCheck";
        AssertVitalComponents(groundCheckName, ceilingCheckName);

        _character.GroundCheck = transform.Find(groundCheckName).transform;
        _character.CeilingCheck = transform.Find(ceilingCheckName).transform;

        rigidbody.freezeRotation = true;
        rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rigidbody.gravityScale = gravityScale;

        _character.IsFacingRight = 0 < transform.localScale.x;
        _character.VelocityBoost = Vector2.zero;
    }

    // Update is called once per frame
    void Update()
    {
        // Movement
        _character.HorizontalMovement = Input.GetAxisRaw("Horizontal");
        if (_character.HorizontalMovement < 0 && _character.IsFacingRight)
        {
            FlipCharacter();
        }
        else if (0 < _character.HorizontalMovement && !_character.IsFacingRight)
        {
            FlipCharacter();
        }

        if (Input.GetButtonDown("Jump") && CanJump())
        {
            _trigger.Jump = true;
        }

        if (_character.IsGrounded && Input.GetAxisRaw("Vertical") < -0.45f)
        {
            _character.IsCrouching = true;
        }
        else if (!_character.IsGrounded && Input.GetAxisRaw("Vertical") < 0 && _groundPoundSettings.UseGroundPound && _character.GroundPoundCooldownClock <= 0 && !_character.AbilitiesLocked)
        {
            _trigger.GroundPound = true;
        }
        else
        {
            _character.IsCrouching = false;
        }

        if (Input.GetButtonDown("Fire1") && _dashSettings.UseDash && _character.DashCooldownClock <= 0 && !_character.AbilitiesLocked)
        {
            _trigger.Dash = true;
        }

        //Optional: Animation
        SetAnimation(_character.MovementState);
    }

    void FixedUpdate()
    {
        //Debug.Log(_character.MovementState.ToString());
        SetCurrentStateVariables();

        if (_character.IsGrounded && !_character.WasGrounded)
        {
            OnLanding();
        }

        // Apply movement velocity
        HandleMovement();

        //Crouching
        if (_character.IsCrouching)
        {
            HandleCrouching();
        }

        // Wall movement
        if (_wallClimbSettings.UseWallClimb && _character.IsWallHugging && rigidbody.velocity.y <= 0 && (HasContactWithObject(_character.WallHugCheckLeftPos) ? Input.GetAxisRaw("Horizontal") < 0 : 0 < Input.GetAxisRaw("Horizontal")))
        {
            HandleWallMovement();
        }

        // Jumping
        if (_trigger.Jump && !_character.IsCrouching && !_character.AbilitiesLocked)
        {
            HandleJumping();
        }

        //Dashing
        if (_trigger.Dash && !_character.AbilitiesLocked)
        {
            HandleDash();
        }

        //Ground Pound
        if (_trigger.GroundPound && !_character.AbilitiesLocked)
        {
            HandleGroundPound();
        }

        if (_cameraSettings.PlayerCamera)
        {
            CameraUpdate();
        }

        //Cooldowns
        AbilityCooldown();

        // Debug lines
        DrawDebugLines(_character.WallHugCheckLeftPos, _character.WallHugCheckRightPos);

        //Velocity Boost
        HandleVelocityBoost();
    }

    void SetCurrentStateVariables()
    {
        _character.WallHugCheckLeftPos = hitBox.bounds.min + new Vector3(0.1f, hitBox.bounds.size.y * 0.5f, 0);
        _character.WallHugCheckRightPos = hitBox.bounds.max - new Vector3(0.1f, hitBox.bounds.size.y * 0.5f, 0);
        _character.WasGrounded = _character.IsGrounded;
        _character.IsGrounded = HasContactWithObject(_character.GroundCheck.position);
        _character.HitHead = HasContactWithObject(_character.CeilingCheck.position);
        _character.IsWallHugging = HasContactWithObject(_character.WallHugCheckLeftPos) || HasContactWithObject(_character.WallHugCheckRightPos);
    }

    void HandleMovement()
    {
        if (_character.HitHead && 0 < rigidbody.velocity.y)
        {
            rigidbody.velocity = new Vector2(_character.HorizontalMovement * speed, 0);
        }

        _character.VelocityX = 0 < Mathf.Abs(_character.VelocityBoost.x) ? _character.VelocityBoost.x : (_character.HorizontalMovement * speed);
        _character.VelocityY = 0 < Mathf.Abs(_character.VelocityBoost.y) ? _character.VelocityBoost.y : rigidbody.velocity.y;
        rigidbody.velocity = new Vector2(_character.LockVelocityX ? 0 : _character.VelocityX, _character.LockVelocityY ? 0 : _character.VelocityY);

        if (0 < Mathf.Abs(_character.VelocityBoost.x) && rigidbody.velocity.x == 0 && _character.KillVelocityBoostOnHittingWall)
        {
            _character.KillVelocityBoostOnHittingWall = false;
            _character.VelocityBoostCooldown = 0f;
            HandleVelocityBoost();
        }

        SetMovementState();
    }

    private void SetMovementState()
    {
        if (_character.IsGrounded && rigidbody.velocity.x == 0)
        {
            _character.MovementState = MovementState.Idle;
        }
        if (_character.IsGrounded && 0 < Mathf.Abs(rigidbody.velocity.x) && !_character.AbilitiesLocked)
        {
            _character.MovementState = MovementState.Running;
        }
        if (!_character.IsGrounded && rigidbody.velocity.y < 0 && !_character.AbilitiesLocked)
        {
            _character.MovementState = MovementState.Falling;
        }
        else if (!_character.IsGrounded && 0 < rigidbody.velocity.y)
        {
            _character.MovementState = MovementState.Jumping;
        }
    }

    void HandleCrouching()
    {
        _character.MovementState = MovementState.Crouching;
        rigidbody.velocity = Vector2.zero;
        hitBox.bounds.Expand(new Vector3(hitBox.bounds.size.x + 100, hitBox.bounds.size.y * 0.5f, 0)); //Not working
    }

    void HandleWallMovement()
    {
        _character.MovementState = MovementState.WallHugging;
        OnLanding();
        rigidbody.velocity = new Vector2(0, -_wallClimbSettings.WallSlideVelocity);
    }

    void HandleJumping()
    {
        if (_wallClimbSettings.UseWallClimb && _character.IsWallHugging)
        {
            _character.HorizontalMovement = HasContactWithObject(_character.WallHugCheckLeftPos) ? _wallClimbSettings.WallJumpPushAwayForce : -_wallClimbSettings.WallJumpPushAwayForce;

            _character.VelocityBoostCooldown = _wallClimbSettings.WallJumpAwayDuration;
            _character.VelocityBoost = new Vector2(_character.HorizontalMovement, 0);
        }
        _character.DoubleJumpsDone++;
        rigidbody.velocity = new Vector2(rigidbody.velocity.x, jumpForce);
        _trigger.Jump = false;
    }

    void HandleDash()
    {
        _character.MovementState = MovementState.Dashing;

        _character.AbilitiesLocked = true;
        _character.KillVelocityBoostOnHittingWall = true;
        _character.VelocityBoostCooldown = _dashSettings.DashDuration;

        _character.VelocityBoost = new Vector2(_character.IsFacingRight ? _dashSettings.DashForce : _dashSettings.DashForce * -1f, 0f);
        _character.LockVelocityY = _dashSettings.DashLocksY;
        _character.DashCooldownClock = _dashSettings.DashCooldown;
        _trigger.Dash = false;
    }

    void HandleGroundPound()
    {
        _character.MovementState = MovementState.GroundPounding;

        _character.AbilitiesLocked = true;
        _character.KillVelocityBoostOnLanding = true;
        _character.VelocityBoostCooldown = 3f;

        _character.VelocityBoost = new Vector2(0, -_groundPoundSettings.GroundPoundVelocity);
        _character.LockVelocityX = _groundPoundSettings.GroundPoundLocksX;
        _character.GroundPoundCooldownClock = _groundPoundSettings.GroundPoundCooldown;
        _trigger.GroundPound = false;
    }

    void AbilityCooldown()
    {
        _character.DashCooldownClock = _dashSettings.UseDash && 0 < _character.DashCooldownClock ? _character.DashCooldownClock - Time.fixedDeltaTime : 0f;
        _character.GroundPoundCooldownClock = _groundPoundSettings.UseGroundPound && 0 < _character.GroundPoundCooldownClock ? _character.GroundPoundCooldownClock - Time.fixedDeltaTime : 0f;
    }

    void DrawDebugLines(Vector3 wallHugCheckLeftPos, Vector3 wallHugCheckRightPos)
    {
        Debug.DrawLine(_character.GroundCheck.position, _character.GroundCheck.position - new Vector3(0, nearObjectSensitivity, 0), _character.IsGrounded ? Color.green : Color.red);
        Debug.DrawLine(_character.CeilingCheck.position, _character.CeilingCheck.position + new Vector3(0, nearObjectSensitivity, 0), _character.HitHead ? Color.green : Color.red);
        Debug.DrawLine(wallHugCheckLeftPos, wallHugCheckLeftPos - new Vector3(nearObjectSensitivity, 0, 0), _character.IsWallHugging ? Color.green : Color.red);
        Debug.DrawLine(wallHugCheckRightPos, wallHugCheckRightPos + new Vector3(nearObjectSensitivity, 0, 0), _character.IsWallHugging ? Color.green : Color.red);
    }

    void HandleVelocityBoost()
    {
        if (0 < _character.VelocityBoostCooldown)
        {
            _character.VelocityBoostCooldown -= Time.fixedDeltaTime;
            _character.VelocityBoost = 0 < _character.VelocityBoostCooldown ? _character.VelocityBoost : Vector2.zero;
        }
        else
        {
            _character.VelocityBoost = Vector2.zero;
            _character.LockVelocityX = false;
            _character.LockVelocityY = false;
            _character.AbilitiesLocked = false;
        }
    }

    void CameraUpdate()
    {
        _camera.CameraX = _cameraSettings.SmoothCamera ? Mathf.Lerp(_cameraSettings.PlayerCamera.transform.position.x, transform.position.x, _cameraSettings.CameraDamping * Time.fixedDeltaTime) : transform.position.x;
        _camera.CameraY = _cameraSettings.SmoothCamera ? Mathf.Lerp(_cameraSettings.PlayerCamera.transform.position.y, transform.position.y, _cameraSettings.CameraDamping * Time.fixedDeltaTime) : transform.position.y;

        _cameraSettings.PlayerCamera.transform.position = new Vector3(_cameraSettings.CameraFollowX ? _camera.CameraX : _cameraSettings.PlayerCamera.transform.position.x, _cameraSettings.CameraFollowY ? _camera.CameraY : _cameraSettings.PlayerCamera.transform.position.y, -1f);
    }

    void FlipCharacter()
    {
        _character.IsFacingRight = !_character.IsFacingRight;
        sprite.flipX = !sprite.flipX;
    }

    bool HasContactWithObject(Vector3 positionToCheck)
    {
        return Physics2D.OverlapCircle(positionToCheck, nearObjectSensitivity, layerMask);
    }

    void OnLanding()
    {
        if (_character.KillVelocityBoostOnLanding)
        {
            _character.VelocityBoostCooldown = 0f;
            _character.KillVelocityBoostOnLanding = false;
            _character.GroundPoundCooldownClock = 0f;
        }

        if (_doubleJumpSettings.UseDoubleJump)
        {
            _character.DoubleJumpsDone = 0;
        }
    }

    bool CanJump()
    {
        var returnValue = _character.IsGrounded;
        if (_wallClimbSettings.UseWallClimb)
        {
            returnValue = _character.IsGrounded || _character.IsWallHugging;
        }
        if (_doubleJumpSettings.UseDoubleJump)
        {
            returnValue = _character.IsGrounded ? _character.IsGrounded : _character.DoubleJumpsDone < _doubleJumpSettings.MaxDoubleJumps;
        }

        return returnValue;
    }

    private void SetAnimation(MovementState newState)
    {
        if (animator != null)
        {
            animator.SetTrigger(newState.ToString());
        }
    }

    private void AssertVitalComponents(string groundCheckName, string ceilingCheckName)
    {
        if (_cameraSettings.PlayerCamera == null)
        {
            Destroy(this);
            throw new MissingComponentException("You have not assigned a camera in PlayerController's Camera Settings section.");
        }
        if (transform.Find(groundCheckName) == null)
        {
            Destroy(this);
            throw new MissingComponentException($"You have not assigned a GameObject called \"{groundCheckName}\" as a child to {gameObject.name}. Place its Transform where the character meets the ground when grounded.");
        }
        if (transform.Find(ceilingCheckName) == null)
        {
            Destroy(this);
            throw new MissingComponentException($"You have not assigned a GameObject called \"{ceilingCheckName}\" as a child to {gameObject.name}. Place its Transform where the character would hit the ceiling when jumping upward.");
        }

        if (animator == null)
        {
            Debug.LogWarning("No Animator found on object.");
        }
    }

    private void OnBecameInvisible()
    {
        if (_cameraSettings.PlayerCamera)
        {
            _cameraSettings.PlayerCamera.transform.position = new Vector3(transform.position.x, transform.position.y, -1f);
        }
    }
}