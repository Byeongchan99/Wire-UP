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
    private PlayerNewInput _input; // ����Ƽ ��ǲ �ý���
    private GameObject _mainCamera;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed; // �̵� �ӵ�
    public float walkSpeed;
    public float sprintSpeed;
    public float swingSpeed;
    public float groundDrag; // ���� ������
    private Vector3 _moveDirection;
    private Vector2 _currentMoveInput;
    private bool _didJumpInput;
    public bool isFreeze; // �÷��̾� �������� ������� ����

    [Header("Speed Control")]
    public float currentSpeed; // ���� �ӵ�
    public float maxAirSpeed; // ���߿����� �ִ� �ӵ�

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    private bool _canJump;
    public float airSpeedMultiplier; // ���߿����� �̵� �ӵ� ����
    public float fallTimeout = 0.15f; // ���߿��� ���� ������� ��ȯ�Ǵ� �ð�
    private float _fallTimeoutTimer; // ���� Ÿ�̸�

    [Header("Swinging")]
    public bool isSwinging; // ���� ������ ���� - ������ �߻��Ͽ� ��ü�� ����Ǿ� �ִ� ����
    public bool isjustSwingEnded = false; // ���� ���� ���� ����(�ٴڿ� ��� ��)

    [Header("Mantling")]
    private bool _isMantling = false; // ��Ʋ ������ ����
    public LayerMask mantleLayers;  // ��Ʋ ������ ���̾� ����
    public float frontRayHeight = 0.9f;  // Ʈ���̽� ���� ����
    public float maxReachDistance = 0.5f;  // ���� Ʈ���̽� �ִ� �Ÿ�
    public float maxLedgeHeight = 0.9f;  // ��Ʋ ������ ��ֹ� �ִ� ����
    private Vector3 _targetMantlePosition; // ��Ʋ�� ��ǥ ��ġ

    [Header("Ground Check")]
    public Transform playerCenter; // �÷��̾��� �߽� ��ġ
    public LayerMask groundLayers; // ���� ���̾� ����ũ
    public bool isGrounded;
    public float groundCheckBoxSize = 0.1f;

    [Header("Slope Handling")]
    public float stickForce; // ���鿡�� ���� �˻縦 ���� ��
    public float maxSlopeAngle; // �÷��̾ ���� �� �ִ� �ִ� ��� ����
    private RaycastHit _slopeRayHit; // ���� ������ ��� ����
    private bool _isExitingSlope; // ���鿡�� ����� ������ ����
    public bool isSlope; // ���� ���� ���� �ִ��� ����
    public bool isUpDirection; // ������ �ö󰡴� ������ ����
    public bool isDownDirection; // ������ �������� ������ ����

    [Header("Camera")]
    private float _targetRotation = 0.0f; // �÷��̾� ��ǥ ȸ����
    private float _rotationVelocity; // ī�޶� ȸ�� �ӵ�
    [Range(0.0f, 0.3f)]
    public float rotationSmoothTime = 0.12f; // ȸ�� �ӵ� ������ �ð�
    public float topMaxClamp = 70.0f; // ī�޶��� �ִ� ���� ����
    public float bottomMaxClamp = -30.0f; // ī�޶��� �ִ� �Ʒ���
    public float cameraAngleOverride = 0.0f; // ī�޶� �߰��� ����Ǵ� ����
    public bool isLockCameraPosition = false; // ī�޶� ��ġ ���� ����
    private const float _threshold = 0.01f; // �Է� ���� �Ӱ谪

    // �ó׸ӽ� ����
    public GameObject cinemachineCameraTarget; // �ó׸ӽ� ī�޶� Ÿ��
    private float _cinemachineTargetYaw; // ī�޶��� Yaw ��
    private float _cinemachineTargetPitch; // ī�޶��� Pitch ��

    [Header("Audio")]
    public AudioClip landingAudioClip; // ���� ���� Ŭ��
    public AudioClip[] footstepAudioClips; // �߼Ҹ� ���� Ŭ�� �迭
    [Range(0, 1)]
    public float footstepAudioVolume = 0.5f; // �߼Ҹ� ����

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
        // ���� ī�޶� ����
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

        AssignAnimationIDs(); // �ִϸ��̼� ID �Ҵ�

        _fallTimeoutTimer = fallTimeout;
    }

    private void Update()
    {
        IsGrounded();
        PlayerInput();
        SpeedControl();
        StateHandler();

        // ������ ����
        if (isGrounded && !isSwinging)
            _rigidbody.drag = groundDrag;
        else
            _rigidbody.drag = 0;
    }

    private void FixedUpdate()
    {
        CheckSlope(); // ���� üũ

        // ���ο� ������ ���鿡 ���� ���� �� ���� ������
        if (!isGrounded && OnSlope() && !_isExitingSlope)
        {
            // �ʹ� ū ���� �ƴϵ��� ����
            _rigidbody.AddForce(Vector3.down * stickForce, ForceMode.Acceleration);
        }

        MovePlayer();
    }

    private void LateUpdate()
    {
        CameraRotation(); // ī�޶� ȸ�� ó��
    }

    // �ִϸ��̼� ID �Ҵ�
    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        _animIDMantle = Animator.StringToHash("Mantle");
    }

    // ���� �˻�
    public void IsGrounded()
    {
        Vector3 boxSize = new Vector3(0.1f, groundCheckBoxSize, 0.1f);
        isGrounded = Physics.CheckBox(transform.position, boxSize, Quaternion.identity, groundLayers);

        // �ٴڿ� ����� �� ���� ���� ���� �ʱ�ȭ
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
            //Debug.Log("��Ʋ ��");
            return;
        }

        _currentMoveInput = _input.move;
        _didJumpInput = _input.jump;

        // ���� �Է�
        if (_didJumpInput && _canJump && isGrounded)
        {
            _canJump = false;
            Jump();
            StartCoroutine(ResetJumpCoroutine());
        }
    }

    private void StateHandler()
    {
        // ����
        if (isFreeze)
        {
            state = MovementState.freeze;
            _moveSpeed = 0;
            _rigidbody.velocity = Vector3.zero;
        }

        // ����
        else if (isSwinging && !isGrounded)
        {
            state = MovementState.swinging;
            _moveSpeed = swingSpeed;
        }

        // ��Ʋ
        else if (_input.mantle)
        {
            state = MovementState.mantling;
        }

        // �޸���
        else if (isGrounded && _input.sprint)
        {
            state = MovementState.sprinting;
            _moveSpeed = sprintSpeed;
        }

        // �ȱ�
        else if (isGrounded)
        {
            state = MovementState.walking;
            _moveSpeed = walkSpeed;
        }

        // ����
        else
        {
            state = MovementState.air;
            if (_input.sprint)
                _moveSpeed = sprintSpeed * airSpeedMultiplier;
            else
                _moveSpeed = walkSpeed * airSpeedMultiplier;
        }
    }

    // ���� �˻� �� �߷� ó��
    private void CheckSlope()
    {
        // ���� ���� ���� �� �߷� ����
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

        // �̵� �Է��� ���� ��
        if (_currentMoveInput == Vector2.zero)
        {
            _moveSpeed = 0.0f;
        }

        float inputMagnitude = _input.analogMovement ? _currentMoveInput.magnitude : 1f;
        Vector3 inputDirection = new Vector3(_currentMoveInput.x, 0.0f, _currentMoveInput.y).normalized;
        _moveDirection = Quaternion.Euler(0, _mainCamera.transform.eulerAngles.y, 0) * inputDirection;

        // �÷��̾� ȸ��
        if (_currentMoveInput != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(_moveDirection.x, _moveDirection.z) * Mathf.Rad2Deg;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        if (!isGrounded && isjustSwingEnded)
        {
            /*
            // ���� ����(velocity)�� �״�� �ΰ�, �Է� �������θ� '�ణ' Force�� ����
            Vector3 forceDirection = new Vector3(_currentMoveInput.x, 0, _currentMoveInput.y);
            forceDirection = Quaternion.Euler(0.0f, _mainCamera.transform.eulerAngles.y, 0.0f) * forceDirection.normalized;

            // �̵����� ��� ���� ���� ������ ����
            float reducedAirControlMultiplier = 10f;
            Vector3 finalForce = forceDirection * (moveSpeed * inputMagnitude * reducedAirControlMultiplier);

            // ĳ���Ϳ� ���� ���� ���߿��� ���ݸ� �����̵���
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
                // �ӵ��� ���� �ִ�ġ �̸��� ��쿡�� ���� �ش�.
                float speedDiff = maxAirSpeed - currentSpeed;
                float factor = speedDiff / maxAirSpeed;  // [0..1]

                // factor�� 1�� �������� ����, 0�� �������� ����
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

        // ���鿡��
        else if (isGrounded)
        {
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            _rigidbody.velocity = targetDirection.normalized * (_moveSpeed * inputMagnitude) + new Vector3(0.0f, _rigidbody.velocity.y, 0.0f);
            //_rigidbody.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }

        // ���߿���
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

        // ���ο����� �ӵ� ����
        if (OnSlope() && !_isExitingSlope)
        {
            if (_rigidbody.velocity.magnitude > _moveSpeed)
                _rigidbody.velocity = _rigidbody.velocity.normalized * _moveSpeed;
        }

        // ���� �Ǵ� ���߿����� �ӵ� ����
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

        // �ִϸ����� ������Ʈ
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDJump, true);
        }
    }

    IEnumerator ResetJumpCoroutine()
    {
        // fallTimeoutDelta �Ŀ� JumpAnimation
        yield return new WaitForSeconds(_fallTimeoutTimer);
        JumpAnimation();

        // jumpCooldown �Ŀ� ResetJump
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
            Debug.Log("�̹� ��Ʋ�ϴ� ��");
            return false; // �̹� ��Ʋ ���̸� ��Ʋ �Ұ�
        }

        // 1. ���� Ʈ���̽�: ĳ������ �߽ɿ��� �ణ �������� �����ؼ� ��ֹ� ����
        Vector3 rayStartCenter = transform.position + transform.forward * -0.1f + Vector3.up * frontRayHeight;
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
        if (Physics.Raycast(rayStartCenter, rayDirection, out hit, maxReachDistance, mantleLayers) ||
            Physics.Raycast(rayStartUpper, rayDirection, out hit, maxReachDistance, mantleLayers) ||
            Physics.Raycast(rayStartLower, rayDirection, out hit, maxReachDistance, mantleLayers))
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
                        mantleLayers
                    );

                    if (hasRoom)
                    {
                        _targetMantlePosition = downwardHit.point;
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

    public void StartMantle()
    {
        Debug.Log("��Ʋ ����");
        _isMantling = true;
        StartCoroutine(MantleMovement());
    }

    IEnumerator MantleMovement()
    {
        Debug.Log("��Ʋ ���� ���� ��");
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDMantle, true);
        }

        _rigidbody.useGravity = false; // �߷� ��Ȱ��ȭ
        //_verticalVelocity = 0f;

        Vector3 startPosition = _targetMantlePosition + (transform.up * -1f) + (transform.forward * -0.5f); // ���� ��ġ ����
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
        transform.position = _targetMantlePosition;

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

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out _slopeRayHit, 0.2f))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeRayHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    // ���� �̵� ����
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

    // �߼Ҹ� ���
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

    // ���� ���� ���
    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(landingAudioClip, transform.position, footstepAudioVolume);
        }
    }
}
