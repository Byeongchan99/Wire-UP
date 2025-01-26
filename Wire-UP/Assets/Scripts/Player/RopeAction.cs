using Cinemachine.Utility;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeAction : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform gunTip;
    public LayerMask whatIsGrappleable;
    private LineRenderer _lineRenderer;
    public PlayerController playerController;
    public Transform playerTransform;
    public Rigidbody playerRb;
    public PlayerNewInput _input;

    [Header("Grappling")]
    public bool aimMode = false;
    public float maxGrappleDistance;
    public bool canGrapple;
    private SpringJoint _joint;
    private Vector3 _grapplePoint;

    [Header("Cooldown")]
    public float grapplingCooldown;
    private float _grapplingCooldownTimer;

    [Header("Swinging")]
    public float forceMultiplier;
    private bool _connectedRope;

    [Header("Input")]
    public KeyCode grappleKey = KeyCode.Mouse1;

    // 로프가 "날아가는" 효과를 위한 파라미터
    [Header("Rope Travel Settings")]
    [Tooltip("로프가 목표 지점까지 뻗어나가는 데 걸리는 시간")]
    public float ropeExtendDuration = 0.3f;
    private float _ropeExtendTimer; // 현재 로프 확장 경과 시간
    private bool _isExtending; // 로프가 지금 뻗어가는 중인지 여부

    // 로프가 "흔들리는" 효과를 위한 파라미터
    [Header("Rope Wave Settings")]
    [Tooltip("라인렌더러 상에서 몇 개의 점(세그먼트)을 사용할지")]
    public int ropeSegmentCount = 20;

    [Tooltip("AnimationCurve로 파동 제어.\nX축 = 0 ~ 1, Y축 = 진폭(Amplitude) 조절값")]
    public AnimationCurve ropeCurve;

    [Tooltip("파동이 시간에 따라 흐르는 속도(값이 클수록 빠르게 흐름)")]
    public float waveSpeed = 2.0f;

    [Tooltip("파동의 기본 진폭(세로 스케일)")]
    public float waveAmplitude = 0.2f;

    // 파동 시간 추적용
    private float _waveTimer = 0f;


    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        // 스윙 가능 상태 갱신
        if (aimMode == true && playerController.isSwinging == false && playerController.isjustSwingEnded == false)
            canGrapple = true;
        else
            canGrapple = false;

        // 마우스 좌클릭 시 그래플 시도
        if (Input.GetMouseButtonDown(0) && canGrapple)
        {
            StartGrapple();
        }

        // 쿨타임 감소
        if (_grapplingCooldownTimer > 0)
            _grapplingCooldownTimer -= Time.deltaTime;

        // 마우스 좌클릭 해제 시 스윙 종료
        if (Input.GetMouseButtonUp(0) && playerController.isSwinging)
        {
            StopGrapple();
        }
    }

    private void FixedUpdate()
    {
        // 공중 스윙 중 이동 처리
        if (playerController.isSwinging && _joint != null && !playerController.isGrounded)
        {
            airMovement();
        }
    }

    private void LateUpdate()
    {
        // 로프 그리기 (시각 업데이트)
        DrawRope();
    }

    // 그래플 시작
    private void StartGrapple()
    {
        // 쿨타임 중이면 리턴
        if (_grapplingCooldownTimer > 0)
            return;

        // 화면 정중앙에서 레이 쏘기
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            _grapplePoint = hit.point;

            // 바로 조인트 연결 (물리적으로는 이미 붙어있는 상태)
            _joint = playerRb.gameObject.AddComponent<SpringJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = _grapplePoint;

            float distanceFromPoint = Vector3.Distance(playerRb.position, _grapplePoint);

            // spring joint 세부 설정
            _joint.maxDistance = distanceFromPoint * 0.8f;
            _joint.minDistance = distanceFromPoint * 0.25f;
            _joint.spring = 10f;
            _joint.damper = 50f;
            _joint.massScale = 1f;

            // 라인렌더러 초기화
            _lineRenderer.enabled = true;
            _lineRenderer.positionCount = ropeSegmentCount;

            // 로프가 날아가기 시작
            _ropeExtendTimer = 0f;
            _isExtending = true;

            // 파동시간 초기화
            _waveTimer = 0f;

            playerController.isSwinging = true;
            _connectedRope = true;
        }
    }

    // 그래플 중단(스윙 종료)
    public void StopGrapple()
    {
        playerController.isSwinging = false;
        _connectedRope = false;

        playerController.OnSwingEnd();

        if (_joint != null)
        {
            Destroy(_joint);
        }

        _lineRenderer.positionCount = 0;
        _lineRenderer.enabled = false;

        _grapplingCooldownTimer = grapplingCooldown;

        // 로프 확장 플래그 해제
        _isExtending = false;
    }

    // 로프 그리기
    private void DrawRope()
    {
        // 연결되지 않았다면 그릴 것 없음
        if (!_connectedRope) return;

        // 세그먼트가 2보다 작으면 그릴 수 없음
        if (ropeSegmentCount < 2) return;

        // 로프가 완전히 뻗었는지 체크
        _ropeExtendTimer += Time.deltaTime;
        
        float extendRatio = 0f;

        if (_isExtending)
        {
            // 0 ~ 1로 보정된 확장 비율
            extendRatio = Mathf.Clamp01(_ropeExtendTimer / ropeExtendDuration);
            // 1 이상이면 더 이상 확장 중이 아님
            if (extendRatio >= 1f)
            {
                _isExtending = false;
                extendRatio = 1f;
            }
        }
        else
        {
            // 이미 뻗은 뒤에는 1로 유지(완전히 닿아 있음)
            extendRatio = 1f;
        }

        // 흔들림을 위한 waveTimer 누적
        _waveTimer += Time.deltaTime * waveSpeed;

        Vector3 direction = (_grapplePoint - gunTip.position); // gunTip에서 grapplePoint까지의 방향
        float fullDist = direction.magnitude; // 최종 도달해야 할 거리
        direction.Normalize();

        // 수직 벡터 계산
        Vector3 perp = Vector3.Cross(direction, Vector3.up);
        if (perp.sqrMagnitude < 0.001f)
        {
            // 만약 평행 관계여서 cross가 0나오면 다른 축으로 교차
            perp = Vector3.Cross(direction, Vector3.right);
        }
        perp.Normalize();

        // 각 세그먼트별 위치 업데이트
        for (int i = 0; i < ropeSegmentCount; i++)
        {
            // 0 ~ 1 구간에서 현재 세그먼트가 차지하는 비율
            float t = (float)i / (ropeSegmentCount - 1);

            // 1. 로프가 뻗어가는 연출 - 전체 거리 중 현재 extendRatio만큼만 사용
            float segmentDist = (fullDist * extendRatio) * t;
            // 이 세그먼트가 도달할 이론적 위치
            Vector3 basePos = gunTip.position + direction * segmentDist;

            // 2. 애니메이션 커브를 이용한 로프가 날아갈 때 흔들리는 연출
            // t = 0 (총 첫 점)일 때는 오프셋 = 0 (gunTip 고정)
            // t = 1 (마지막 점)일 때는 최대 파동
            // ropeCurve를 통해 파동값을 얻고, waveAmplitude * t 로 크기 조절
            float curveValue = ropeCurve.Evaluate(t + _waveTimer);

            // gunTip 쪽( t=0 )은 0에 가깝게, grapple 쪽( t=1 )은 waveAmplitude에 가깝게
            float waveSize = curveValue * waveAmplitude * t;

            // perp(수직방향)에 웨이브 오프셋 적용
            Vector3 waveOffset = perp * waveSize;

            // 최종 위치
            Vector3 finalPos = basePos + waveOffset;

            // 3. 마지막 세그먼트가 로프가 완전히 뻗은 상태라면 _grapplePoint에 고정
            if (extendRatio >= 1f && i == ropeSegmentCount - 1)
            {
                finalPos = _grapplePoint;
            }

            // 라인렌더러에 적용
            _lineRenderer.SetPosition(i, finalPos);
        }

        transform.LookAt(_grapplePoint); // 총이 그래플 지점을 바라보도록 설정
    }

    // 스윙 중 공중에서 움직임 처리
    public void airMovement()
    {
        Vector2 moveInput = _input.move;
        if (moveInput == Vector2.zero)
            return;

        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * moveInput.y + camRight * moveInput.x;
        moveDirection.Normalize();

        playerRb.AddForce(moveDirection * forceMultiplier * Time.deltaTime, ForceMode.Force);
    }
}
