using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    private PlayerInput _playerInput;
    private Animator _animator;
    private Rigidbody _rigidbody;
    private PlayerNewInput _input;
    private GameObject _mainCamera;

    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float swingSpeed;
    public float groundDrag;
    Vector3 moveDirection;
    private Vector2 _currentMoveInput;
    private bool _currentJumpInput;
    public bool freeze; // 플레이어 움직임이 멈췄는지 여부

    [Header("Speed Cotrol")]
    private float _maxFallVelocity = -15.0f; // 최대 낙하 속도

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier; // 공중에서의 이동 속도 배율
    public float fallTimeout = 0.15f;
    bool readyToJump;
    private float _jumpCooldownDelta;
    private float _fallTimeoutDelta;

    [Header("Swinging")]
    public bool isSwinging; // 스윙 중인지 여부 - 로프를 발사하여 물체와 연결되어 있는 상태
    public bool isSwingEnded = false; // 스윙 종료 직후 여부(바닥에 닿기 전)

    [Header("Mantling")]
    private bool _isMantling = false; // 맨틀 중인지 여부
    public LayerMask mantleLayerMask;  // 맨틀 가능한 레이어 설정
    public float rayHeight = 0.9f;  // 트레이스 시작 높이
    public float maxReachDistance = 0.5f;  // 전방 트레이스 최대 거리
    public float maxLedgeHeight = 0.9f;  // 맨틀 가능한 장애물 최대 높이
    private Vector3 targetMantlePosition; // 맨틀할 목표 위치

    [Header("Ground Check")]
    public Transform playerCenter; // 플레이어의 중심 위치
    public LayerMask GroundLayers;
    public bool isGrounded;
    public float groundCheckBoxSize = 0.1f;

    [Header("Slope Handling")]
    public float stickForce; // 경사면에서 지면 검사를 위한 힘
    public float maxSlopeAngle; // 플레이어가 걸을 수 있는 최대 경사 각도
    private RaycastHit slopeHit; // 경사면 정보를 담는 변수
    private bool exitingSlope; // 경사면에서 벗어나는 중인지 여부
    public bool isSlope; // 현재 경사면 위에 있는지 여부
    public bool upDirection; // 경사면을 올라가는 중인지 여부
    public bool downDirection; // 경사면을 내려가는 중인지 여부

    [Header("Camera")]
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f; // 회전 속도 스무딩 시간
    public float TopClamp = 70.0f; // 카메라의 최대 위쪽 각도
    public float BottomClamp = -30.0f; // 카메라의 최대 아래쪽
    public float CameraAngleOverride = 0.0f; // 카메라에 추가로 적용되는 각도
    public bool LockCameraPosition = false; // 카메라 위치 고정 여부
    private const float _threshold = 0.01f; // 입력 감도 임계값

    // 시네머신 세팅
    public GameObject CinemachineCameraTarget; // 시네머신 카메라 타겟
    private float _cinemachineTargetYaw; // 카메라의 Yaw 값
    private float _cinemachineTargetPitch; // 카메라의 Pitch 값

    [Header("Audio")]
    public AudioClip LandingAudioClip; // 착지 사운드 클립
    public AudioClip[] FootstepAudioClips; // 발소리 사운드 클립 배열
    [Range(0, 1)]
    public float FootstepAudioVolume = 0.5f; // 발소리 볼륨

    [Header("Animation IDs")]
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;
    private int _animIDMantle;
    private bool _hasAnimator;

    [Header("Player State")]
    public MovementState state;
    public enum MovementState
    {
        freeze,
        swinging,
        walking,
        sprinting,
        mantling,
        air
    }

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
        _rigidbody.freezeRotation = true;

        readyToJump = true;

        _hasAnimator = TryGetComponent(out _animator);
        _input = GetComponent<PlayerNewInput>();

        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        AssignAnimationIDs(); // 애니메이션 ID 할당

        _jumpCooldownDelta = jumpCooldown;
        _fallTimeoutDelta = fallTimeout;
    }

    private void Update()
    {
        IsGrounded();
        PlayerInput();
        SpeedControl();
        StateHandler();

        // handle drag
        if (isGrounded && !isSwinging)
            _rigidbody.drag = groundDrag;
        else
            _rigidbody.drag = 0;
    }

    private void FixedUpdate()
    {
        CheckSlope(); // 경사면 체크

        if (isGrounded && OnSlope())
        {
            // 너무 큰 값이 아니도록 조심 (예: 1~20 선에서 테스트)
            _rigidbody.AddForce(Vector3.down * stickForce, ForceMode.Acceleration);
        }

        MovePlayer();
    }

    private void LateUpdate()
    {
        CameraRotation(); // 카메라 회전 처리
    }

    // 애니메이션 ID 할당
    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        _animIDMantle = Animator.StringToHash("Mantle");
    }

    // 지면 검사
    public void IsGrounded()
    {
        Vector3 boxSize = new Vector3(0.1f, groundCheckBoxSize, 0.1f);
        isGrounded = Physics.CheckBox(transform.position, boxSize, Quaternion.identity, GroundLayers);

        // 바닥에 닿았을 때 스윙 종료 상태 초기화
        if (isGrounded && isSwingEnded)
        {
            isSwingEnded = false;
        }

        if (_animator)
        {
            _animator.SetBool(_animIDGrounded, isGrounded);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position, new Vector3(0.1f, groundCheckBoxSize, 0.1f));
    }

    private void PlayerInput()
    {
        if (_isMantling)
        {
            //Debug.Log("맨틀 중");
            return;
        }

        _currentMoveInput = _input.move;
        _currentJumpInput = _input.jump;

        // 점프 입력
        if (_currentJumpInput && readyToJump && isGrounded)
        {
            readyToJump = false;
            Jump();
            StartCoroutine(ResetJumpCoroutine());
        }
    }

    private void StateHandler()
    {
        // Mode - Freeze
        if (freeze)
        {
            state = MovementState.freeze;
            moveSpeed = 0;
            _rigidbody.velocity = Vector3.zero;
        }

        // Mode - Swinging
        else if (isSwinging && !isGrounded)
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
        else if (isGrounded && _input.sprint)
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }

        // Mode - Walking
        else if (isGrounded)
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

    // 경사면 검사 및 중력 처리
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

    private void MovePlayer()
    {
        if (isSwinging && !isGrounded) return;

        // 이동 입력이 없을 때
        if (_currentMoveInput == Vector2.zero)
        {
            moveSpeed = 0.0f;
            /*
            // 경사면 미끄러짐 방지
            if (isSlope && downDirection)
            {
                //_rigidbody.velocity.y = 0;
            }
            */
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

        if (!isGrounded && isSwingEnded)
        {
            
            // 현재 관성(velocity)는 그대로 두고, 입력 방향으로만 '약간' Force를 가함
            Vector3 forceDirection = new Vector3(_currentMoveInput.x, 0, _currentMoveInput.y);
            forceDirection = Quaternion.Euler(0.0f, _mainCamera.transform.eulerAngles.y, 0.0f) * forceDirection.normalized;

            // 이동력을 어느 정도 줄지 배율을 조정
            float reducedAirControlMultiplier = 10f;
            Vector3 finalForce = forceDirection * (moveSpeed * inputMagnitude * reducedAirControlMultiplier);

            // 캐릭터에 힘을 가해 공중에서 조금만 움직이도록
            _rigidbody.AddForce(finalForce * Time.deltaTime, ForceMode.Force);
            return;
            
            /*
            _rigidbody.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
            return;
            */
        }

        // on slope
        if (OnSlope() && !exitingSlope)
        {
            _rigidbody.velocity = GetSlopeMoveDirection() * moveSpeed * inputMagnitude;
            //_rigidbody.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);

            if (_rigidbody.velocity.y > 0)
                _rigidbody.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        // on ground
        else if (isGrounded)
        {
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            /*
            if (!isSlope)
                _verticalVelocity = 0f;
            */

            _rigidbody.velocity = targetDirection.normalized * (moveSpeed * inputMagnitude) + new Vector3(0.0f, _rigidbody.velocity.y, 0.0f);
            //_rigidbody.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }

        // in air
        else if (!isGrounded)
            _rigidbody.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, moveSpeed);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }
    }

    private void SpeedControl()
    {
        if (isSwinging) return;

        if (!isGrounded) return; // 여기 수정했음

        // limiting speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if (_rigidbody.velocity.magnitude > moveSpeed)
                _rigidbody.velocity = _rigidbody.velocity.normalized * moveSpeed;
        }

        // limiting speed on ground or in air
        else
        {
            Vector3 flatVel = new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);

            // limit velocity if needed
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                _rigidbody.velocity = new Vector3(limitedVel.x, _rigidbody.velocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;

        // reset y velocity
        _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);
        _rigidbody.AddForce(transform.up * jumpForce, ForceMode.Impulse);

        // 애니메이터 업데이트
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDJump, true);
        }
    }

    IEnumerator ResetJumpCoroutine()
    {
        // fallTimeoutDelta 후에 JumpAnimation
        yield return new WaitForSeconds(_fallTimeoutDelta);
        JumpAnimation();

        // jumpCooldown 후에 ResetJump
        yield return new WaitForSeconds(jumpCooldown);
        ResetJump();
    }

    private void JumpAnimation()
    {
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDFreeFall, true);
        }
    }

    private void ResetJump()
    {
        _currentJumpInput = false;
        readyToJump = true;
        exitingSlope = false;

        if (_hasAnimator)
        {
            _animator.SetBool(_animIDJump, false);
            _animator.SetBool(_animIDFreeFall, false);
        }
    }

    public void OnSwingEnd()
    {
        isSwingEnded = true;
    }

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

    public void StartMantle()
    {
        Debug.Log("맨틀 시작");
        _isMantling = true;
        StartCoroutine(MantleMovement());
    }

    IEnumerator MantleMovement()
    {
        Debug.Log("맨틀 동작 실행 중");
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDMantle, true);
        }

        _rigidbody.useGravity = false; // 중력 비활성화
        //_verticalVelocity = 0f;

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

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, 0.2f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    // 경사면 이동 방향
    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

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

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }

    // 발소리 재생
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

    // 착지 사운드 재생
    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.position, FootstepAudioVolume);
        }
    }
}
