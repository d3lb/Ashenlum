using System.Collections;
using UnityEngine;
using static PlayerState;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerPhysicsData physicsData;

    #region VARIABLES 

    //Components
    public Rigidbody2D rb { get; private set; }
    private SpriteRenderer Sprite;

    //States
    [SerializeField] private PlayerState state;
    private int facingDir;

    //Player Input
    private PlayerInput input;

    //Timers
    public float LastOnGroundTime { get; private set; }
    public float LastOnWallTime { get; private set; }
    public float LastOnWallRightTime { get; private set; }
    public float LastOnWallLeftTime { get; private set; }
    public float LastPressedJumpTime { get; private set; }
    public float LastPressedDashTime { get; private set; }

    //Ground
    private bool wasGroundedLastFrame;

    // Spaced by distance, not time - a timer ticks the same whether creeping or sprinting.
    [Header("Footsteps")]
    [SerializeField] private float stepDistance = 2.2f;
    private float stepTravelled;

    //Jump
    private int jumpNumber;
    public int JumpNumber => jumpNumber;

    //Wall Jump
    private int lastWallJumpDir;
    private float wallJumpTimer;
    private float wallJumpReturnTimer;
    private bool usedWallReturnAssist;
    private float wallJumpRegrabTimer;
    private int wallDir; // -1 = wall on left, 1 = wall on right

    //Dash
    private int dashesLeft;
    private float lastDashTime = -999f;

    // A fast tap releases before the jump applies, so the release is remembered.
    private bool jumpCutQueued;

    // physicsData is shared; a purchase must not write to it.
    private int MaxDashes => physicsData.dashAmount +
        (GameManager.Instance != null ? GameManager.Instance.activeRun.bonusDashes : 0);
    private float dashRefillTimer;
    private Vector2 lastDashDir;

    // For the HUD pips.
    public int DashCharges => dashesLeft;
    public int DashChargesMax => MaxDashes;

    public float DashRefillPercent =>
        dashesLeft >= MaxDashes || physicsData.dashRefillTime <= 0f
            ? 0f
            : Mathf.Clamp01(dashRefillTimer / physicsData.dashRefillTime);

        
    //Input
    private Vector2 moveInput;
    public Vector2 MoveInput => moveInput; // for External Use


    

    //Checks & Tags
    [Header("Checks")]
    [SerializeField] private Transform _groundCheckPoint;
    [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.49f, 0.03f);
    [Space(5)]
    [SerializeField] private Transform _frontWallCheckPoint;
    [SerializeField] private Transform _backWallCheckPoint;
    [SerializeField] private Vector2 _wallCheckSize = new Vector2(0.5f, 1f);
    
    [Header("Layers & Tags")]
    [SerializeField] private LayerMask _groundLayer;
    #endregion

    // Grounded only, so the shade never lands on a spike.
    public Vector2 LastSafeGround { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Sprite = GetComponent<SpriteRenderer>();
        input = GetComponent<PlayerInput>();
    }
    private void Start()
    {
        SetGravityScale(physicsData.gravityScale);
        state.IsFacingRight = true;
        LastSafeGround = transform.position;
    }

    // dashesLeft defaults to 0, so without this you spawn with none. Also covers respawn.
    private void OnEnable()
    {
        dashesLeft = MaxDashes;
        dashRefillTimer = 0f;
    }

    private void Update()
    {
        #region TIMERS
        LastOnGroundTime -= Time.deltaTime;
        LastOnWallTime -= Time.deltaTime;
        LastOnWallRightTime -= Time.deltaTime;
        LastOnWallLeftTime -= Time.deltaTime;

        LastPressedJumpTime -= Time.deltaTime;
        LastPressedDashTime -= Time.deltaTime;

        wallJumpTimer -= Time.deltaTime;
        wallJumpReturnTimer -= Time.deltaTime;
        wallJumpRegrabTimer -= Time.deltaTime;

        #endregion

        #region INPUT

        moveInput = input.Movement;

        // Check If player should be facing left or right
        if (moveInput.x != 0 && !state.IsBusy)
        {
            facingDir = moveInput.x > 0 ? 1 : -1;
            state.IsFacingRight = facingDir == 1;
        }

        Sprite.flipX = facingDir == -1;


        if (input.JumpPressed)
        {
            LastPressedJumpTime = physicsData.jumpInputBufferTime;
            jumpCutQueued = false;
        }

        if (input.JumpReleased)
        {
            jumpCutQueued = true;
        }

        if (input.DashPressed)
        {
            LastPressedDashTime = physicsData.dashInputBufferTime;
        }

        state.IsGrounded = LastOnGroundTime > 0f;

        #endregion

        #region COLLISION CHECKS
        if (state.CurrentState != PlayerStateType.Dash)
        {
            //Ground Check
            if (Physics2D.OverlapBox(_groundCheckPoint.position, _groundCheckSize, 0, _groundLayer))
            {
                LastOnGroundTime = physicsData.coyoteTime;
                LastSafeGround = transform.position;
            }


            //Wall Check
            if (wallJumpRegrabTimer <= 0f)
            {
                bool frontWall = Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _groundLayer);
                bool backWall = Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _groundLayer);

                RegisterWallCheck(frontWall, _frontWallCheckPoint);
                RegisterWallCheck(backWall, _backWallCheckPoint);

                LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);
            }

            //Slide Check
            bool pressingIntoWall = (LastOnWallRightTime > 0 && moveInput.x > 0) || (LastOnWallLeftTime > 0 && moveInput.x < 0);

            state.IsSliding = GameManager.Instance.activeRun.isWallJumpUnlocked && LastOnGroundTime <= 0 && LastOnWallTime > 0 && pressingIntoWall;
        }
        #endregion

        #region JUMP CHECKS

        // Detect leaving ground
        if (!state.IsGrounded && wasGroundedLastFrame)
        {
            jumpNumber = 1; // consume jump
        }

        // Reset jumps when grounded
        if (LastOnGroundTime > 0)
        {
            jumpNumber = 0;
        }

        if (state.IsGrounded && !wasGroundedLastFrame) SoundManager.Play(SoundId.Land);

        wasGroundedLastFrame = state.IsGrounded;

        // Jump
        if (CanWallJump() && LastPressedJumpTime > 0)
        {
            lastWallJumpDir = -wallDir;
            WallJump(lastWallJumpDir);
            LastPressedJumpTime = 0;
        }
        else if (CanJump() && LastPressedJumpTime > 0)
        {
            jumpNumber++;
            SoundManager.Play(jumpNumber > 1 ? SoundId.DoubleJump : SoundId.Jump);
            Jump();
            LastPressedJumpTime = 0;
        }
        #endregion

        #region DASH CHECKS
        if (CanDash())
        {
            LastPressedDashTime = 0;

            // If there is horizontal input, dash that way
            if (Mathf.Abs(moveInput.x) > 0.1f)
            {
                lastDashDir = new Vector2(Mathf.Sign(moveInput.x), 0);
            }
            // Otherwise, dash based on facing direction
            else
            {
                lastDashDir = state.IsFacingRight ? Vector2.right : Vector2.left;
            }

            state.CurrentState = PlayerStateType.Dash;

            StartCoroutine(StartDash(lastDashDir));
        }
        #endregion

        #region SLIDE CHECKS
        if (CanSlide() && ((LastOnWallLeftTime > 0 && moveInput.x < 0) || (LastOnWallRightTime > 0 && moveInput.x > 0)))
            state.CurrentState = PlayerStateType.WallSlide;
        #endregion

        #region GRAVITY
        if (state.CurrentState == PlayerStateType.WallSlide)
        {
            SetGravityScale(0);
        }
        else if (state.CurrentState == PlayerStateType.Dash)
        {
            SetGravityScale(0);
        }
        else if (rb.linearVelocity.y < 0 && moveInput.y < 0)
        {
            //Much higher gravity if holding down
            SetGravityScale(physicsData.gravityScale);
            //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y));
        }
        else if ((state.CurrentState == PlayerStateType.Jump || state.CurrentState == PlayerStateType.Fall) && Mathf.Abs(rb.linearVelocity.y) < physicsData.jumpHangTimeThreshold)
        {
            SetGravityScale(physicsData.gravityScale * physicsData.jumpHangGravityMult);
        }
        else if (rb.linearVelocity.y < 0)
        {
            //Higher gravity if falling
            SetGravityScale(physicsData.gravityScale * physicsData.fallGravityMult);
            //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -physicsData.maxFallSpeed));
        }
        else
        {
            //Default gravity if standing on a platform or moving upwards
            SetGravityScale(physicsData.gravityScale);
        }

        #endregion

        TryCutJump();
        TickDashRefill();
        TickFootsteps();

        UpdateState();
    }

    private void FixedUpdate()
    {
        //Handle Run
        if ((!state.IsUsingAbility || state.CurrentState == PlayerStateType.Burst) && !state.IsDashing)
        {
            Run();
        }

        HandleWallJumpReturnAssist();

        //Handle Slide
        if (state.CurrentState == PlayerStateType.WallSlide)
            Slide();
    }

    void UpdateState()
    {
        if (state.IsUsingAbility)
        {
            return;
        }

        // ACTION STATES
        if (state.IsAttacking)
        {
            return;
        }

        if (state.IsDashing)
        {
            state.CurrentState = PlayerStateType.Dash;
            return;
        }

        if (state.IsSliding)
        {
            state.CurrentState = PlayerStateType.WallSlide;
            return;
        }

        // AIR STATES
        if (LastOnGroundTime <= 0)
        {
            if (rb.linearVelocity.y > 0.1f)
                state.CurrentState = PlayerStateType.Jump;
            else 
                state.CurrentState = PlayerStateType.Fall;

            return;
        }

        // GROUND STATES
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f && moveInput.x != 0)
            state.CurrentState = PlayerStateType.Run;
        else
            state.CurrentState = PlayerStateType.Idle;
    }

    #region GENERAL METHODS
    public void SetGravityScale(float scale)
    {
        rb.gravityScale = scale;
    }

    private void Sleep(float duration)
    {
        StartCoroutine(nameof(PerformSleep), duration);
    }

    private IEnumerator PerformSleep(float duration)
    {
        TimeManager.Freeze(this);
        yield return new WaitForSecondsRealtime(duration);
        TimeManager.Release(this);
    }

    private void OnDisable()
    {
        TimeManager.Release(this);

        // The dash coroutine dies with a disabled object, leaving IsBusy permanent.
        if (state != null) state.IsDashing = false;
    }
    #endregion


    //MOVEMENT METHODS
    #region RUN METHODS
    private void Run()
    {
        //Calculate the direction we want to move in and our desired velocity
        float targetSpeed = moveInput.x * physicsData.runMaxSpeed;


        //Gets an acceleration value based on if we are accelerating (includes turning) 
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? physicsData.runAccelAmount : physicsData.runDeccelAmount;

        if (wallJumpTimer > 0f)
            accelRate *= physicsData.wallJumpRunLerp;

        //Conserve Momentum
        if (physicsData.doConserveMomentum && Mathf.Abs(rb.linearVelocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && LastOnGroundTime < 0)
        {
            accelRate = 0;
        }

        float newVelX = Mathf.Lerp(
            rb.linearVelocity.x,
            targetSpeed,
            accelRate * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);
    }
    #endregion

    #region JUMP METHODS

    // Normal jump
    private void Jump()
    {
        //Ensures we can't call Jump multiple times from one press
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;

        float force = physicsData.jumpForce;

        if (jumpNumber > 1)
        {
            force *= 0.8f;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.2f);

        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }

    // Every frame, not on the release event - the release can arrive before the jump.
    private void TryCutJump()
    {
        if (!jumpCutQueued) return;

        if (rb.linearVelocity.y <= 0f)
        {
            // Already falling, nothing to cut.
            if (LastOnGroundTime > 0) jumpCutQueued = false;
            return;
        }

        if (jumpNumber != 1 || wallJumpTimer > 0f) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x,
                                        rb.linearVelocity.y * physicsData.jumpCutMultiplier);
        jumpCutQueued = false;
    }


    // Wall jump
    private void WallJump(int dir)
    {
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;
        LastOnWallTime = 0;
        LastOnWallRightTime = 0;
        LastOnWallLeftTime = 0;


        wallJumpTimer = physicsData.wallJumpTime;
        wallJumpRegrabTimer = physicsData.wallJumpRegrabDelay;
        wallJumpReturnTimer = physicsData.wallJumpTime + physicsData.wallJumpReturnWindow;
        usedWallReturnAssist = false;
        lastWallJumpDir = dir;
        jumpNumber = 1;

        state.IsSliding = false;
        state.CurrentState = PlayerStateType.Jump;

        float xForce = Mathf.Abs(physicsData.wallJumpForce.x) * dir;
        rb.linearVelocity = new Vector2(xForce, physicsData.wallJumpForce.y);
    }

    private void RegisterWallCheck(bool hitWall, Transform checkPoint)
    {
        if (!hitWall) return;

        if (checkPoint.position.x > transform.position.x)
        {
            LastOnWallRightTime = physicsData.coyoteTime;
            wallDir = 1;
        }
        else
        {
            LastOnWallLeftTime = physicsData.coyoteTime;
            wallDir = -1;
        }
    }

    private void HandleWallJumpReturnAssist()
    {
        if (usedWallReturnAssist) return;
        if (wallJumpTimer > 0f) return;
        if (wallJumpReturnTimer <= 0f) return;
        if (LastOnGroundTime > 0f) return;

        int returnDir = -lastWallJumpDir;

        if (returnDir > 0 && moveInput.x <= 0.1f) return;
        if (returnDir < 0 && moveInput.x >= -0.1f) return;

        usedWallReturnAssist = true;

        float yVel = Mathf.Max(rb.linearVelocity.y, physicsData.wallJumpReturnMinY);
        rb.linearVelocity = new Vector2(returnDir * physicsData.wallJumpReturnSpeed, yVel);
    }

    #endregion

    #region DASH METHODS
    //Dash Coroutine
    private IEnumerator StartDash(Vector2 dir)
    {
        state.IsDashing = true;
        SoundManager.Play(SoundId.Dash);

        dashesLeft--;
        lastDashTime = Time.time;
        float gScale = physicsData.gravityScale;

        rb.linearVelocity = Vector2.zero;
        
        rb.AddForce(dir * physicsData.dashSpeed, ForceMode2D.Impulse);

        float startTime = Time.time;

        while (Time.time - startTime <= physicsData.dashTime)
        {
            yield return null;
        }

        //Dash over
        state.IsDashing = false;
    }

    // One charge at a time, never in parallel.
    private void TickFootsteps()
    {
        bool walking = state.IsGrounded
                       && !state.IsBusy
                       && Mathf.Abs(moveInput.x) > 0.1f
                       && Mathf.Abs(rb.linearVelocity.x) > 0.5f;

        if (!walking)
        {
            // Primed, so the first step lands as you start moving.
            stepTravelled = stepDistance;
            return;
        }

        stepTravelled += Mathf.Abs(rb.linearVelocity.x) * Time.deltaTime;
        if (stepTravelled < stepDistance) return;

        stepTravelled = 0f;
        SoundManager.Play(SoundId.Footstep);
    }

    private void TickDashRefill()
    {
        if (dashesLeft > MaxDashes) dashesLeft = MaxDashes;

        // Counts during the dash too, or the real gap is dashTime + dashRefillTime.
        if (dashesLeft >= MaxDashes)
        {
            dashRefillTimer = 0f;
            return;
        }

        dashRefillTimer += Time.deltaTime;
        if (dashRefillTimer < physicsData.dashRefillTime) return;

        // Carries the overshoot, so the second charge is not a frame slower.
        dashRefillTimer -= physicsData.dashRefillTime;
        dashesLeft++;
    }
    #endregion

    #region OTHER MOVEMENT METHODS
    private void Slide()
    {

        float speedDif = physicsData.slideSpeed - rb.linearVelocity.y;
        float movement = speedDif * physicsData.slideAccel;
        movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));

        rb.AddForce(movement * Vector2.up);
    }
    #endregion


    #region CHECK METHODS

    private bool CanJump()
    {
        int allowedJumps = GameManager.Instance.activeRun.isDoubleJumpUnlocked ? physicsData.jumpAmount : 1;
        return (LastOnGroundTime > 0 || jumpNumber < allowedJumps) && !state.IsBusy;
    }

    private bool CanWallJump()
    {
        if (!GameManager.Instance.activeRun.isWallJumpUnlocked) return false;
        if (wallJumpRegrabTimer > 0f) return false;

        return LastPressedJumpTime > 0 && LastOnWallTime > 0 && LastOnGroundTime <= 0 && !state.IsBusy;
    }
    private bool CanDash()
    {
       
        if (!GameManager.Instance.activeRun.isDashUnlocked) return false;

        // Separate from the refill, or two charges chain with no gap.
        if (Time.time < lastDashTime + physicsData.dashCooldown) return false;

        return !state.IsBusy && dashesLeft > 0 && LastPressedDashTime > 0;

    }

    public bool CanSlide()
    {
        if (!GameManager.Instance.activeRun.isWallJumpUnlocked) return false;

        if (LastOnWallTime > 0 && state.CurrentState != PlayerStateType.Jump && state.CurrentState != PlayerStateType.Dash && LastOnGroundTime <= 0)
            return true;
        else
            return false;
    }
    #endregion

    #region EDITOR METHODS
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(_groundCheckPoint.position, _groundCheckSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(_frontWallCheckPoint.position, _wallCheckSize);
        Gizmos.DrawWireCube(_backWallCheckPoint.position, _wallCheckSize);
    }
    #endregion
}