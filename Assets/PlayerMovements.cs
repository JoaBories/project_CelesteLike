using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovements : MonoBehaviour
{
    public static PlayerMovements instance;

    private Controls _inputActions;

    private InputAction _moveAction;
    public static float moveDir;

    private InputAction _jumpAction;


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

    [Header("Checks")]
    [SerializeField] private Transform checkGroundPoint;
    [SerializeField] private Vector2 checkGroundSize;
    [SerializeField] private LayerMask groundLayers;

    private Rigidbody2D _rb;

    private float lastGroundTime;
    private float lastJumpTime;
    private float jumpInputBuffer;

    private bool isJumping;

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
    }

    private void OnDisable()
    {
        _moveAction.Disable();
    }

    private void Update()
    {
        moveDir = _moveAction.ReadValue<float>();
        if (Mathf.Abs(moveDir) < moveDeadZone) moveDir = 0;
    }

    private void FixedUpdate()
    {
        lastGroundTime -= Time.fixedDeltaTime;
        lastJumpTime -= Time.fixedDeltaTime;
        jumpInputBuffer -= Time.fixedDeltaTime;

        if (CheckGround())
        {
            lastGroundTime = 0;
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

        float targetSpeed = moveDir * speed;
        float speedDiff = targetSpeed - _rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : decceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velPower) * Mathf.Sign(speedDiff);
        _rb.AddForce(movement * Vector2.right);

        if (lastGroundTime >= 0 && moveDir == 0)
        {
            float amount = Mathf.Min(Mathf.Abs(_rb.velocity.x), Mathf.Abs(frictionAmount)) * Mathf.Sign(_rb.velocity.x);
            _rb.AddForce(Vector2.right * -amount, ForceMode2D.Impulse);
        }

        if (_rb.velocity.y < 0.5) _rb.gravityScale = gravityScale * fallGravityMultiplier;
        else _rb.gravityScale = gravityScale;
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
            _rb.AddForce(Vector2.down * _rb.velocity.y * (1 - jumpCutMultiplier), ForceMode2D.Impulse);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = (CheckGround()) ? Color.green : Color.red;
        Gizmos.DrawWireCube(checkGroundPoint.position, checkGroundSize);
    }
}
