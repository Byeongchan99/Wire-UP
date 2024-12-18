using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

[RequireComponent(typeof(Rigidbody))]
#if ENABLE_INPUT_SYSTEM
[RequireComponent(typeof(PlayerInput))]
#endif
public class ThirdPersonControllerWithRigidbody : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Move speed of the character in m/s")]
    public float MoveSpeed = 2.0f;

    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 5.335f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the Rigidbody built in grounded check")]
    public bool Grounded = true;
    public bool WallAttached = false; // �߰��� �� üũ ���� ����

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.1f;

    [Tooltip("The radius of the grounded check. Should match the radius of the Rigidbody collider")]
    public float GroundedRadius = 0.2f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;

    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;

    [Tooltip("Additional degrees to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;

    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // player
    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif
    private Animator _animator;
    private Rigidbody _rigidbody;
    private PlayerNewInput _input;
    private GameObject _mainCamera;

    private const float _threshold = 0.01f;

    private bool _hasAnimator;

    /*
    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
        }
    }
    */

    private void Awake()
    {
        // get a reference to our main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        _hasAnimator = TryGetComponent(out _animator);
        _rigidbody = GetComponent<Rigidbody>();
        _input = GetComponent<PlayerNewInput>();
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

        AssignAnimationIDs();

        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    private void FixedUpdate()
    {
        _hasAnimator = TryGetComponent(out _animator);

        if (_isMantling)
        {
            Debug.Log("��Ʋ ��");
            return;
        }

        JumpAndGravity();
        //GroundedCheck();
        GroundedAndWallCheck();
        Move();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        _animIDMantle = Animator.StringToHash("Mantle");
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }

    private void GroundedAndWallCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);

        // �ٴ� üũ
        Collider[] hitColliders = Physics.OverlapSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

        bool isGrounded = false;
        bool isTouchingWall = false;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider == null) continue;

            // �ٴ� ���̾���
            if (hitCollider.CompareTag("Ground"))
            {
                isGrounded = true;
                break;
            }

            //Debug.Log(hitCollider.name);
            Vector3 closestPoint = hitCollider.ClosestPoint(spherePosition); // �ݶ��̴����� ĳ���� ��ġ�� ���� ����� ����

            // �浹�� �ݶ��̴��� ǥ�� ������ ���ϱ� ���� Raycast ���
            RaycastHit hit;
            if (Physics.Raycast(spherePosition, hitCollider.transform.position - closestPoint, out hit, GroundedRadius, GroundLayers))
            {
                // ǥ���� ���� ���ͷ� ���� ���
                float angle = Vector3.Angle(hit.normal, Vector3.up);

                if (angle < 60f)
                {
                    // 60�� ������ ����: �ٴ����� ����
                    isGrounded = true;
                }
                else if (angle >= 60f && angle < 120f)
                {
                    // 60�� �̻� 120�� �̸��� ����: ������ ����
                    isTouchingWall = true;
                }
            }
        }

        // ���� ������Ʈ
        Grounded = isGrounded;
        WallAttached = isTouchingWall; // �߰��� �� üũ ���� ����

        // �ִϸ����� ������Ʈ
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }

    private void CameraRotation()
    {
        // if there is an input and camera position is not fixed
        if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            //Don't multiply mouse input by Time.deltaTime;
            //float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
            float deltaTimeMultiplier = 1.0f;

            _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw, 0.0f);
    }

    private void Move()
    {
        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

        // if there is no input, set the target speed to 0
        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        // normalise input direction
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // if there is a move input rotate player when the player is moving
        if (_input.move != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);

            // rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        // move the player
        _rigidbody.velocity = targetDirection.normalized * (targetSpeed * inputMagnitude) + new Vector3(0.0f, _rigidbody.velocity.y, 0.0f);

        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, targetSpeed);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            // reset the fall timeout timer
            _fallTimeoutDelta = FallTimeout;

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // stop our velocity dropping infinitely when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (_input.jump && _jumpTimeoutDelta <= 0.0f && Grounded && !WallAttached)
            {
                // ���� ���� - �ٴڿ� �پ� �ִ� ��쿡�� ���� ����
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }

            // if we are not grounded, do not jump
            _input.jump = false;
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }

        _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, _verticalVelocity, _rigidbody.velocity.z);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
    }

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

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.position, FootstepAudioVolume);
        }
    }

    /// <summary> Mantle ���� ���� </summary>
    private int _animIDMantle;
    private bool _isMantling = false;
    public LayerMask mantleLayerMask;  // ��Ʋ ������ ���̾� ����
    public float rayHeight = 0.9f;  // Ʈ���̽� ���� ����
    public float maxReachDistance = 0.5f;  // ���� Ʈ���̽� �ִ� �Ÿ�
    public float maxLedgeHeight = 0.9f;  // ��Ʋ ������ ��ֹ� �ִ� ����
    private Vector3 targetMantlePosition;

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

    System.Collections.IEnumerator MantleMovement()
    {
        Debug.Log("��Ʋ ���� ���� ��");
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDMantle, true);
        }

        Vector3 startPosition = targetMantlePosition + (transform.up * -1f) + (transform.forward * -0.4f); // ���� ��ġ�� �ִϸ��̼ǿ� �°� ������ �̵�
                                                                                                           //float duration = 0.833f;  // ��Ʋ �ִϸ��̼��� ����(�� 26������, ��� �ӵ� 0.5��� - 0.833�� * 2)
        float elapsedTime = 0f;

        // 0 ~ 17�����ӵ��� ���� 1��ŭ �̵�
        float firstPhaseDuration = 17f / 30f; // 17������ (30fps ���� �� 0.567��)
        Vector3 firstPhaseTarget = startPosition + transform.up * 1f;
        while (elapsedTime < firstPhaseDuration)
        {
            transform.position = Vector3.Lerp(startPosition, firstPhaseTarget, elapsedTime / firstPhaseDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 18 ~ 25�����ӵ��� ������ 0.4��ŭ �̵�
        float secondPhaseDuration = 8f / 30f; // 8������ (30fps ���� �� 0.267��)
        Vector3 secondPhaseTarget = firstPhaseTarget + transform.forward * 0.4f;
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
        // ��Ʋ �� ĳ���� ���¸� �⺻ ����(Idle)�� ��ȯ
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDMantle, false);
        }
        yield return null;
    }
}
