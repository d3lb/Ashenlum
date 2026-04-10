using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerState;

public class PlayerMovement : MonoBehaviour
{
    public PlayerData Data;
    private PlayerAnimation playerAnimation;

    #region VARIABLES 

    //Components
    public Rigidbody2D RB { get; private set; }
    private SpriteRenderer Sprite;

    //States
    [SerializeField] private PlayerState state;
    private int facingDir;

    //Timers
    public float LastOnGroundTime { get; private set; }
    public float LastOnWallTime { get; private set; }
    public float LastOnWallRightTime { get; private set; }
    public float LastOnWallLeftTime { get; private set; }
    public float LastPressedJumpTime { get; private set; }
    public float LastPressedDashTime { get; private set; }

    //Ground
    private bool wasGroundedLastFrame;
    //Jump
    public int _jumpNumber;

    //Wall Jump
    private int _lastWallJumpDir;

    //Dash
    private int _dashesLeft;
    private bool _dashRefilling;
    private Vector2 _lastDashDir;

        
    //Input
    private Vector2 _moveInput;
    public Vector2 MoveInput => _moveInput; // for Graph debugging


    

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

    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
        Sprite = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        SetGravityScale(Data.gravityScale);
        state.IsFacingRight = true;
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
        #endregion

        #region INPUT
        _moveInput.x = Input.GetAxisRaw("Horizontal");
        _moveInput.y = Input.GetAxisRaw("Vertical");

        // Check If player should be facing left or right
        if (_moveInput.x != 0)
        {
            facingDir = (_moveInput.x > 0) ? 1 : -1;
            state.IsFacingRight = facingDir == 1;
        }
            Sprite.flipX = facingDir == -1;


        if (Input.GetKeyDown(KeyCode.Space))
        {
            LastPressedJumpTime = Data.jumpInputBufferTime;
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            // Cancel the jump buffer if released early
            LastPressedJumpTime = 0;

            if (state.CurrentState == PlayerStateType.Jump)
                StartCoroutine(CutJump());
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            LastPressedDashTime = Data.dashInputBufferTime;
        }
        #endregion

