using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovements : MonoBehaviour
{
    public static PlayerMovements instance;

    private Controls _inputActions;

    private InputAction _moveAction;

    public  Vector2 _direction;
    public static float moveDir;

    private InputAction _jumpAction;

    private InputAction _dashAction;

    private InputAction _grabAction;


    [Header("Movements Values")]
    [SerializeField] private float moveDeadZone;
    [SerializeField] private float acceleration;
    [SerializeField] private float speed;
    [SerializeField] private float decceleration;
    [SerializeField] private float velPower;
    [SerializeField] private float frictionAmount;
    [SerializeField] private float fallGravityMultiplier;
    [SerializeField] private float fallGravityTreshold;
    [SerializeField] private float gravityScale;
    [SerializeField] private float maxDownSpeed;

    [Header("Jump")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float coyoteTime;
    [SerializeField] private float jumpCutMultiplier;
    [SerializeField] private float jumpInputBufferTime;

    [Header("Dash")]
    [SerializeField] private float dashingPower;
    [SerializeField] private float dashingTime;
    [SerializeField] private float dashCooldown;
    [SerializeField] private Vector2 superDashForce;
    [SerializeField] private Vector2 wallBounceForce;

    [Header ("walls")]
    [SerializeField] private Vector2 wallJumpForce;
    [SerializeField] private float maxEndurance;
    [SerializeField] private float enduranceLossWhenJump;
    [SerializeField] private float enduranceLossMultiplyWhenClimbing;
    [SerializeField] private float climbSpeed;
    [SerializeField] private Vector2 toTopOfWall;

    [Header("Checks")]
    [SerializeField] private Vector2 checkGroundSize;
    [SerializeField] private Vector2 checkGroundOffset;
    [SerializeField] private Vector2 checkWallSize;
    [SerializeField] private Vector2 checkWallOffset;
    [SerializeField] private Vector2 checkWallBelowSize;
    [SerializeField] private Vector2 checkWallBelowOffset;
    [SerializeField] private LayerMask groundLayers;

    [Header("Rendering")]
    [SerializeField] private GameObject playerRenderer;

    [Header("Hair")]
    [SerializeField] private Vector2 baseHairOffset;
    [SerializeField] private float maxHairOffsetX;
    [SerializeField] private float maxHairOffsetY;
    [SerializeField] private float normalHairLerpSpeed;
    [SerializeField] private float dashHairLerpSpeed;

    [SerializeField] private Color redHairColor;
    [SerializeField] private Color blueHairColor;
    [SerializeField] private Color whiteHairColor;

    private Rigidbody2D _rb;
    private SpriteRenderer _sprite;
    private Animator _Anim;

    private GameObject currentCheckpoint;

    private float lastGroundTime;
    private float lastJumpTime;
    private float lastDashTime;
    private float jumpInputBuffer;
    private float lastWallJumpTimer;

    private bool isJumping;
    private bool isDashing;
    private bool isGrabbed;

    private bool grabingAction;

    private bool hasQuittedGround;

    private bool canDash;
    private Vector2 dashDir;

    private Vector2 wallDir;
    private Vector2 lastWallDir;

    private float endurance;

    private bool maxDownSpeedReached;

    private void Awake()
    {
        instance = this;

        _rb = GetComponent<Rigidbody2D>();
        _sprite = playerRenderer.GetComponent<SpriteRenderer>();
        _Anim = GetComponent<Animator>();

        _inputActions = new Controls();
    }

    private void OnEnable()
    {
        _moveAction = _inputActions.Gameplay.Move;
        _moveAction.Enable();

        _jumpAction = _inputActions.Gameplay.Jump;
        _jumpAction.Enable();
        _jumpAction.performed += Jump;
        _jumpAction.canceled += JumpCancel;

        _dashAction = _inputActions.Gameplay.Dash;
        _dashAction.Enable();
        _dashAction.performed += Dash;

        _grabAction = _inputActions.Gameplay.Grab;
        _grabAction.Enable();
        _grabAction.started += StartGrab;
        _grabAction.canceled += EndGrab;
    }

    private void OnDisable()
    {
        _moveAction.Disable();

        _jumpAction.Disable();

        _dashAction.Disable();

        _grabAction.Disable();
    }

    private void Update()
    {
        _direction = _moveAction.ReadValue<Vector2>();

        moveDir = _direction.x;
        if (Mathf.Abs(moveDir) < moveDeadZone) moveDir = 0;

        if (!isGrabbed)
        {
            if (moveDir < 0)
            {
                _sprite.flipX = true;
            }
            else if (moveDir > 0)
            {
                _sprite.flipX = false;
            }
        }

        if (_direction.x < 0.3f && _direction.x > -0.3f) dashDir.x = 0;
        else dashDir.x = Mathf.Sign(_direction.x);
        if (_direction.y < 0.3f && _direction.y > -0.3f) dashDir.y = 0;
        else dashDir.y = Mathf.Sign(_direction.y);
        if (dashDir == Vector2.zero) dashDir = (_sprite.flipX) ? Vector2.left : Vector2.right;

        if (lastGroundTime >= -coyoteTime)
        {
            _Anim.SetFloat("Speed", Mathf.Abs(_rb.velocity.x)/speed);
        }


        //hairs
        //movements
        Vector2 hairOffset;
        int inversSpriteDir = (_sprite.flipX) ? 1 : -1;

        if (isDashing)
        {
            HairAnchor.instance.lerpSpeed = dashHairLerpSpeed;
            HairAnchor.instance.partOffsetX = Mathf.Clamp(maxHairOffsetX * -dashDir.normalized.x, -maxHairOffsetX, maxHairOffsetX);
            HairAnchor.instance.partOffsetY = Mathf.Clamp(maxHairOffsetY * -dashDir.normalized.y, -maxHairOffsetY, maxHairOffsetY);
        }
        else
        {
            HairAnchor.instance.lerpSpeed = normalHairLerpSpeed;

            if (lastGroundTime < 0)
            {
                if (isGrabbed)
                {
                    HairAnchor.instance.partOffsetX = baseHairOffset.x * inversSpriteDir;
                    HairAnchor.instance.partOffsetY = baseHairOffset.y;
                }
                else
                {
                    hairOffset = _rb.velocity / new Vector2(speed, maxDownSpeed);
                    hairOffset *= new Vector2(Mathf.Abs(hairOffset.normalized.x), Mathf.Abs(hairOffset.normalized.y));
                    HairAnchor.instance.partOffsetX = maxHairOffsetX * -hairOffset.x;
                    HairAnchor.instance.partOffsetY = maxHairOffsetY * -hairOffset.y;
                }
            }
            else
            {
                float hairXOffset = inversSpriteDir * Mathf.Clamp(maxHairOffsetX * (baseHairOffset.x + Mathf.Abs(_rb.velocity.x/speed)), baseHairOffset.x, maxHairOffsetX);
                hairOffset = new(hairXOffset, baseHairOffset.y);

                HairAnchor.instance.partOffsetX = hairOffset.x;
                HairAnchor.instance.partOffsetY = hairOffset.y;
            }
        }

        //color
        if (isDashing)
        {
            HairAnchor.instance.hairColor = whiteHairColor;
        } 
        else
        {
            if (canDash)
            {
                HairAnchor.instance.hairColor = redHairColor;
            }
            else
            {
                HairAnchor.instance.hairColor = blueHairColor;
            }
        }
        
    }

    private void FixedUpdate()
    {   
        lastGroundTime -= Time.fixedDeltaTime;
        lastJumpTime -= Time.fixedDeltaTime;
        jumpInputBuffer -= Time.fixedDeltaTime;
        lastDashTime -= Time.fixedDeltaTime;
        lastWallJumpTimer -= Time.fixedDeltaTime;

        wallDir = Vector2.zero;
        if (CheckWall(1)) wallDir.y = 1;
        if (CheckWall(-1)) wallDir.x = 1;

        if (wallDir != Vector2.zero) lastWallDir = wallDir;

        //End Dash
        if(lastDashTime <= -dashingTime && isDashing)
        {
            isDashing = false;
        }

        //Checkground
        if (CheckGround())
        {
            lastGroundTime = 0;
            hasQuittedGround = false;
            endurance = maxEndurance;
            if (lastDashTime <= -dashCooldown) canDash = true;
            if (lastJumpTime < -0.1f && !isGrabbed && !isDashing && !isJumping) _Anim.Play("Move");

            if (isJumping && lastJumpTime < -0.1f)
            {
                isJumping = false;
                _Anim.Play("Move");

                if (maxDownSpeedReached)
                {
                    maxDownSpeedReached = false;
                }

                if (jumpInputBuffer >= 0)
                {
                    Jump(new InputAction.CallbackContext());
                    jumpInputBuffer = 0;
                }
            }
        }
        else
        {
            hasQuittedGround = true;
            if (!isGrabbed && !isDashing && !isJumping)
            {
                _Anim.Play("Fall");
            }
        }

        //Begin Climb
        if(!isGrabbed && grabingAction && endurance > 0 && ((wallDir.y == 1 && !_sprite.flipX) || (wallDir.x == 1 && _sprite.flipX)) && lastWallJumpTimer < -0.2f)
        {
            isGrabbed = true;
            isDashing = false;

            int wallDirection = (wallDir.x == 1) ? -1 : 1;
            RaycastHit2D wallHit = Physics2D.Raycast(transform.position - new Vector3(0, checkGroundOffset.y, 0), new Vector2(wallDirection, 0), 0.9f, groundLayers);
            if (!wallHit) wallHit = Physics2D.Raycast(transform.position, new Vector2(wallDirection, 0), 0.9f, groundLayers);
            if (!wallHit) wallHit = Physics2D.Raycast(transform.position + new Vector3(0, checkGroundOffset.y, 0), new Vector2(wallDirection, 0), 0.9f, groundLayers);

            if (wallHit) transform.position = new Vector3(wallHit.point.x - (playerRenderer.transform.localScale.x/2) * wallDirection, transform.position.y, transform.position.z);

            _rb.velocity = Vector2.zero;
            _rb.gravityScale = 0;

            _Anim.Play("Climb");
            _Anim.SetFloat("UpSpeed", 0);
        }

        //Climb
        if (isGrabbed)
        {
            endurance -= Time.fixedDeltaTime;

            _rb.velocity = Vector2.up * dashDir.y * climbSpeed;

            if(dashDir.y != 0)
            {
                _Anim.SetFloat("UpSpeed", 1);
            }
            else
            {
                _Anim.SetFloat("UpSpeed", 0);
            }

            if (!grabingAction || endurance <= 0)
            {
                isGrabbed = false;
            }
            else if (wallDir == Vector2.zero)
            {
                isGrabbed = false;
                if (CheckWallBelow())
                {
                    _rb.AddForce(toTopOfWall * new Vector2(0, 1), ForceMode2D.Impulse);
                    StartCoroutine(toTopWallX(0.15f, (lastWallDir.x == 1) ? -1 : 1));
                }
            }
        }


        // Basic Movement
        if (!isDashing && !isGrabbed) 
        {
            float targetSpeed = (moveDir == 0) ? 0 : Mathf.Sign(moveDir) * speed;
            float speedDiff = targetSpeed - _rb.velocity.x;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : decceleration;
            float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velPower) * Mathf.Sign(speedDiff);
            _rb.AddForce(movement * Vector2.right);

            if (lastGroundTime >= 0 && moveDir == 0)
            {
                float amount = Mathf.Min(Mathf.Abs(_rb.velocity.x), Mathf.Abs(frictionAmount)) * Mathf.Sign(_rb.velocity.x);
                _rb.AddForce(Vector2.right * -amount, ForceMode2D.Impulse);
            }

            if (_rb.velocity.y < -maxDownSpeed)
            {
                _rb.velocity = new Vector2(_rb.velocity.x, -maxDownSpeed);
                maxDownSpeedReached = true;
            }

            if (_rb.velocity.y < fallGravityTreshold)
            {
                _rb.gravityScale = gravityScale * fallGravityMultiplier;
            }
            else _rb.gravityScale = gravityScale;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("orb"))
        {
            if (collision.GetComponent<DashOrb>().active)
            {
                collision.GetComponent<DashOrb>().Use();
                endurance = maxEndurance;
                canDash = true;
            }
        }
        else if (collision.CompareTag("checkpoint"))
        {
            currentCheckpoint = collision.gameObject;
        }
        else if (collision.CompareTag("damage"))
        {
            if (currentCheckpoint != null)
            {
                transform.position = currentCheckpoint.transform.position;
            }
        }
    }

    public bool CheckGround()
    {
        return Physics2D.OverlapBox(transform.position + (Vector3) checkGroundOffset, checkGroundSize, 0, groundLayers);
    }

    public bool CheckWall(int dir)
    {
        return Physics2D.OverlapBox(transform.position + (Vector3) (checkWallOffset * new Vector2(dir, 1)), checkWallSize, 0, groundLayers);
    }

    public bool CheckWallBelow()
    {
        return Physics2D.OverlapBox(transform.position + (Vector3)checkWallBelowOffset, checkWallBelowSize, 0, groundLayers);
    }

    public void spring()
    {
        endurance = maxEndurance;
        canDash = true;
    }

    private IEnumerator toTopWallX(float time, int xpropulsion)
    {
        yield return new WaitForSeconds(time);
        _rb.AddForce(toTopOfWall * new Vector2(xpropulsion, 0), ForceMode2D.Impulse);
    }

    private void Jump(InputAction.CallbackContext context)
    {
        float XpropulsionForWall = (wallDir.x == 1) ? 1 : -1;
        float Xpropulsion = (_sprite.flipX) ? -1 : 1;
        Vector2 wallForce = wallJumpForce * new Vector2(XpropulsionForWall, 1);
        Vector2 force = jumpForce * Vector2.up;

        if (isDashing && CheckWallBelow() && !isJumping)
        {
            force = superDashForce * new Vector2(Xpropulsion, 1);
            _rb.velocity = Vector2.zero;
            _rb.AddForce(force, ForceMode2D.Impulse);
            _Anim.Play("Jump");
        }
        else if (lastGroundTime >= -coyoteTime && !isJumping && !(isDashing && hasQuittedGround) && !isGrabbed) // regular jump
        {
            isJumping = true;
            lastJumpTime = 0;
            _rb.velocity *= Vector2.right;
            _rb.AddForce(force, ForceMode2D.Impulse);
            _Anim.Play("Jump");
        } 
        else if (isGrabbed) // grab into jump
        {
            endurance -= enduranceLossWhenJump;
            _rb.gravityScale = gravityScale;
            lastWallJumpTimer = 0;
            isGrabbed = false;
            _Anim.Play("Jump");

            if (dashDir.x == XpropulsionForWall)
            {
                _sprite.flipX = !_sprite.flipX;
                force = wallForce;
            }
            else if (dashDir.y != 0 || Mathf.Sign(dashDir.x) != XpropulsionForWall)
            {
                _rb.velocity = Vector2.zero;
            }
            _rb.AddForce(force, ForceMode2D.Impulse);
        }
        else if (wallDir != Vector2.zero /*&& lastDashTime < -0.22f*/) // wall jump
        {
            if (isDashing)
            {
                wallForce = wallBounceForce * new Vector2(XpropulsionForWall, 1);
                isDashing = false;
            }
            else
            {

            }
            _Anim.Play("Jump");
            _sprite.flipX = !_sprite.flipX;
            _rb.velocity *= Vector2.right;
            _rb.AddForce(wallForce, ForceMode2D.Impulse);
        }
        else // buffer
        {
            jumpInputBuffer = jumpInputBufferTime;
        }
    }

    private void JumpCancel(InputAction.CallbackContext context)
    {
        if (isJumping && _rb.velocity.y > 0)
        {
            _rb.AddForce((1 - jumpCutMultiplier) * _rb.velocity.y * Vector2.down, ForceMode2D.Impulse);
        }
    }

    private void Dash(InputAction.CallbackContext context)
    {
        if (canDash)
        {
            if (dashDir.x != 0 && dashDir.y != 0)
            {
                _rb.velocity = dashingPower / 1.5f * dashDir * new Vector2(1.2f, 1);
            }
            else
            {
                _rb.velocity = dashingPower * dashDir * new Vector2(1.2f, 1);
            }

            _Anim.Play("Dash");
            lastGroundTime = coyoteTime * -1.1f;
            isDashing = true;
            lastDashTime = 0;
            _rb.gravityScale = 0;
            canDash = false;
        }
    }

    private void StartGrab(InputAction.CallbackContext context)
    {
        grabingAction = true;
    }

    private void EndGrab(InputAction.CallbackContext context)
    {
        grabingAction = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (CheckGround()) Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + (Vector3)checkGroundOffset, checkGroundSize);

        wallDir = Vector2.zero;
        if (CheckWall(1)) wallDir.y = 1;
        if (CheckWall(-1)) wallDir.x = 1;

        if (wallDir.y == 1) Gizmos.color = Color.green; 
        else Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + (Vector3)(checkWallOffset * new Vector2(1, 1)), checkWallSize);
        if (wallDir.x == 1) Gizmos.color = Color.green; 
        else Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + (Vector3)(checkWallOffset * new Vector2(-1, 1)), checkWallSize);

        Gizmos.color = Color.red;
        if (CheckWallBelow()) Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + (Vector3)checkWallBelowOffset, checkWallBelowSize);
    }
}
