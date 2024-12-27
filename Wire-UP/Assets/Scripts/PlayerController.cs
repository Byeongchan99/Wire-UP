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
    public bool freeze; // �÷��̾� �������� ������� ����

    [Header("Speed Cotrol")]
    private float _maxFallVelocity = -15.0f; // �ִ� ���� �ӵ�

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier; // ���߿����� �̵� �ӵ� ����
    public float fallTimeout = 0.15f;
    bool readyToJump;
    private float _jumpCooldownDelta;
    private float _fallTimeoutDelta;

    [Header("Swinging")]
    public bool isSwinging; // ���� ������ ���� - ������ �߻��Ͽ� ��ü�� ����Ǿ� �ִ� ����
    public bool isSwingEnded = false; // ���� ���� ���� ����(�ٴڿ� ��� ��)

    [Header("Mantling")]
    private bool _isMantling = false; // ��Ʋ ������ ����
    public LayerMask mantleLayerMask;  // ��Ʋ ������ ���̾� ����
    public float rayHeight = 0.9f;  // Ʈ���̽� ���� ����
    public float maxReachDistance = 0.5f;  // ���� Ʈ���̽� �ִ� �Ÿ�
    public float maxLedgeHeight = 0.9f;  // ��Ʋ ������ ��ֹ� �ִ� ����
    private Vector3 targetMantlePosition; // ��Ʋ�� ��ǥ ��ġ

    [Header("Ground Check")]
    public Transform playerCenter; // �÷��̾��� �߽� ��ġ
    public LayerMask GroundLayers;
    public bool isGrounded;
    public float groundCheckBoxSize = 0.1f;

    [Header("Slope Handling")]
    public float stickForce; // ���鿡�� ���� �˻縦 ���� ��
    public float maxSlopeAngle; // �÷��̾ ���� �� �ִ� �ִ� ��� ����
    private RaycastHit slopeHit; // ���� ������ ��� ����
    private bool exitingSlope; // ���鿡�� ����� ������ ����
    public bool isSlope; // ���� ���� ���� �ִ��� ����
    public bool upDirection; // ������ �ö󰡴� ������ ����
    public bool downDirection; // ������ �������� ������ ����

    [Header("Camera")]
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f; // ȸ�� �ӵ� ������ �ð�
    public float TopClamp = 70.0f; // ī�޶��� �ִ� ���� ����
    public float BottomClamp = -30.0f; // ī�޶��� �ִ� �Ʒ���
    public float CameraAngleOverride = 0.0f; // ī�޶� �߰��� ����Ǵ� ����
    public bool LockCameraPosition = false; // ī�޶� ��ġ ���� ����
    private const float _threshold = 0.01f; // �Է� ���� �Ӱ谪

    // �ó׸ӽ� ����
    public GameObject CinemachineCameraTarget; // �ó׸ӽ� ī�޶� Ÿ��
    private float _cinemachineTargetYaw; // ī�޶��� Yaw ��
    private float _cinemachineTargetPitch; // ī�޶��� Pitch ��

    [Header("Audio")]
    public AudioClip LandingAudioClip; // ���� ���� Ŭ��
    public AudioClip[] FootstepAudioClips; // �߼Ҹ� ���� Ŭ�� �迭
    [Range(0, 1)]
    public float FootstepAudioVolume = 0.5f; // �߼Ҹ� ����

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

        readyToJump = true;

        _hasAnimator = TryGetComponent(out _animator);
        _input = GetComponent<PlayerNewInput>();

        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        AssignAnimationIDs(); // �ִϸ��̼� ID �Ҵ�

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
        CheckSlope(); // ���� üũ

        if (isGrounded && OnSlope())
        {
            // �ʹ� ū ���� �ƴϵ��� ���� (��: 1~20 ������ �׽�Ʈ)
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
        isGrounded = Physics.CheckBox(transform.position, boxSize, Quaternion.identity, GroundLayers);

        // �ٴڿ� ����� �� ���� ���� ���� �ʱ�ȭ
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
            //Debug.Log("��Ʋ ��");
            return;
        }

        _currentMoveInput = _input.move;
        _currentJumpInput = _input.jump;

        // ���� �Է�
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

    // ���� �˻� �� �߷� ó��
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

    private void MovePlayer()
    {
        if (isSwinging && !isGrounded) return;

        // �̵� �Է��� ���� ��
        if (_currentMoveInput == Vector2.zero)
        {
            moveSpeed = 0.0f;
            /*
            // ���� �̲����� ����
            if (isSlope && downDirection)
            {
                //_rigidbody.velocity.y = 0;
            }
            */
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

        if (!isGrounded && isSwingEnded)
        {
            
            // ���� ����(velocity)�� �״�� �ΰ�, �Է� �������θ� '�ణ' Force�� ����
            Vector3 forceDirection = new Vector3(_currentMoveInput.x, 0, _currentMoveInput.y);
            forceDirection = Quaternion.Euler(0.0f, _mainCamera.transform.eulerAngles.y, 0.0f) * forceDirection.normalized;

            // �̵����� ��� ���� ���� ������ ����
            float reducedAirControlMultiplier = 10f;
            Vector3 finalForce = forceDirection * (moveSpeed * inputMagnitude * reducedAirControlMultiplier);

            // ĳ���Ϳ� ���� ���� ���߿��� ���ݸ� �����̵���
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

        if (!isGrounded) return; // ���� ��������

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

        // �ִϸ����� ������Ʈ
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDJump, true);
        }
    }

    IEnumerator ResetJumpCoroutine()
    {
        // fallTimeoutDelta �Ŀ� JumpAnimation
        yield return new WaitForSeconds(_fallTimeoutDelta);
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

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, 0.2f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    // ���� �̵� ����
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

    // �߼Ҹ� ���
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

    // ���� ���� ���
    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.position, FootstepAudioVolume);
        }
    }
}
