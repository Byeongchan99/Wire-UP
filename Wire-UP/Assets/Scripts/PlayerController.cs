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
    private PlayerNewInput _input; // 유니티 인풋 시스템
    private GameObject _mainCamera;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed; // 이동 속도
    public float walkSpeed;
    public float sprintSpeed;
    public float swingSpeed;
    public float groundDrag; // 지면 마찰력
    private Vector3 _moveDirection;
    private Vector2 _currentMoveInput;
    private bool _didJumpInput;
    public bool isFreeze; // 플레이어 움직임이 멈췄는지 여부

    [Header("Speed Control")]
    public float currentSpeed; // 현재 속도
    public float maxAirSpeed; // 공중에서의 최대 속도

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    private bool _canJump;
    public float airSpeedMultiplier; // 공중에서의 이동 속도 배율
    public float fallTimeout = 0.15f; // 공중에서 낙하 모션으로 전환되는 시간
    private float _fallTimeoutTimer; // 낙하 타이머

    [Header("Swinging")]
    public bool isSwinging; // 스윙 중인지 여부 - 로프를 발사하여 물체와 연결되어 있는 상태
    public bool isjustSwingEnded = false; // 스윙 종료 직후 여부(바닥에 닿기 전)

    [Header("Mantling")]
    private bool _isMantling = false; // 맨틀 중인지 여부
    public LayerMask mantleLayers;  // 맨틀 가능한 레이어 설정
    public float frontRayHeight = 0.9f;  // 트레이스 시작 높이
    public float maxReachDistance = 0.5f;  // 전방 트레이스 최대 거리
    public float maxLedgeHeight = 0.9f;  // 맨틀 가능한 장애물 최대 높이
    private Vector3 _targetMantlePosition; // 맨틀할 목표 위치

    [Header("Ground Check")]
    public Transform playerCenter; // 플레이어의 중심 위치
    public LayerMask groundLayers; // 지면 레이어 마스크
    public bool isGrounded;
    public float groundCheckBoxSize = 0.1f;

    [Header("Slope Handling")]
    public float stickForce; // 경사면에서 지면 검사를 위한 힘
    public float maxSlopeAngle; // 플레이어가 걸을 수 있는 최대 경사 각도
    private RaycastHit _slopeRayHit; // 경사면 정보를 담는 변수
    private bool _isExitingSlope; // 경사면에서 벗어나는 중인지 여부
    public bool isSlope; // 현재 경사면 위에 있는지 여부
    public bool isUpDirection; // 경사면을 올라가는 중인지 여부
    public bool isDownDirection; // 경사면을 내려가는 중인지 여부

    [Header("Camera")]
    private float _targetRotation = 0.0f; // 플레이어 목표 회전값
    private float _rotationVelocity; // 카메라 회전 속도
    [Range(0.0f, 0.3f)]
    public float rotationSmoothTime = 0.12f; // 회전 속도 스무딩 시간
    public float topMaxClamp = 70.0f; // 카메라의 최대 위쪽 각도
    public float bottomMaxClamp = -30.0f; // 카메라의 최대 아래쪽
    public float cameraAngleOverride = 0.0f; // 카메라에 추가로 적용되는 각도
    public bool isLockCameraPosition = false; // 카메라 위치 고정 여부
    private const float _threshold = 0.01f; // 입력 감도 임계값

    // 시네머신 세팅
    public GameObject cinemachineCameraTarget; // 시네머신 카메라 타겟
    private float _cinemachineTargetYaw; // 카메라의 Yaw 값
    private float _cinemachineTargetPitch; // 카메라의 Pitch 값

    [Header("Audio")]
    public AudioClip landingAudioClip; // 착지 사운드 클립
    public AudioClip[] footstepAudioClips; // 발소리 사운드 클립 배열
    [Range(0, 1)]
    public float footstepAudioVolume = 0.5f; // 발소리 볼륨

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

        _canJump = true;

        _hasAnimator = TryGetComponent(out _animator);
        _input = GetComponent<PlayerNewInput>();

        _cinemachineTargetYaw = cinemachineCameraTarget.transform.rotation.eulerAngles.y;

        AssignAnimationIDs(); // 애니메이션 ID 할당

        _fallTimeoutTimer = fallTimeout;
    }

    private void Update()
    {
        IsGrounded();
        PlayerInput();
        SpeedControl();
        StateHandler();

        // 마찰력 설정
        if (isGrounded && !isSwinging)
            _rigidbody.drag = groundDrag;
        else
            _rigidbody.drag = 0;
    }

    private void FixedUpdate()
    {
        CheckSlope(); // 경사면 체크

        // 경사로에 있지만 지면에 있지 않을 때 힘을 가해줌
        if (!isGrounded && OnSlope() && !_isExitingSlope)
        {
            // 너무 큰 값이 아니도록 조심
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
        isGrounded = Physics.CheckBox(transform.position, boxSize, Quaternion.identity, groundLayers);

        // 바닥에 닿았을 때 스윙 종료 상태 초기화
        if (isGrounded && isjustSwingEnded)
        {
            isjustSwingEnded = false;
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
        _didJumpInput = _input.jump;

        // 점프 입력
        if (_didJumpInput && _canJump && isGrounded)
        {
            _canJump = false;
            Jump();
            StartCoroutine(ResetJumpCoroutine());
        }
    }

    private void StateHandler()
    {
        // 정지
        if (isFreeze)
        {
            state = MovementState.freeze;
            _moveSpeed = 0;
            _rigidbody.velocity = Vector3.zero;
        }

        // 스윙
        else if (isSwinging && !isGrounded)
        {
            state = MovementState.swinging;
            _moveSpeed = swingSpeed;
        }

        // 맨틀
        else if (_input.mantle)
        {
            state = MovementState.mantling;
        }

        // 달리기
        else if (isGrounded && _input.sprint)
        {
            state = MovementState.sprinting;
            _moveSpeed = sprintSpeed;
        }

        // 걷기
        else if (isGrounded)
        {
            state = MovementState.walking;
            _moveSpeed = walkSpeed;
        }

        // 공중
        else
        {
            state = MovementState.air;
            if (_input.sprint)
                _moveSpeed = sprintSpeed * airSpeedMultiplier;
            else
                _moveSpeed = walkSpeed * airSpeedMultiplier;
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
                isUpDirection = true;
                isDownDirection = false;
            }
            else
            {
                isDownDirection = true;
                isUpDirection = false;
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
            _moveSpeed = 0.0f;
        }

        float inputMagnitude = _input.analogMovement ? _currentMoveInput.magnitude : 1f;
        Vector3 inputDirection = new Vector3(_currentMoveInput.x, 0.0f, _currentMoveInput.y).normalized;
        _moveDirection = Quaternion.Euler(0, _mainCamera.transform.eulerAngles.y, 0) * inputDirection;

        // 플레이어 회전
        if (_currentMoveInput != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(_moveDirection.x, _moveDirection.z) * Mathf.Rad2Deg;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        if (!isGrounded && isjustSwingEnded)
        {
            /*
            // 현재 관성(velocity)는 그대로 두고, 입력 방향으로만 '약간' Force를 가함
            Vector3 forceDirection = new Vector3(_currentMoveInput.x, 0, _currentMoveInput.y);
            forceDirection = Quaternion.Euler(0.0f, _mainCamera.transform.eulerAngles.y, 0.0f) * forceDirection.normalized;

            // 이동력을 어느 정도 줄지 배율을 조정
            float reducedAirControlMultiplier = 10f;
            Vector3 finalForce = forceDirection * (moveSpeed * inputMagnitude * reducedAirControlMultiplier);

            // 캐릭터에 힘을 가해 공중에서 조금만 움직이도록
            _rigidbody.AddForce(finalForce * Time.deltaTime, ForceMode.Force);
            return;
            */

            /*
            _rigidbody.AddForce(_moveDirection.normalized * _moveSpeed * 5f * airSpeedMultiplier, ForceMode.Force);
            return;    
            */

            Vector3 currentVelocity = _rigidbody.velocity;
            currentSpeed = new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;

            if (currentSpeed < maxAirSpeed)
            {
                // 속도가 아직 최대치 미만인 경우에만 힘을 준다.
                float speedDiff = maxAirSpeed - currentSpeed;
                float factor = speedDiff / maxAirSpeed;  // [0..1]

                // factor가 1에 가까울수록 많이, 0에 가까울수록 적게
                float forceMultiplier = 10f * factor;

                _rigidbody.AddForce(_moveDirection.normalized * _moveSpeed * forceMultiplier * airSpeedMultiplier, ForceMode.Force);
            }

            return;
        }

        // on slope
        if (OnSlope() && !_isExitingSlope)
        {
            _rigidbody.velocity = GetSlopeMoveDirection() * _moveSpeed * inputMagnitude;
            //_rigidbody.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);

            if (_rigidbody.velocity.y > 0)
                _rigidbody.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        // 지면에서
        else if (isGrounded)
        {
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            _rigidbody.velocity = targetDirection.normalized * (_moveSpeed * inputMagnitude) + new Vector3(0.0f, _rigidbody.velocity.y, 0.0f);
            //_rigidbody.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }

        // 공중에서
        else if (!isGrounded)
            _rigidbody.AddForce(_moveDirection.normalized * _moveSpeed * 10f * airSpeedMultiplier, ForceMode.Force);

        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _moveSpeed);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }
    }

    private void SpeedControl()
    {
        if (isSwinging || isjustSwingEnded) return;

        // 경사로에서의 속도 제어
        if (OnSlope() && !_isExitingSlope)
        {
            if (_rigidbody.velocity.magnitude > _moveSpeed)
                _rigidbody.velocity = _rigidbody.velocity.normalized * _moveSpeed;
        }

        // 지면 또는 공중에서의 속도 제어
        else
        {
            Vector3 flatVel = new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);

            // limit velocity if needed
            if (flatVel.magnitude > _moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * _moveSpeed;
                _rigidbody.velocity = new Vector3(limitedVel.x, _rigidbody.velocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        _isExitingSlope = true;

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
        yield return new WaitForSeconds(_fallTimeoutTimer);
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
        _didJumpInput = false;
        _canJump = true;
        _isExitingSlope = false;

        if (_hasAnimator)
        {
            _animator.SetBool(_animIDJump, false);
            _animator.SetBool(_animIDFreeFall, false);
        }
    }

    public void OnSwingEnd()
    {
        isjustSwingEnded = true;
    }

    public bool CanPerformMantle()
    {
        if (_isMantling)
        {
            Debug.Log("이미 맨틀하는 중");
            return false; // 이미 맨틀 중이면 맨틀 불가
        }

        // 1. 전방 트레이스: 캐릭터의 중심에서 약간 뒤쪽으로 시작해서 장애물 감지
        Vector3 rayStartCenter = transform.position + transform.forward * -0.1f + Vector3.up * frontRayHeight;
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
        if (Physics.Raycast(rayStartCenter, rayDirection, out hit, maxReachDistance, mantleLayers) ||
            Physics.Raycast(rayStartUpper, rayDirection, out hit, maxReachDistance, mantleLayers) ||
            Physics.Raycast(rayStartLower, rayDirection, out hit, maxReachDistance, mantleLayers))
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
                        mantleLayers
                    );

                    if (hasRoom)
                    {
                        _targetMantlePosition = downwardHit.point;
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

        Vector3 startPosition = _targetMantlePosition + (transform.up * -1f) + (transform.forward * -0.5f); // 시작 위치 조정
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
        transform.position = _targetMantlePosition;

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
        if (Physics.Raycast(transform.position, Vector3.down, out _slopeRayHit, 0.2f))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeRayHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    // 경사면 이동 방향
    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(_moveDirection, _slopeRayHit.normal).normalized;
    }

    private void CameraRotation()
    {
        if (_input.look.sqrMagnitude >= _threshold && !isLockCameraPosition)
        {
            float deltaTimeMultiplier = 1.0f;
            _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, bottomMaxClamp, topMaxClamp);

        cinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + cameraAngleOverride, _cinemachineTargetYaw, 0.0f);
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
            if (footstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, footstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(footstepAudioClips[index], transform.position, footstepAudioVolume);
            }
        }
    }

    // 착지 사운드 재생
    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(landingAudioClip, transform.position, footstepAudioVolume);
        }
    }
}
