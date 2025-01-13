using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovements : MonoBehaviour
{
    public static PlayerMovements instance;

    private Controls _inputActions;

    private InputAction _moveAction;

    public static Vector2 _direction;
    public static float moveDir;

    private InputAction _jumpAction;

    private InputAction _dashAction;


    [Header("Movements Values")]
    [SerializeField] private float moveDeadZone;
    [SerializeField] private float acceleration;
    [SerializeField] private float speed;
    [SerializeField] private float decceleration;
    [SerializeField] private float velPower;
    [SerializeField] private float frictionAmount;
    [SerializeField] private float fallGravityMultiplier;
    [SerializeField] private float gravityScale;

    [Header("Jump")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float coyoteTime;
    [SerializeField] private float jumpCutMultiplier;
    [SerializeField] private float jumpInputBufferTime;

    [Header("Dash")]
    [SerializeField] private float dashingPower;
    [SerializeField] private float dashingTime;
    [SerializeField] private float dashCooldown;

    [Header("Checks")]
    [SerializeField] private Transform checkGroundPoint;
    [SerializeField] private Vector2 checkGroundSize;
    [SerializeField] private LayerMask groundLayers;

    [Header("Rendering")]
    [SerializeField] private GameObject playerRenderer;
    [SerializeField] private TrailRenderer trailRenderer;


    private Rigidbody2D _rb;

    private float lastGroundTime;
    private float lastJumpTime;
    private float lastDashTime;
    private float jumpInputBuffer;

    private bool isJumping;
    private bool isDashing;

    private bool canDash;

    private void Awake()
    {
        instance = this;
        _rb = GetComponent<Rigidbody2D>();

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
    }

    private void OnDisable()
    {
        _moveAction.Disable();
    }

    private void Update()
    {

        _direction = _moveAction.ReadValue<Vector2>();
        moveDir = _direction.x;
        if (Mathf.Abs(moveDir) < moveDeadZone) moveDir = 0;
    }

    private void FixedUpdate()
    {
        lastGroundTime -= Time.fixedDeltaTime;
        lastJumpTime -= Time.fixedDeltaTime;
        jumpInputBuffer -= Time.fixedDeltaTime;
        lastDashTime -= Time.fixedDeltaTime;

        if (CheckGround())
        {
            lastGroundTime = 0;
            if (lastDashTime <= -dashCooldown) canDash = true;
            if (isJumping && lastJumpTime < -0.1f)
            {
                isJumping = false;
                if (jumpInputBuffer >= 0)
                {
                    Jump(new InputAction.CallbackContext());
                    jumpInputBuffer = 0;
                }
            }
        }

        if (!isDashing)
        {
            float targetSpeed = (moveDir == 0) ? 0 : Mathf.Sign(moveDir) * speed;
            float speedDiff = targetSpeed - _rb.velocity.x;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : decceleration;
            float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velPower) * Mathf.Sign(speedDiff);
            _rb.AddForce(movement * Vector2.right);
        }

        if (lastGroundTime >= 0 && moveDir == 0)
        {
            float amount = Mathf.Min(Mathf.Abs(_rb.velocity.x), Mathf.Abs(frictionAmount)) * Mathf.Sign(_rb.velocity.x);
            _rb.AddForce(Vector2.right * -amount, ForceMode2D.Impulse);
        }

        if(lastDashTime <= -dashingTime && isDashing)
        {
            isDashing = false;
        }


        if (!isDashing) 
        {
            if (_rb.velocity.y < 0.5) _rb.gravityScale = gravityScale * fallGravityMultiplier;
            else _rb.gravityScale = gravityScale;
        }
    }

    public bool CheckGround()
    {
        return Physics2D.OverlapBox(checkGroundPoint.position, checkGroundSize, 0, groundLayers);
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (lastGroundTime >= -coyoteTime && !isJumping)
        {
            _rb.AddForce(jumpForce * Vector2.up, ForceMode2D.Impulse);
            lastJumpTime = 0;
            isJumping = true;
        } else
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
            isDashing = true;
            lastDashTime = 0;
            _rb.gravityScale = 0;
            _rb.velocity = dashingPower * _direction;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = (CheckGround()) ? Color.green : Color.red;
        Gizmos.DrawWireCube(checkGroundPoint.position, checkGroundSize);
    }
}
