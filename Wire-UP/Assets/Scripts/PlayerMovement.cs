using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using StarterAssets;
using UnityEngine.InputSystem.XR;
using static UnityEngine.UI.Image;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Current movement speed")]
    [SerializeField] private float moveSpeed; // ���� �̵� �ӵ�

    [Tooltip("Walking speed")]
    public float walkSpeed; // �ȱ� �ӵ�   

    [Tooltip("Sprinting speed")]
    public float sprintSpeed; // �޸��� �ӵ�

    [Tooltip("Swing speed")]
    public float swingSpeed; // ���� �ӵ�
    public bool isSwingEnded = false; // ���� ���� ����(������ �����Ͽ� ���� �׼��� ������ �� ������ ������ ����)

    private float _targetRotation = 0.0f;
    private float _rotationVelocity;

    [SerializeField]
    private float _verticalVelocity; // ���� �ӵ�

    private float _maxFallVelocity = -15.0f; // �ִ� ���� �ӵ�

    [Tooltip("Drag applied when grounded")]
    public float groundDrag; // ���鿡�� ����Ǵ� ������

    [Header("Jumping Settings")]
    [Tooltip("Height of the jump")]
    public float JumpHeight = 1.2f; // ���� ����

    [Tooltip("Cooldown time between jumps")]
    public float jumpCooldown; // ���� ��ٿ� �ð�

    [Tooltip("Multiplier for movement speed in air")]
    public float airMultiplier; // ���߿����� �̵� �ӵ� ����

    [Tooltip("Is the player ready to jump")]
    private bool readyToJump; // ���� �غ� ����

    [Tooltip("Gravity applied to the player")]
    public float Gravity = -15.0f; // �߷� ��

    private bool _currentJumpInput;

    /*
    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    */

    [Header("Ground Check")]
    [Tooltip("Transform representing the player's center")]
    public Transform playerCenter; // �÷��̾��� �߽� ��ġ

    [Tooltip("Height of the player")]
    public float playerHeight; // �÷��̾��� ����

    [Tooltip("Layers considered as ground")]
    public LayerMask GroundLayers; // �������� ������ ���̾�

    [SerializeField]
    [Tooltip("Is the player grounded")]
    public bool grounded; // �÷��̾ ���鿡 �ִ��� ����

    [Tooltip("Size of the box used for ground checking")]
    public float groundCheckBoxSize = 0.1f; // ���� üũ�� ���Ǵ� �ڽ� ũ��

    [Tooltip("Time before being able to jump again after landing")]
    public float JumpTimeout = 0.50f; // ���� �� �ٽ� ������ �� ���� �������� �ð�

    [Tooltip("Time before considering the player in free fall")]
    public float FallTimeout = 0.15f; // ���� ���·� ��ȯ�Ǳ������ �ð�

    [Header("Slope Handling")]
    [Tooltip("Maximum angle of slope player can walk on")]
    public float maxSlopeAngle; // �÷��̾ ���� �� �ִ� �ִ� ��� ����

    private RaycastHit slopeHit; // ���� ������ ��� ����

    [SerializeField]
    [Tooltip("Is the player exiting a slope")]
    private bool exitingSlope; // ���鿡�� ����� ������ ����

    [Tooltip("Is the player on a slope")]
    public bool isSlope; // ���� ���� ���� �ִ��� ����

    [Tooltip("Is the player moving up the slope")]
    public bool upDirection; // ������ �ö󰡴� ������ ����

    [Tooltip("Is the player moving down the slope")]
    public bool downDirection; // ������ �������� ������ ����

    [Header("Audio")]
    [Tooltip("Audio clip for landing sound")]
    public AudioClip LandingAudioClip; // ���� ���� Ŭ��

    [Tooltip("Audio clips for footstep sounds")]
    public AudioClip[] FootstepAudioClips; // �߼Ҹ� ���� Ŭ�� �迭

    [Tooltip("Volume of footstep sounds")]
    [Range(0, 1)]
    public float FootstepAudioVolume = 0.5f; // �߼Ҹ� ����

    [Header("Camera Settings")]
    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f; // ȸ�� �ӵ� ������ �ð�

    [Tooltip("Maximum upward camera angle")]
    public float TopClamp = 70.0f; // ī�޶��� �ִ� ���� ����

    [Tooltip("Maximum downward camera angle")]
    public float BottomClamp = -30.0f; // ī�޶��� �ִ� �Ʒ��� ����

    [Tooltip("Additional rotation applied to camera")]
    public float CameraAngleOverride = 0.0f; // ī�޶� �߰��� ����Ǵ� ����

    [Tooltip("Locks the camera position")]
    public bool LockCameraPosition = false; // ī�޶� ��ġ ���� ����

    [Header("Cinemachine Settings")]
    [Tooltip("Reference to the Cinemachine camera target")]
    public GameObject CinemachineCameraTarget; // �ó׸ӽ� ī�޶� Ÿ��

    private float _cinemachineTargetYaw; // ī�޶��� Yaw ��
    private float _cinemachineTargetPitch; // ī�޶��� Pitch ��

    [Header("Camera Effects")]
    //public PlayerCam cam;

    [Tooltip("Field of view when grappling")]
    public float grappleFov = 95f; // �׷� �� FOV

    //public Transform orientation;

    //float horizontalInput;
    //float verticalInput;

    [Header("Movement Direction")]
    private Vector2 _currentMoveInput;
    [Tooltip("Direction of player movement")]
    private Vector3 moveDirection; // �÷��̾� �̵� ����

    [Header("Player State")]
    [Tooltip("Current movement state of the player")]
    public MovementState state; // �÷��̾��� ���� ����

    public enum MovementState
    {
        freeze,
        grappling,
        swinging,
        walking,
        sprinting,
        //crouching,
        mantling,
        air
    }

    [Tooltip("Is player movement frozen")]
    public bool freeze; // �÷��̾� �������� ������� ����

    [Tooltip("Is the player currently grappling")]
    public bool activeGrapple; // ���� �׷� ������ ����

    [Tooltip("Is the player currently swinging")]
    public bool swinging; // ���� ���� ������ ����

    // Ÿ�Ӿƿ� ��ŸŸ��
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    [Header("Animation IDs")]
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;
    private int _animIDMantle;

    [Header("Components")]
    private PlayerInput _playerInput;
    private Animator _animator;
    private Rigidbody _rigidbody;
    private PlayerNewInput _input;
    private GameObject _mainCamera;

    private const float _threshold = 0.01f; // �Է� ���� �Ӱ谪

    private bool _hasAnimator;

    private void Awake()
    {
        // ���� ī�޶� ����
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.freezeRotation = true; // ȸ�� ����

        readyToJump = true;

        _hasAnimator = TryGetComponent(out _animator);
        _input = GetComponent<PlayerNewInput>();

        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        //startYScale = transform.localScale.y;

        AssignAnimationIDs(); // �ִϸ��̼� ID �Ҵ�

        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    /// <summary>
    /// �÷��̾ ���鿡 �ִ��� üũ
    /// </summary>
    public void IsGrounded()
    {
        Vector3 boxSize = new Vector3(0.1f, 0.1f, 0.1f);
        grounded = Physics.CheckBox(transform.position, boxSize, Quaternion.identity, GroundLayers);

        // �ٴڿ� ����� �� ���� ���� ���� �ʱ�ȭ
        if (grounded && isSwingEnded)
        {
            isSwingEnded = false;
        }
    }

    private void Update()
    {
        PlayerInput(); // �÷��̾� �Է� ó��
        // �ӵ� ���� �� ���� ó��
        SpeedControl();
        StateHandler();

        // ���鿡 ���� �� ������ ����
        if (grounded && !activeGrapple)
            _rigidbody.drag = groundDrag;
        else
            _rigidbody.drag = 0;

        if (_animator)
        {
            _animator.SetBool(_animIDGrounded, grounded);
        }
    }

    private void FixedUpdate()
    {
        IsGrounded(); // ���� üũ
        CheckSlope(); // ���� üũ

        if (_isMantling)
        {
            //Debug.Log("��Ʋ ��");
            return;
        }

        MovePlayer(); // �÷��̾� �̵� ó��
        Jump(); // ���� ó��
    }

    private void LateUpdate()
    {
        CameraRotation(); // ī�޶� ȸ�� ó��
    }

    /// <summary>
    /// �ִϸ��̼� ID �Ҵ�
    /// </summary>
    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        _animIDMantle = Animator.StringToHash("Mantle");
    }

    private void PlayerInput()
    {
        _currentMoveInput = _input.move;
        _currentJumpInput = _input.jump;

        /*
        // when to jump
        if (_input.jump && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();

            //Invoke(nameof(ResetJump), jumpCooldown);
        }
        */
    }

    /// <summary>
    /// �÷��̾��� ���¸� ����
    /// </summary>
    private void StateHandler()
    {
        // Mode - Freeze
        if (freeze)
        {
            state = MovementState.freeze;
            moveSpeed = 0;
            _rigidbody.velocity = Vector3.zero;
        }

        // Mode - Grappling
        else if (activeGrapple)
        {
            state = MovementState.grappling;
            moveSpeed = sprintSpeed;
        }

        // Mode - Swinging
        else if (swinging && !grounded)
        {
            state = MovementState.swinging;
            moveSpeed = swingSpeed;
        }

        // Mode - Mantling
        else if (_input.mantle)
        {
            state = MovementState.mantling;
        }

        // Mode - Sprinting
        else if (grounded && _input.sprint)
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }

        // Mode - Walking
        else if (grounded)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }

        // Mode - Air
        else
        {
            state = MovementState.air;
            if (_input.sprint)
                moveSpeed = sprintSpeed * airMultiplier;
            else
                moveSpeed = walkSpeed * airMultiplier;
        }
    }

    /// <summary>
    /// ���� üũ �� �߷� ó��
    /// </summary>
    private void CheckSlope()
    {
        // ���� ���� ���� �� �߷� ����
        if (OnSlope())
        {
            Vector3 slopeMoveDirection = GetSlopeMoveDirection();

            if (slopeMoveDirection.y >= 0)
            {
                upDirection = true;
                downDirection = false;
            }
            else
            {
                downDirection = true;
                upDirection = false;
            }

            isSlope = true;
            _rigidbody.useGravity = false;
        }
        else
        {
            isSlope = false;

            if (!_isMantling)
                _rigidbody.useGravity = true;
        }
    }

    /// <summary>
    /// �÷��̾� �̵� ó��
    /// </summary>
    private void MovePlayer()
    {
        if (activeGrapple) return;

        if (swinging && !grounded) return;

        // �̵� �Է��� ���� ��
        if (_currentMoveInput == Vector2.zero)
        {
            moveSpeed = 0.0f;
            // ���� �̲����� ����
            if (isSlope && downDirection)
            {
                _verticalVelocity = 0f;
            }
        }

        float inputMagnitude = _input.analogMovement ? _currentMoveInput.magnitude : 1f;
        Vector3 inputDirection = new Vector3(_currentMoveInput.x, 0.0f, _currentMoveInput.y).normalized;
        moveDirection = Quaternion.Euler(0, _mainCamera.transform.eulerAngles.y, 0) * inputDirection;

        // �÷��̾� ȸ��
        if (_currentMoveInput != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        // --- ���� �κ� ���� ---
        // ���� ���� �� ���� �̵��� �ణ ����ϴ� ����
        // --- ������ �κ�: ���� ���� �� ���� �̵��� "AddForce"�� ó�� ---
        if (isSwingEnded)
        {
            if (!grounded)
            {
                // ���� ����(velocity)�� �״�� �ΰ�, �Է� �������θ� '�ణ' Force�� ����
                Vector3 forceDirection = new Vector3(_currentMoveInput.x, 0, _currentMoveInput.y);
                forceDirection = Quaternion.Euler(0.0f, _mainCamera.transform.eulerAngles.y, 0.0f)
                                 * forceDirection.normalized;

                // �̵����� ��� ���� ���� ������ ����
                float reducedAirControlMultiplier = 0.00001f;
                Vector3 finalForce = forceDirection * (moveSpeed * inputMagnitude * reducedAirControlMultiplier);

                // ĳ���Ϳ� ���� ���� ���߿��� ���ݸ� �����̵���
                _rigidbody.AddForce(finalForce * Time.deltaTime, ForceMode.Force);

                // ���� + �߷� �״�� ���� �� velocity ���� ���� X
                // �Լ� ����
                return;
            }
            else
            {
                // �ٴڿ� ��Ҵٸ� ���Ŀ��� �⺻ ���� ����
                // (isSwingEnded�� �ʱ�ȭ�� ������ ������ �ִٸ� OK)
            }
        }
        // --- ���� �κ� �� ---

        // ���� ������ �̵�
        if (OnSlope() && !exitingSlope)
        {
            _rigidbody.velocity = GetSlopeMoveDirection() * moveSpeed * inputMagnitude;

            if (downDirection)
            {
                _verticalVelocity = -3f;
            }
        }
        // ���� ������ �̵�
        else if (grounded)
        {
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            if (!isSlope)
                _verticalVelocity = 0f;

            _rigidbody.velocity = targetDirection.normalized * (moveSpeed * inputMagnitude) + new Vector3(0.0f, _rigidbody.velocity.y, 0.0f);
        }
        // ���� �̵�
        else if (!grounded)
        {
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
            _rigidbody.velocity = targetDirection.normalized * (moveSpeed * inputMagnitude) + new Vector3(0.0f, _rigidbody.velocity.y, 0.0f) * airMultiplier;
        }

        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, moveSpeed);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }
    }

    /// <summary>
    /// �ӵ� ���� ó��
    /// </summary>
    private void SpeedControl()
    {
        if (activeGrapple) return;

        //if (!grounded) return;

        // ���� �������� �ӵ� ����
        if (OnSlope() && !exitingSlope)
        {
            if (_rigidbody.velocity.magnitude > moveSpeed)
                _rigidbody.velocity = _rigidbody.velocity.normalized * moveSpeed;
        }
        // �����̳� ���߿����� �ӵ� ����
        else
        {
            Vector3 flatVel = new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);

            // �ӵ� ���� ����
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                _rigidbody.velocity = new Vector3(limitedVel.x, _rigidbody.velocity.y, limitedVel.z);
            }
        }
    }

    /// <summary>
    /// ���� ó��
    /// </summary>
    private void Jump()
    {
        if (swinging && !grounded) return;

        // ���� ���� �� �ٴڿ� ��� ������ ���� ��Ȱ��ȭ
        if (isSwingEnded) return;

        if (grounded)
        {
            // ���� Ÿ�Ӿƿ� ����
            _fallTimeoutDelta = FallTimeout;

            // �ִϸ����� ������Ʈ
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }
        
            // ���� �ӵ� ����ȭ
            if (_verticalVelocity < 0.0f && !downDirection)
            {
                _verticalVelocity = 0f;
            }

            // ���� �Է� ó��
            if (_currentJumpInput && readyToJump && grounded)
            {
                if (isSlope)
                    exitingSlope = true;
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                readyToJump = false; // ������ ����
                _jumpTimeoutDelta = JumpTimeout; // ��ٿ� Ÿ�̸� �ʱ�ȭ

                // �ִϸ����� ������Ʈ
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // ���� Ÿ�Ӿƿ� ����
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // ���� Ÿ�Ӿƿ� ����
            _jumpTimeoutDelta = JumpTimeout;

            // ���� Ÿ�Ӿƿ� ���� �� �ִϸ����� ������Ʈ
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }

            // ���߿��� ���� �Է� ����
            _input.jump = false;
        }

        // ��ٿ� Ÿ�̸� �Ϸ� �� ���� ����
        if (!readyToJump)
        {
            if (_jumpTimeoutDelta <= 0.0f)
                ResetJump();
        }

        // �߷� ����
        if (_verticalVelocity > _maxFallVelocity && !grounded)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }

        // �ӵ� ����
        _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, _verticalVelocity, _rigidbody.velocity.z);
    }

    /// <summary>
    /// ���� ���� ����
    /// </summary>
    private void ResetJump()
    {
        _currentJumpInput = false;
        readyToJump = true;
        exitingSlope = false;
    }

    /// <summary>
    /// ī�޶� ȸ�� ó��
    /// </summary>
    private void CameraRotation()
    {
        if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            float deltaTimeMultiplier = 1.0f;
            _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
    }

    /// <summary>
    /// ������ �ּ�/�ִ� ������ Ŭ����
    /// </summary>
    /// <param name="angle">���� ����</param>
    /// <param name="min">�ּ� ��</param>
    /// <param name="max">�ִ� ��</param>
    /// <returns>Ŭ������ ����</returns>
    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }

    /// <summary>
    /// ���� ���� �ִ��� üũ
    /// </summary>
    /// <returns>���� ���� �ִ��� ����</returns>
    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, 0.2f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    /// <summary>
    /// ���鿡���� �̵� ���� ���
    /// </summary>
    /// <returns>���� �̵� ����</returns>
    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    /// <summary>
    /// ����׿� ����� �׸���
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position, new Vector3(0.2f, 0.1f, 0.2f));
    }

    /// <summary>
    /// �߼Ҹ� ���
    /// </summary>
    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.position, FootstepAudioVolume);
            }
        }
    }

    /// <summary>
    /// ���� ���� ���
    /// </summary>
    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.position, FootstepAudioVolume);
        }
    }

    /*
    #region Text & Debugging

    public TextMeshProUGUI text_speed;
    public TextMeshProUGUI text_mode;
    private void TextStuff()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (OnSlope())
            text_speed.SetText("Speed: " + Round(rb.velocity.magnitude, 1) + " / " + Round(moveSpeed, 1));

        else
            text_speed.SetText("Speed: " + Round(flatVel.magnitude, 1) + " / " + Round(moveSpeed, 1));

        text_mode.SetText(state.ToString());
    }


    public static float Round(float value, int digits)
    {
        float mult = Mathf.Pow(10.0f, (float)digits);
        return Mathf.Round(value * mult) / mult;
    }

    #endregion
    */

    /// <summary>
    /// ��Ʋ ���� ���� �� ���
    /// </summary>
    [Header("Mantle Settings")]
    private bool _isMantling = false; // ��Ʋ ������ ����

    [Tooltip("Layer mask for mantleable surfaces")]
    public LayerMask mantleLayerMask;  // ��Ʋ ������ ���̾� ����

    [Tooltip("Height from which the forward raycast starts")]
    public float rayHeight = 0.9f;  // Ʈ���̽� ���� ����

    [Tooltip("Maximum distance for forward reach")]
    public float maxReachDistance = 0.5f;  // ���� Ʈ���̽� �ִ� �Ÿ�

    [Tooltip("Maximum height of ledge for mantle")]
    public float maxLedgeHeight = 0.9f;  // ��Ʋ ������ ��ֹ� �ִ� ����

    private Vector3 targetMantlePosition; // ��Ʋ�� ��ǥ ��ġ

    /// <summary>
    /// ��Ʋ ���� ���� üũ
    /// </summary>
    /// <returns>��Ʋ ���� ����</returns>
    public bool CanPerformMantle()
    {
        if (_isMantling)
        {
            Debug.Log("�̹� ��Ʋ�ϴ� ��");
            return false; // �̹� ��Ʋ ���̸� ��Ʋ �Ұ�
        }

        // 1. ���� Ʈ���̽�: ĳ������ �߽ɿ��� �ణ �������� �����ؼ� ��ֹ� ����
        Vector3 rayStartCenter = transform.position + transform.forward * -0.1f + Vector3.up * rayHeight;
        Vector3 rayDirection = transform.forward;
        float verticalOffset = 0.1f;

        // �߽�, ��, �Ʒ� ������ ���� ���� ���
        Vector3 rayStartUpper = rayStartCenter + Vector3.up * verticalOffset;
        Vector3 rayStartLower = rayStartCenter - Vector3.up * verticalOffset;

        Debug.DrawRay(rayStartCenter, rayDirection * maxReachDistance, Color.red, 0.5f);
        Debug.DrawRay(rayStartUpper, rayDirection * maxReachDistance, Color.blue, 0.5f);
        Debug.DrawRay(rayStartLower, rayDirection * maxReachDistance, Color.blue, 0.5f);

        bool hitDetected = false;
        RaycastHit hit;

        // �߽�, ��, �Ʒ� ���� �� �ϳ��� �浹�ϸ� ��Ʈ�� ����
        if (Physics.Raycast(rayStartCenter, rayDirection, out hit, maxReachDistance, mantleLayerMask) ||
            Physics.Raycast(rayStartUpper, rayDirection, out hit, maxReachDistance, mantleLayerMask) ||
            Physics.Raycast(rayStartLower, rayDirection, out hit, maxReachDistance, mantleLayerMask))
        {
            hitDetected = true;
        }

        if (hitDetected)
        {
            // 2. �浹 �������� ���� �̵��� �� �Ʒ� �������� Ʈ���̽�

            Vector3 downwardRayStart = hit.point + Vector3.up * maxLedgeHeight + transform.forward * 0.2f;

            Debug.DrawRay(downwardRayStart, Vector3.down * maxLedgeHeight, Color.green, 0.5f);
            if (Physics.Raycast(downwardRayStart, Vector3.down, out RaycastHit downwardHit, maxLedgeHeight))
            {
                // 3. ǥ���� �������� Ȯ��
                if (downwardHit.normal.y > 0.6f)
                {
                    // 4. ĸ�� �浹 �˻�� ����� ������ �ִ��� Ȯ��(���� ����)
                    Vector3 capsulePosition = downwardHit.point;
                    float capsuleHeight = 1.8f;
                    float capsuleRadius = 0.2f;

                    bool hasRoom = !Physics.CheckCapsule(
                        capsulePosition + Vector3.up * capsuleRadius,
                        capsulePosition + Vector3.up * (capsuleHeight - capsuleRadius),
                        0.05f,
                        mantleLayerMask
                    );

                    if (hasRoom)
                    {
                        targetMantlePosition = downwardHit.point;
                        return true; // ��Ʋ ����
                    }
                    /*
                    ���� ĸ�� �浹 �˻� ������ ��Ʋ ������ ��� �����Ѵٸ� �Ʒ� �ڵ�� ��ü
                    targetMantlePosition = downwardHit.point;
                    return true;
                    */
                }
            }
        }

        return false; // ��Ʋ �Ұ�
    }

    /// <summary>
    /// ��Ʋ ���� ����
    /// </summary>
    public void StartMantle()
    {
        Debug.Log("��Ʋ ����");
        _isMantling = true;
        StartCoroutine(MantleMovement());
    }

    /// <summary>
    /// ��Ʋ �̵� �ڷ�ƾ
    /// </summary>
    System.Collections.IEnumerator MantleMovement()
    {
        Debug.Log("��Ʋ ���� ���� ��");
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDMantle, true);
        }

        _rigidbody.useGravity = false; // �߷� ��Ȱ��ȭ
        _verticalVelocity = 0f;

        Vector3 startPosition = targetMantlePosition + (transform.up * -1f) + (transform.forward * -0.5f); // ���� ��ġ ����
        transform.position = startPosition;

        float elapsedTime = 0f;

        // ù ��° �ܰ�: ���� �̵�
        float firstPhaseDuration = 17f / 30f; // �� 0.567��
        Vector3 firstPhaseTarget = startPosition + transform.up * 1.1f;
        while (elapsedTime < firstPhaseDuration)
        {
            transform.position = Vector3.Lerp(startPosition, firstPhaseTarget, elapsedTime / firstPhaseDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // �� ��° �ܰ�: ������ �̵�
        float secondPhaseDuration = 8f / 30f; // �� 0.267��
        Vector3 secondPhaseTarget = firstPhaseTarget + transform.forward * 0.5f;
        elapsedTime = 0f;
        while (elapsedTime < secondPhaseDuration)
        {
            transform.position = Vector3.Lerp(firstPhaseTarget, secondPhaseTarget, elapsedTime / secondPhaseDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // ���� ��ġ�� ����
        transform.position = targetMantlePosition;

        _isMantling = false;
        _input.MantleInput(false);
        // ��Ʋ �� �ִϸ��̼� ���� ����
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDMantle, false);
        }

        _rigidbody.useGravity = true; // �߷� Ȱ��ȭ
        yield return null;
    }

    public void OnSwingEnd()
    {
        isSwingEnded = true;
    }
}
