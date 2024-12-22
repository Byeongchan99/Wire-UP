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
    [SerializeField] private float moveSpeed; // 현재 이동 속도

    [Tooltip("Walking speed")]
    public float walkSpeed; // 걷기 속도   

    [Tooltip("Sprinting speed")]
    public float sprintSpeed; // 달리기 속도

    [Tooltip("Swing speed")]
    public float swingSpeed; // 스윙 속도
    public bool isSwingEnded = false; // 스윙 종료 여부(로프를 연결하여 스윙 액션을 실행한 후 로프를 해제한 상태)

    private float _targetRotation = 0.0f;
    private float _rotationVelocity;

    [SerializeField]
    private float _verticalVelocity; // 수직 속도

    private float _maxFallVelocity = -15.0f; // 최대 낙하 속도

    [Tooltip("Drag applied when grounded")]
    public float groundDrag; // 지면에서 적용되는 마찰력

    [Header("Jumping Settings")]
    [Tooltip("Height of the jump")]
    public float JumpHeight = 1.2f; // 점프 높이

    [Tooltip("Cooldown time between jumps")]
    public float jumpCooldown; // 점프 쿨다운 시간

    [Tooltip("Multiplier for movement speed in air")]
    public float airMultiplier; // 공중에서의 이동 속도 배율

    [Tooltip("Is the player ready to jump")]
    private bool readyToJump; // 점프 준비 상태

    [Tooltip("Gravity applied to the player")]
    public float Gravity = -15.0f; // 중력 값

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
    public Transform playerCenter; // 플레이어의 중심 위치

    [Tooltip("Height of the player")]
    public float playerHeight; // 플레이어의 높이

    [Tooltip("Layers considered as ground")]
    public LayerMask GroundLayers; // 지면으로 간주할 레이어

    [SerializeField]
    [Tooltip("Is the player grounded")]
    public bool grounded; // 플레이어가 지면에 있는지 여부

    [Tooltip("Size of the box used for ground checking")]
    public float groundCheckBoxSize = 0.1f; // 지면 체크에 사용되는 박스 크기

    [Tooltip("Time before being able to jump again after landing")]
    public float JumpTimeout = 0.50f; // 점프 후 다시 점프할 수 있을 때까지의 시간

    [Tooltip("Time before considering the player in free fall")]
    public float FallTimeout = 0.15f; // 낙하 상태로 전환되기까지의 시간

    [Header("Slope Handling")]
    [Tooltip("Maximum angle of slope player can walk on")]
    public float maxSlopeAngle; // 플레이어가 걸을 수 있는 최대 경사 각도

    private RaycastHit slopeHit; // 경사면 정보를 담는 변수

    [SerializeField]
    [Tooltip("Is the player exiting a slope")]
    private bool exitingSlope; // 경사면에서 벗어나는 중인지 여부

    [Tooltip("Is the player on a slope")]
    public bool isSlope; // 현재 경사면 위에 있는지 여부

    [Tooltip("Is the player moving up the slope")]
    public bool upDirection; // 경사면을 올라가는 중인지 여부

    [Tooltip("Is the player moving down the slope")]
    public bool downDirection; // 경사면을 내려가는 중인지 여부

    [Header("Audio")]
    [Tooltip("Audio clip for landing sound")]
    public AudioClip LandingAudioClip; // 착지 사운드 클립

    [Tooltip("Audio clips for footstep sounds")]
    public AudioClip[] FootstepAudioClips; // 발소리 사운드 클립 배열

    [Tooltip("Volume of footstep sounds")]
    [Range(0, 1)]
    public float FootstepAudioVolume = 0.5f; // 발소리 볼륨

    [Header("Camera Settings")]
    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f; // 회전 속도 스무딩 시간

    [Tooltip("Maximum upward camera angle")]
    public float TopClamp = 70.0f; // 카메라의 최대 위쪽 각도

    [Tooltip("Maximum downward camera angle")]
    public float BottomClamp = -30.0f; // 카메라의 최대 아래쪽 각도

    [Tooltip("Additional rotation applied to camera")]
    public float CameraAngleOverride = 0.0f; // 카메라에 추가로 적용되는 각도

    [Tooltip("Locks the camera position")]
    public bool LockCameraPosition = false; // 카메라 위치 고정 여부

    [Header("Cinemachine Settings")]
    [Tooltip("Reference to the Cinemachine camera target")]
    public GameObject CinemachineCameraTarget; // 시네머신 카메라 타겟

    private float _cinemachineTargetYaw; // 카메라의 Yaw 값
    private float _cinemachineTargetPitch; // 카메라의 Pitch 값

    [Header("Camera Effects")]
    //public PlayerCam cam;

    [Tooltip("Field of view when grappling")]
    public float grappleFov = 95f; // 그랩 시 FOV

    //public Transform orientation;

    //float horizontalInput;
    //float verticalInput;

    [Header("Movement Direction")]
    private Vector2 _currentMoveInput;
    [Tooltip("Direction of player movement")]
    private Vector3 moveDirection; // 플레이어 이동 방향

    [Header("Player State")]
    [Tooltip("Current movement state of the player")]
    public MovementState state; // 플레이어의 현재 상태

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
    public bool freeze; // 플레이어 움직임이 멈췄는지 여부

    [Tooltip("Is the player currently grappling")]
    public bool activeGrapple; // 현재 그랩 중인지 여부

    [Tooltip("Is the player currently swinging")]
    public bool swinging; // 현재 스윙 중인지 여부

    // 타임아웃 델타타임
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

    private const float _threshold = 0.01f; // 입력 감도 임계값

    private bool _hasAnimator;

    private void Awake()
    {
        // 메인 카메라 참조
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.freezeRotation = true; // 회전 고정

        readyToJump = true;

        _hasAnimator = TryGetComponent(out _animator);
        _input = GetComponent<PlayerNewInput>();

        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        //startYScale = transform.localScale.y;

        AssignAnimationIDs(); // 애니메이션 ID 할당

        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    /// <summary>
    /// 플레이어가 지면에 있는지 체크
    /// </summary>
    public void IsGrounded()
    {
        Vector3 boxSize = new Vector3(0.1f, 0.1f, 0.1f);
        grounded = Physics.CheckBox(transform.position, boxSize, Quaternion.identity, GroundLayers);

        // 바닥에 닿았을 때 스윙 종료 상태 초기화
        if (grounded && isSwingEnded)
        {
            isSwingEnded = false;
        }
    }

    private void Update()
    {
        PlayerInput(); // 플레이어 입력 처리
        // 속도 제어 및 상태 처리
        SpeedControl();
        StateHandler();

        // 지면에 있을 때 마찰력 적용
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
        IsGrounded(); // 지면 체크
        CheckSlope(); // 경사면 체크

        if (_isMantling)
        {
            //Debug.Log("맨틀 중");
            return;
        }

        MovePlayer(); // 플레이어 이동 처리
        Jump(); // 점프 처리
    }

    private void LateUpdate()
    {
        CameraRotation(); // 카메라 회전 처리
    }

    /// <summary>
    /// 애니메이션 ID 할당
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
    /// 플레이어의 상태를 관리
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
    /// 경사면 체크 및 중력 처리
    /// </summary>
    private void CheckSlope()
    {
        // 경사면 위에 있을 때 중력 해제
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
    /// 플레이어 이동 처리
    /// </summary>
    private void MovePlayer()
    {
        if (activeGrapple) return;

        if (swinging && !grounded) return;

        // 이동 입력이 없을 때
        if (_currentMoveInput == Vector2.zero)
        {
            moveSpeed = 0.0f;
            // 경사면 미끄러짐 방지
            if (isSlope && downDirection)
            {
                _verticalVelocity = 0f;
            }
        }

        float inputMagnitude = _input.analogMovement ? _currentMoveInput.magnitude : 1f;
        Vector3 inputDirection = new Vector3(_currentMoveInput.x, 0.0f, _currentMoveInput.y).normalized;
        moveDirection = Quaternion.Euler(0, _mainCamera.transform.eulerAngles.y, 0) * inputDirection;

        // 플레이어 회전
        if (_currentMoveInput != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        // --- 수정 부분 시작 ---
        // 스윙 종료 후 공중 이동을 약간 허용하는 로직
        // --- 수정된 부분: 스윙 종료 후 공중 이동을 "AddForce"로 처리 ---
        if (isSwingEnded)
        {
            if (!grounded)
            {
                // 현재 관성(velocity)는 그대로 두고, 입력 방향으로만 '약간' Force를 가함
                Vector3 forceDirection = new Vector3(_currentMoveInput.x, 0, _currentMoveInput.y);
                forceDirection = Quaternion.Euler(0.0f, _mainCamera.transform.eulerAngles.y, 0.0f)
                                 * forceDirection.normalized;

                // 이동력을 어느 정도 줄지 배율을 조정
                float reducedAirControlMultiplier = 0.00001f;
                Vector3 finalForce = forceDirection * (moveSpeed * inputMagnitude * reducedAirControlMultiplier);

                // 캐릭터에 힘을 가해 공중에서 조금만 움직이도록
                _rigidbody.AddForce(finalForce * Time.deltaTime, ForceMode.Force);

                // 관성 + 중력 그대로 유지 → velocity 직접 세팅 X
                // 함수 종료
                return;
            }
            else
            {
                // 바닥에 닿았다면 이후에는 기본 로직 진행
                // (isSwingEnded를 초기화할 로직이 별도로 있다면 OK)
            }
        }
        // --- 수정 부분 끝 ---

        // 경사면 위에서 이동
        if (OnSlope() && !exitingSlope)
        {
            _rigidbody.velocity = GetSlopeMoveDirection() * moveSpeed * inputMagnitude;

            if (downDirection)
            {
                _verticalVelocity = -3f;
            }
        }
        // 지면 위에서 이동
        else if (grounded)
        {
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            if (!isSlope)
                _verticalVelocity = 0f;

            _rigidbody.velocity = targetDirection.normalized * (moveSpeed * inputMagnitude) + new Vector3(0.0f, _rigidbody.velocity.y, 0.0f);
        }
        // 공중 이동
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
    /// 속도 제한 처리
    /// </summary>
    private void SpeedControl()
    {
        if (activeGrapple) return;

        //if (!grounded) return;

        // 경사면 위에서의 속도 제한
        if (OnSlope() && !exitingSlope)
        {
            if (_rigidbody.velocity.magnitude > moveSpeed)
                _rigidbody.velocity = _rigidbody.velocity.normalized * moveSpeed;
        }
        // 지면이나 공중에서의 속도 제한
        else
        {
            Vector3 flatVel = new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);

            // 속도 제한 적용
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                _rigidbody.velocity = new Vector3(limitedVel.x, _rigidbody.velocity.y, limitedVel.z);
            }
        }
    }

    /// <summary>
    /// 점프 처리
    /// </summary>
    private void Jump()
    {
        if (swinging && !grounded) return;

        // 스윙 종료 후 바닥에 닿기 전까지 점프 비활성화
        if (isSwingEnded) return;

        if (grounded)
        {
            // 낙하 타임아웃 리셋
            _fallTimeoutDelta = FallTimeout;

            // 애니메이터 업데이트
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }
        
            // 수직 속도 안정화
            if (_verticalVelocity < 0.0f && !downDirection)
            {
                _verticalVelocity = 0f;
            }

            // 점프 입력 처리
            if (_currentJumpInput && readyToJump && grounded)
            {
                if (isSlope)
                    exitingSlope = true;
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                readyToJump = false; // 재점프 방지
                _jumpTimeoutDelta = JumpTimeout; // 쿨다운 타이머 초기화

                // 애니메이터 업데이트
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // 점프 타임아웃 감소
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // 점프 타임아웃 리셋
            _jumpTimeoutDelta = JumpTimeout;

            // 낙하 타임아웃 감소 및 애니메이터 업데이트
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

            // 공중에서 점프 입력 무시
            _input.jump = false;
        }

        // 쿨다운 타이머 완료 시 점프 리셋
        if (!readyToJump)
        {
            if (_jumpTimeoutDelta <= 0.0f)
                ResetJump();
        }

        // 중력 적용
        if (_verticalVelocity > _maxFallVelocity && !grounded)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }

        // 속도 적용
        _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, _verticalVelocity, _rigidbody.velocity.z);
    }

    /// <summary>
    /// 점프 상태 리셋
    /// </summary>
    private void ResetJump()
    {
        _currentJumpInput = false;
        readyToJump = true;
        exitingSlope = false;
    }

    /// <summary>
    /// 카메라 회전 처리
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
    /// 각도를 최소/최대 값으로 클램프
    /// </summary>
    /// <param name="angle">현재 각도</param>
    /// <param name="min">최소 값</param>
    /// <param name="max">최대 값</param>
    /// <returns>클램프된 각도</returns>
    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }

    /// <summary>
    /// 경사면 위에 있는지 체크
    /// </summary>
    /// <returns>경사면 위에 있는지 여부</returns>
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
    /// 경사면에서의 이동 방향 계산
    /// </summary>
    /// <returns>경사면 이동 방향</returns>
    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    /// <summary>
    /// 디버그용 기즈모 그리기
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position, new Vector3(0.2f, 0.1f, 0.2f));
    }

    /// <summary>
    /// 발소리 재생
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
    /// 착지 사운드 재생
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
    /// 맨틀 관련 변수 및 기능
    /// </summary>
    [Header("Mantle Settings")]
    private bool _isMantling = false; // 맨틀 중인지 여부

    [Tooltip("Layer mask for mantleable surfaces")]
    public LayerMask mantleLayerMask;  // 맨틀 가능한 레이어 설정

    [Tooltip("Height from which the forward raycast starts")]
    public float rayHeight = 0.9f;  // 트레이스 시작 높이

    [Tooltip("Maximum distance for forward reach")]
    public float maxReachDistance = 0.5f;  // 전방 트레이스 최대 거리

    [Tooltip("Maximum height of ledge for mantle")]
    public float maxLedgeHeight = 0.9f;  // 맨틀 가능한 장애물 최대 높이

    private Vector3 targetMantlePosition; // 맨틀할 목표 위치

    /// <summary>
    /// 맨틀 가능 여부 체크
    /// </summary>
    /// <returns>맨틀 가능 여부</returns>
    public bool CanPerformMantle()
    {
        if (_isMantling)
        {
            Debug.Log("이미 맨틀하는 중");
            return false; // 이미 맨틀 중이면 맨틀 불가
        }

        // 1. 전방 트레이스: 캐릭터의 중심에서 약간 뒤쪽으로 시작해서 장애물 감지
        Vector3 rayStartCenter = transform.position + transform.forward * -0.1f + Vector3.up * rayHeight;
        Vector3 rayDirection = transform.forward;
        float verticalOffset = 0.1f;

        // 중심, 위, 아래 레이의 시작 지점 계산
        Vector3 rayStartUpper = rayStartCenter + Vector3.up * verticalOffset;
        Vector3 rayStartLower = rayStartCenter - Vector3.up * verticalOffset;

        Debug.DrawRay(rayStartCenter, rayDirection * maxReachDistance, Color.red, 0.5f);
        Debug.DrawRay(rayStartUpper, rayDirection * maxReachDistance, Color.blue, 0.5f);
        Debug.DrawRay(rayStartLower, rayDirection * maxReachDistance, Color.blue, 0.5f);

        bool hitDetected = false;
        RaycastHit hit;

        // 중심, 위, 아래 레이 중 하나라도 충돌하면 히트로 간주
        if (Physics.Raycast(rayStartCenter, rayDirection, out hit, maxReachDistance, mantleLayerMask) ||
            Physics.Raycast(rayStartUpper, rayDirection, out hit, maxReachDistance, mantleLayerMask) ||
            Physics.Raycast(rayStartLower, rayDirection, out hit, maxReachDistance, mantleLayerMask))
        {
            hitDetected = true;
        }

        if (hitDetected)
        {
            // 2. 충돌 지점에서 위로 이동한 후 아래 방향으로 트레이스

            Vector3 downwardRayStart = hit.point + Vector3.up * maxLedgeHeight + transform.forward * 0.2f;

            Debug.DrawRay(downwardRayStart, Vector3.down * maxLedgeHeight, Color.green, 0.5f);
            if (Physics.Raycast(downwardRayStart, Vector3.down, out RaycastHit downwardHit, maxLedgeHeight))
            {
                // 3. 표면이 평평한지 확인
                if (downwardHit.normal.y > 0.6f)
                {
                    // 4. 캡슐 충돌 검사로 충분한 공간이 있는지 확인(끼임 방지)
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
                        return true; // 맨틀 가능
                    }
                    /*
                    만약 캡슐 충돌 검사 때문에 맨틀 동작이 계속 실패한다면 아래 코드로 대체
                    targetMantlePosition = downwardHit.point;
                    return true;
                    */
                }
            }
        }

        return false; // 맨틀 불가
    }

    /// <summary>
    /// 맨틀 동작 시작
    /// </summary>
    public void StartMantle()
    {
        Debug.Log("맨틀 시작");
        _isMantling = true;
        StartCoroutine(MantleMovement());
    }

    /// <summary>
    /// 맨틀 이동 코루틴
    /// </summary>
    System.Collections.IEnumerator MantleMovement()
    {
        Debug.Log("맨틀 동작 실행 중");
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDMantle, true);
        }

        _rigidbody.useGravity = false; // 중력 비활성화
        _verticalVelocity = 0f;

        Vector3 startPosition = targetMantlePosition + (transform.up * -1f) + (transform.forward * -0.5f); // 시작 위치 조정
        transform.position = startPosition;

        float elapsedTime = 0f;

        // 첫 번째 단계: 위로 이동
        float firstPhaseDuration = 17f / 30f; // 약 0.567초
        Vector3 firstPhaseTarget = startPosition + transform.up * 1.1f;
        while (elapsedTime < firstPhaseDuration)
        {
            transform.position = Vector3.Lerp(startPosition, firstPhaseTarget, elapsedTime / firstPhaseDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 두 번째 단계: 앞으로 이동
        float secondPhaseDuration = 8f / 30f; // 약 0.267초
        Vector3 secondPhaseTarget = firstPhaseTarget + transform.forward * 0.5f;
        elapsedTime = 0f;
        while (elapsedTime < secondPhaseDuration)
        {
            transform.position = Vector3.Lerp(firstPhaseTarget, secondPhaseTarget, elapsedTime / secondPhaseDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 최종 위치로 설정
        transform.position = targetMantlePosition;

        _isMantling = false;
        _input.MantleInput(false);
        // 맨틀 후 애니메이션 상태 리셋
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDMantle, false);
        }

        _rigidbody.useGravity = true; // 중력 활성화
        yield return null;
    }

    public void OnSwingEnd()
    {
        isSwingEnded = true;
    }
}