        #region COLLISION CHECKS
        if (state.CurrentState != PlayerStateType.Dash && state.CurrentState != PlayerStateType.Jump)
        {
            //Ground Check
            if (Physics2D.OverlapBox(_groundCheckPoint.position, _groundCheckSize, 0, _groundLayer) && state.CurrentState != PlayerStateType.Jump)
            {
                LastOnGroundTime = Data.coyoteTime;
            }

            // Detect leaving ground
            if (LastOnGroundTime <= 0 && wasGroundedLastFrame)
            {
                _jumpNumber = 1; // consume jump
            }

            // Reset jumps when grounded
            if (LastOnGroundTime > 0)
            {
                _jumpNumber = 0;
            }

            wasGroundedLastFrame = LastOnGroundTime > 0;

            //Wall Check
            bool frontWall = Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _groundLayer);
            bool backWall  = Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _groundLayer);

            if (frontWall) LastOnWallRightTime = Data.coyoteTime;
            if (backWall)  LastOnWallLeftTime  = Data.coyoteTime;

            LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);


            //Slide Check
            state.IsSliding =
                LastOnGroundTime <= 0 &&
                LastOnWallTime > 0 &&
                Mathf.Abs(_moveInput.x) > 0.1f;
        }
        #endregion

        #region JUMP CHECKS
        if (CanJump() && LastPressedJumpTime > 0)
        {
            _jumpNumber++;
            Jump();

            LastPressedJumpTime = 0;
        }
        //WALL JUMP
        else if (CanWallJump() && LastPressedJumpTime > 0)
        {
            _lastWallJumpDir = (LastOnWallRightTime > 0) ? -1 : 1;

            _jumpNumber++;
            WallJump(_lastWallJumpDir);

            LastPressedJumpTime = 0;
        }
        #endregion

        #region DASH CHECKS
        if (CanDash())
        {
            LastPressedDashTime = 0;

            // If there is horizontal input, dash that way
            if (Mathf.Abs(_moveInput.x) > 0.1f)
            {
                _lastDashDir = new Vector2(Mathf.Sign(_moveInput.x), 0);
            }
            // Otherwise, dash based on facing direction
            else
            {
                _lastDashDir = state.IsFacingRight ? Vector2.right : Vector2.left;
            }

            state.CurrentState = PlayerStateType.Dash;

            StartCoroutine(StartDash(_lastDashDir));
        }
        #endregion

        #region SLIDE CHECKS
        if (CanSlide() && ((LastOnWallLeftTime > 0 && _moveInput.x < 0) || (LastOnWallRightTime > 0 && _moveInput.x > 0)))
            state.CurrentState = PlayerStateType.WallSlide;
        #endregion

        #region GRAVITY
        if (state.CurrentState == PlayerStateType.WallSlide)
            {
                SetGravityScale(0);
            }
            else if (RB.linearVelocity.y < 0 && _moveInput.y < 0)
            {
                //Much higher gravity if holding down
                SetGravityScale(Data.gravityScale);
                //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
                RB.linearVelocity = new Vector2(RB.linearVelocity.x, Mathf.Max(RB.linearVelocity.y));
            }
            else if ((state.CurrentState == PlayerStateType.Jump || state.CurrentState == PlayerStateType.Fall) && Mathf.Abs(RB.linearVelocity.y) < Data.jumpHangTimeThreshold)
            {
                SetGravityScale(Data.gravityScale * Data.jumpHangGravityMult);
            }
            else if (RB.linearVelocity.y < 0)
            {
                //Higher gravity if falling
                SetGravityScale(Data.gravityScale * Data.fallGravityMult);
                //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
                RB.linearVelocity = new Vector2(RB.linearVelocity.x, Mathf.Max(RB.linearVelocity.y, -Data.maxFallSpeed));
            }
            else
            {
                //Default gravity if standing on a platform or moving upwards
                SetGravityScale(Data.gravityScale);
            }

        #endregion

        UpdateState();
        Debug.Log("State: " + state.CurrentState);
    }

    private void FixedUpdate()
    {
        //Handle Run
            Run();

        //Handle Slide
        if (state.CurrentState == PlayerStateType.WallSlide)
            Slide();
    }

    void UpdateState()
    {
        // ACTION STATES
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
            if (RB.linearVelocity.y > 0.1f)
                state.CurrentState = PlayerStateType.Jump;
            else 
                state.CurrentState = PlayerStateType.Fall;

            return;
        }

        // GROUND STATES
        if (Mathf.Abs(RB.linearVelocity.x) > 0.1f && _moveInput.x != 0)
            state.CurrentState = PlayerStateType.Run;
        else
            state.CurrentState = PlayerStateType.Idle;
    }

    #region GENERAL METHODS
    public void SetGravityScale(float scale)
    {
        RB.gravityScale = scale;
    }

    private void Sleep(float duration)
    {
        StartCoroutine(nameof(PerformSleep), duration);
    }

    private IEnumerator PerformSleep(float duration)
    {
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1;
    }
    #endregion

    //MOVEMENT METHODS
    #region RUN METHODS
    private void Run()
    {
        //Calculate the direction we want to move in and our desired velocity
        float targetSpeed = _moveInput.x * Data.runMaxSpeed;


        //Gets an acceleration value based on if we are accelerating (includes turning) 
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;

        #region Conserve Momentum
        //We won't slow the player down if they are moving in their desired direction but at a greater speed than their maxSpeed
        if (Data.doConserveMomentum && Mathf.Abs(RB.linearVelocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(RB.linearVelocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && LastOnGroundTime < 0)
        {
            //Prevent any deceleration from happening, or in other words conserve are current momentum
            accelRate = 0;
        }
        #endregion

        float newVelX = Mathf.Lerp(
            RB.linearVelocity.x,
            targetSpeed,
            accelRate * Time.fixedDeltaTime
        );

        RB.linearVelocity = new Vector2(newVelX, RB.linearVelocity.y);
    }
    #endregion

    #region JUMP METHODS
    private void Jump()
    {
        //Ensures we can't call Jump multiple times from one press
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;

        float force = Data.jumpForce;

        if (_jumpNumber > 1)
        {
            force *= 0.6f;
        }

        if (RB.linearVelocity.y < 0)
            force -= RB.linearVelocity.y;

        RB.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }
    private IEnumerator CutJump()
    {
        yield return null;
        if (RB.linearVelocity.y > 0 && _jumpNumber == 1)
            RB.linearVelocity = new Vector2(RB.linearVelocity.x, RB.linearVelocity.y * 0.5f);
    }



    private void WallJump(int dir)
    {
        //Ensures we can't call Wall Jump multiple times from one press
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;
        LastOnWallRightTime = 0;
        LastOnWallLeftTime = 0;

        #region Perform Wall Jump
        Vector2 force = new Vector2(Data.wallJumpForce.x, Data.wallJumpForce.y);
        force.x *= dir; //apply force in opposite direction of wall

        if (Mathf.Sign(RB.linearVelocity.x) != Mathf.Sign(force.x))
            force.x -= RB.linearVelocity.x;

        if (RB.linearVelocity.y < 0) //checks whether player is falling, if so we subtract the velocity.y (counteracting force of gravity). This ensures the player always reaches our desired jump force or greater
            force.y -= RB.linearVelocity.y;

        //Unlike in the run we want to use the Impulse mode.
        //The default mode will apply are force instantly ignoring masss
        RB.AddForce(force, ForceMode2D.Impulse);
        #endregion
    }
    #endregion

    #region DASH METHODS
    //Dash Coroutine
    private IEnumerator StartDash(Vector2 dir)
    {
        state.IsDashing = true;

        _dashesLeft--;
        float gScale = Data.gravityScale;
        SetGravityScale(0);

        RB.linearVelocity = Vector2.zero;
        
        RB.AddForce(dir * Data.dashSpeed, ForceMode2D.Impulse);

        float startTime = Time.time;

        while (Time.time - startTime <= Data.dashTime)
        {
            yield return null;
        }

        //Dash over
        state.IsDashing = false;
        SetGravityScale(gScale);
    }

    //Short period before the player is able to dash again
    private IEnumerator RefillDash(int amount)
    {
        _dashRefilling = true;
        yield return new WaitForSeconds(Data.dashRefillTime);
        _dashRefilling = false;
        _dashesLeft = Mathf.Min(Data.dashAmount, _dashesLeft + 1);
    }
    #endregion

    #region OTHER MOVEMENT METHODS
    private void Slide()
    {
        //Works the same as the Run but only in the y-axis
        //THis seems to work fine, buit maybe you'll find a better way to implement a slide into this system
        float speedDif = Data.slideSpeed - RB.linearVelocity.y;
        float movement = speedDif * Data.slideAccel;
        //So, we clamp the movement here to prevent any over corrections (these aren't noticeable in the Run)
        //The force applied can't be greater than the (negative) speedDifference * by how many times a second FixedUpdate() is called. For more info research how force are applied to rigidbodies.
        movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));

        RB.AddForce(movement * Vector2.up);
    }
    #endregion


    #region CHECK METHODS

    private bool CanJump()
    {
        return LastOnGroundTime > 0 ||  _jumpNumber < Data.jumpAmount;
    }

    private bool CanWallJump()
    {
        return LastPressedJumpTime > 0 && LastOnWallTime > 0 && LastOnGroundTime <= 0 && (state.CurrentState != PlayerStateType.Jump ||
             (LastOnWallRightTime > 0 && _lastWallJumpDir == 1) || (LastOnWallLeftTime > 0 && _lastWallJumpDir == -1));
    }
    private bool CanDash()
    {
        if (state.CurrentState != PlayerStateType.Dash && _dashesLeft < Data.dashAmount && LastOnGroundTime > 0 && !_dashRefilling)
        {
            StartCoroutine(nameof(RefillDash), 1);
        }

        return !state.IsBusy && _dashesLeft > 0 && LastPressedDashTime > 0;
        
    }

    public bool CanSlide()
    {
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