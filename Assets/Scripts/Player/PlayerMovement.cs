using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public PlayerData Data;
    private PlayerAnimation playerAnimation;

    #region VARIABLES 

    //Components
    public Rigidbody2D RB { get; private set; }
    private List<GameObject> trailObjects = new List<GameObject>(); // For The Dash Trail
    private SpriteRenderer Sprite;

    //States
    [SerializeField] private PlayerState state;

    //Timers
    public float LastOnGroundTime { get; private set; }
    public float LastOnWallTime { get; private set; }
    public float LastOnWallRightTime { get; private set; }
    public float LastOnWallLeftTime { get; private set; }

    //Jump
    private bool _isJumpFalling;
    private int _jumpNumber;

    //Wall Jump
    private float _wallJumpStartTime;
    private int _lastWallJumpDir;

    //Dash
    private int _dashesLeft;
    private bool _dashRefilling;
    private Vector2 _lastDashDir;


    //Input
    private Vector2 _moveInput;

    public float LastPressedJumpTime { get; private set; }
    public float LastPressedDashTime { get; private set; }
    

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
        Facing();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            LastPressedJumpTime = Data.jumpInputBufferTime;
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            // Cancel the jump buffer if released early
            LastPressedJumpTime = 0;

            if (state.IsJumping)
                StartCoroutine(CutJump());
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            LastPressedDashTime = Data.dashInputBufferTime;
        }
        #endregion



        #region COLLISION CHECKS
        if (!state.IsDashing && !state.IsJumping)
        {
            //Ground Check
            if (Physics2D.OverlapBox(_groundCheckPoint.position, _groundCheckSize, 0, _groundLayer) && !state.IsJumping)
            {
                LastOnGroundTime = Data.coyoteTime;
                _jumpNumber = 0;
            }

            //Right Wall Check
            if (((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && state.IsFacingRight)
                    || (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && !state.IsFacingRight)) && !state.IsWallJumping)
                LastOnWallRightTime = Data.coyoteTime;

            //Left Wall Check
            if (((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && !state.IsFacingRight)
                || (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && state.IsFacingRight)) && !state.IsWallJumping)
                LastOnWallLeftTime = Data.coyoteTime;

            //Two checks needed for both left and right walls since whenever the play turns the wall checkPoints swap sides
            LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);
        }
        #endregion

        #region JUMP CHECKS
        if (state.IsJumping && RB.linearVelocity.y < 0)
        {
            state.IsJumping = false;

            if (!state.IsWallJumping)
                state.IsFalling = true;
        }

        if (state.IsWallJumping && Time.time - _wallJumpStartTime > Data.wallJumpTime)
        {
            state.IsWallJumping = false;
        }

        if (LastOnGroundTime > 0 && !state.IsJumping && !state.IsWallJumping)
        {
            state.IsFalling = false;
        }

        if (CanJump() && LastPressedJumpTime > 0)
        {
            state.IsJumping = true;
            state.IsWallJumping = false;
            state.IsFalling = false;
            _jumpNumber++;
            Jump();
        }
        else if (!state.IsBusy)
        {
            //WALL JUMP
            if (CanWallJump() && LastPressedJumpTime > 0)
            {
                state.IsWallJumping = true;
                state.IsJumping = false;
                state.IsFalling = false;

                _wallJumpStartTime = Time.time;
                _lastWallJumpDir = (LastOnWallRightTime > 0) ? -1 : 1;

                WallJump(_lastWallJumpDir);
            }
        }
        #endregion

        #region DASH CHECKS
        if (CanDash())
        {
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

            state.IsDashing = true;
            state.IsJumping = false;
            state.IsWallJumping = false;

            StartCoroutine(StartDash(_lastDashDir));
        }
        #endregion

        #region SLIDE CHECKS
        if (CanSlide() && ((LastOnWallLeftTime > 0 && _moveInput.x < 0) || (LastOnWallRightTime > 0 && _moveInput.x > 0)))
            state.IsSliding = true;
        else
            state.IsSliding = false;
        #endregion

        #region GRAVITY
        if (!state.IsBusy)
        {
            if (state.IsSliding)
            {
                SetGravityScale(0);
            }
            else if (RB.linearVelocity.y < 0 && _moveInput.y < 0)
            {
                //Much higher gravity if holding down
                SetGravityScale(Data.gravityScale * Data.fastFallGravityMult);
                //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
                RB.linearVelocity = new Vector2(RB.linearVelocity.x, Mathf.Max(RB.linearVelocity.y, -Data.maxFastFallSpeed));
            }
            else if ((state.IsJumping || state.IsWallJumping || state.IsFalling) && Mathf.Abs(RB.linearVelocity.y) < Data.jumpHangTimeThreshold)
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
        }
        #endregion
    }

    private void FixedUpdate()
    {
        //Handle Run
        if (!state.IsBusy)
        {
            if (state.IsWallJumping)
                Run(Data.wallJumpRunLerp);
            else
                Run(1);
        }

        //Handle Slide
        if (state.IsSliding)
            Slide();
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
    private void Run(float lerpAmount)
    {
        //Calculate the direction we want to move in and our desired velocity
        float targetSpeed = _moveInput.x * Data.runMaxSpeed;
        //We can reduce are control using Lerp() this smooths changes to are direction and speed
        targetSpeed = Mathf.Lerp(RB.linearVelocity.x, targetSpeed, lerpAmount);

        #region Calculate AccelRate
        float accelRate;

        //Gets an acceleration value based on if we are accelerating (includes turning) 
        //or trying to decelerate (stop). As well as applying a multiplier if we're air borne.
        if (LastOnGroundTime > 0)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount * Data.accelInAir : Data.runDeccelAmount * Data.deccelInAir;
        #endregion

        #region Conserve Momentum
        //We won't slow the player down if they are moving in their desired direction but at a greater speed than their maxSpeed
        if (Data.doConserveMomentum && Mathf.Abs(RB.linearVelocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(RB.linearVelocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && LastOnGroundTime < 0)
        {
            //Prevent any deceleration from happening, or in other words conserve are current momentum
            //You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
            accelRate = 0;
        }
        #endregion

        //Calculate difference between current velocity and desired velocity
        float speedDif = targetSpeed - RB.linearVelocity.x;
        //Calculate force along x-axis to apply to thr player

        float movement = speedDif * accelRate;

        //Convert this to a vector and apply to rigidbody
        RB.AddForce(movement * Vector2.right, ForceMode2D.Force);

        /*
		 * For those interested here is what AddForce() will do
		 * RB.velocity = new Vector2(RB.velocity.x + (Time.fixedDeltaTime  * speedDif * accelRate) / RB.mass, RB.velocity.y);
		 * Time.fixedDeltaTime is by default in Unity 0.02 seconds equal to 50 FixedUpdate() calls per second
		*/
    }

    private void Facing()
    {
        if (_moveInput.x < 0)
        {
            state.IsFacingRight = false;
            transform.localScale = new Vector2(-1, transform.localScale.y);
        }
        else if (_moveInput.x > 0) 
        {
            state.IsFacingRight = true;
            transform.localScale = new Vector2(1, transform.localScale.y); 
        }

    }
    #endregion

    #region JUMP METHODS
    private void Jump()
    {
        //Ensures we can't call Jump multiple times from one press
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;

        float force = Data.jumpForce;
        if (RB.linearVelocity.y < 0)
            force -= RB.linearVelocity.y;

        RB.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }
    private IEnumerator CutJump()
    {
        yield return null;
        if (RB.linearVelocity.y > 0)
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
        LastOnGroundTime = 0;
        LastPressedDashTime = 0;
        
        StartCoroutine(CreateTrailObjects());

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
        SetGravityScale(gScale);
        state.IsDashing = false;
        ClearTrail();
    }

    //Short period before the player is able to dash again
    private IEnumerator RefillDash(int amount)
    {
        _dashRefilling = true;
        yield return new WaitForSeconds(Data.dashRefillTime);
        _dashRefilling = false;
        _dashesLeft = Mathf.Min(Data.dashAmount, _dashesLeft + 1);
    }

    private IEnumerator CreateTrailObjects()
    {
        while (state.IsDashing)
        {
            GameObject trailObject = new GameObject("Trail");
            trailObject.transform.position = transform.position;

            GameObject Trail = new GameObject("DashTrail");
            Trail.transform.SetParent(trailObject.transform);
            Trail.transform.localPosition = Vector3.zero;

            SpriteRenderer TrailRenderer = Trail.AddComponent<SpriteRenderer>();
            TrailRenderer.sprite = Sprite.sprite;
            TrailRenderer.flipX = Sprite.flipX;
            TrailRenderer.color = new Color(0f, 0f, 0f, 0.85f);

            trailObjects.Add(trailObject);

            yield return new WaitForSeconds(0.05f);
        }
    }

    private void ClearTrail()
    {
        foreach (var obj in trailObjects)
        {
            Destroy(obj);
        }
        trailObjects.Clear();
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
        return LastOnGroundTime > 0 && !state.IsJumping || _jumpNumber < Data.jumpAmount;
    }

    private bool CanWallJump()
    {
        return LastPressedJumpTime > 0 && LastOnWallTime > 0 && LastOnGroundTime <= 0 && (!state.IsWallJumping ||
             (LastOnWallRightTime > 0 && _lastWallJumpDir == 1) || (LastOnWallLeftTime > 0 && _lastWallJumpDir == -1));
    }
    private bool CanDash()
    {
        if (!state.IsDashing && _dashesLeft < Data.dashAmount && LastOnGroundTime > 0 && !_dashRefilling)
        {
            StartCoroutine(nameof(RefillDash), 1);
        }

        return !state.IsBusy && _dashesLeft > 0 && LastPressedDashTime > 0;
        
    }

    public bool CanSlide()
    {
        if (LastOnWallTime > 0 && !state.IsJumping && !state.IsWallJumping && !state.IsDashing && LastOnGroundTime <= 0)
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