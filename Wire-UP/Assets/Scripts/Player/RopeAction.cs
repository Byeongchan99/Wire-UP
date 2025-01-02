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
    public bool aimMode = false; // 조준 모드
    public float maxGrappleDistance; // 최대 그래플 거리
    public bool canGrapple; // 그래플 가능 여부
    private SpringJoint _joint; // 스프링 조인트
    private Vector3 _grapplePoint; // 그래플 지점

    [Header("Cooldown")]
    public float grapplingCooldown; // 그래플링 쿨다운
    private float _grapplingCooldownTimer; // 그래플링 쿨다운 타이머

    [Header("Swinging")]
    public float forceMultiplier; // 공중에서 움직일 때 가해지는 힘
    private bool _connectedRope; // 로프 연결 여부

    [Header("Input")]
    public KeyCode grappleKey = KeyCode.Mouse1;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (aimMode == true && playerController.isSwinging == false && playerController.isjustSwingEnded == false)
        {
            canGrapple = true;
        }
        else
        {
            canGrapple = false;
        }

        // 공중에서 연속으로 스윙 불가능 - 나중에 밸런스 조절
        if (Input.GetMouseButtonDown(0) && canGrapple)
        {
            Debug.Log("StartGrapple");
            StartGrapple();
        }

        if (_grapplingCooldownTimer > 0)
            _grapplingCooldownTimer -= Time.deltaTime;

        // 중간에 그래플링 멈추기
        if (Input.GetMouseButtonUp(0) && playerController.isSwinging)
        {
            StopGrapple();
        }
    }

    private void FixedUpdate()
    {
        // FixedUpdate에서 스윙 처리
        if (playerController.isSwinging && _joint != null && !playerController.isGrounded)
        {           
            airMovement();     
        }
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    private void StartGrapple()
    {
        if (_grapplingCooldownTimer > 0)
            return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            _grapplePoint = hit.point;

            // SpringJoint를 플레이어에 추가
            _joint = playerRb.gameObject.AddComponent<SpringJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = _grapplePoint;

            float distanceFromPoint = Vector3.Distance(playerRb.position, _grapplePoint);

            // 그래플 조인트 옵션 설정
            _joint.maxDistance = distanceFromPoint * 0.8f;
            _joint.minDistance = distanceFromPoint * 0.25f;

            /*
            joint.maxDistance = distanceFromPoint;
            joint.minDistance = distanceFromPoint;
            */

            _joint.spring = 5f;
            _joint.damper = 50f;
            _joint.massScale = 1f;

            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, gunTip.position);
            _lineRenderer.SetPosition(1, _grapplePoint);
            _lineRenderer.enabled = true;

            playerController.isSwinging = true;
            _connectedRope = true;
        }       
    }

    public void StopGrapple()
    {
        playerController.isSwinging = false;
        _connectedRope = false;

        // 스윙 종료 처리
        playerController.OnSwingEnd();

        // SpringJoint 제거
        if (_joint != null)
        {
            Destroy(_joint);
        }

        _lineRenderer.positionCount = 0;
        _lineRenderer.enabled = false;

        _grapplingCooldownTimer = grapplingCooldown;
    }

    private void DrawRope()
    {
        if (_connectedRope)
        {
            _lineRenderer.SetPosition(0, gunTip.position);
            _lineRenderer.SetPosition(1, _grapplePoint);
            transform.LookAt(_grapplePoint);
            //Debug.Log("gunTip 회전: " + gunTip.rotation.eulerAngles);
        }
    }

    public void airMovement()
    {
        // 입력된 이동 값을 가져옵니다.
        Vector2 moveInput = _input.move;

        // 입력이 없으면 움직임을 적용하지 않습니다.
        if (moveInput == Vector2.zero)
            return;

        // 카메라의 회전을 기반으로 이동 방향을 계산합니다.
        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;

        // 수평면에 투영하여 y값을 0으로 만듭니다.
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // 이동 방향 계산
        Vector3 moveDirection = camForward * moveInput.y + camRight * moveInput.x;
        moveDirection.Normalize();

        // 힘 가하기
        playerRb.AddForce(moveDirection * forceMultiplier * Time.deltaTime, ForceMode.Force);
    }
}
