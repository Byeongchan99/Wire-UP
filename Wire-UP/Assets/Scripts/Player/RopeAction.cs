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

    // ������ "���ư���" ȿ���� ���� �Ķ����
    [Header("Rope Travel Settings")]
    [Tooltip("������ ��ǥ �������� ������� �� �ɸ��� �ð�")]
    public float ropeExtendDuration = 0.3f;
    private float _ropeExtendTimer; // ���� ���� Ȯ�� ��� �ð�
    private bool _isExtending; // ������ ���� ����� ������ ����

    // ������ "��鸮��" ȿ���� ���� �Ķ����
    [Header("Rope Wave Settings")]
    [Tooltip("���η����� �󿡼� �� ���� ��(���׸�Ʈ)�� �������")]
    public int ropeSegmentCount = 20;

    [Tooltip("AnimationCurve�� �ĵ� ����.\nX�� = 0 ~ 1, Y�� = ����(Amplitude) ������")]
    public AnimationCurve ropeCurve;

    [Tooltip("�ĵ��� �ð��� ���� �帣�� �ӵ�(���� Ŭ���� ������ �帧)")]
    public float waveSpeed = 2.0f;

    [Tooltip("�ĵ��� �⺻ ����(���� ������)")]
    public float waveAmplitude = 0.2f;

    // �ĵ� �ð� ������
    private float _waveTimer = 0f;


    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        // ���� ���� ���� ����
        if (aimMode == true && playerController.isSwinging == false && playerController.isjustSwingEnded == false)
            canGrapple = true;
        else
            canGrapple = false;

        // ���콺 ��Ŭ�� �� �׷��� �õ�
        if (Input.GetMouseButtonDown(0) && canGrapple)
        {
            StartGrapple();
        }

        // ��Ÿ�� ����
        if (_grapplingCooldownTimer > 0)
            _grapplingCooldownTimer -= Time.deltaTime;

        // ���콺 ��Ŭ�� ���� �� ���� ����
        if (Input.GetMouseButtonUp(0) && playerController.isSwinging)
        {
            StopGrapple();
        }
    }

    private void FixedUpdate()
    {
        // ���� ���� �� �̵� ó��
        if (playerController.isSwinging && _joint != null && !playerController.isGrounded)
        {
            airMovement();
        }
    }

    private void LateUpdate()
    {
        // ���� �׸��� (�ð� ������Ʈ)
        DrawRope();
    }

    // �׷��� ����
    private void StartGrapple()
    {
        // ��Ÿ�� ���̸� ����
        if (_grapplingCooldownTimer > 0)
            return;

        // ȭ�� ���߾ӿ��� ���� ���
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            _grapplePoint = hit.point;

            // �ٷ� ����Ʈ ���� (���������δ� �̹� �پ��ִ� ����)
            _joint = playerRb.gameObject.AddComponent<SpringJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = _grapplePoint;

            float distanceFromPoint = Vector3.Distance(playerRb.position, _grapplePoint);

            // spring joint ���� ����
            _joint.maxDistance = distanceFromPoint * 0.8f;
            _joint.minDistance = distanceFromPoint * 0.25f;
            _joint.spring = 10f;
            _joint.damper = 50f;
            _joint.massScale = 1f;

            // ���η����� �ʱ�ȭ
            _lineRenderer.enabled = true;
            _lineRenderer.positionCount = ropeSegmentCount;

            // ������ ���ư��� ����
            _ropeExtendTimer = 0f;
            _isExtending = true;

            // �ĵ��ð� �ʱ�ȭ
            _waveTimer = 0f;

            playerController.isSwinging = true;
            _connectedRope = true;
        }
    }

    // �׷��� �ߴ�(���� ����)
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

        // ���� Ȯ�� �÷��� ����
        _isExtending = false;
    }

    // ���� �׸���
    private void DrawRope()
    {
        // ������� �ʾҴٸ� �׸� �� ����
        if (!_connectedRope) return;

        // ���׸�Ʈ�� 2���� ������ �׸� �� ����
        if (ropeSegmentCount < 2) return;

        // ������ ������ �������� üũ
        _ropeExtendTimer += Time.deltaTime;
        
        float extendRatio = 0f;

        if (_isExtending)
        {
            // 0 ~ 1�� ������ Ȯ�� ����
            extendRatio = Mathf.Clamp01(_ropeExtendTimer / ropeExtendDuration);
            // 1 �̻��̸� �� �̻� Ȯ�� ���� �ƴ�
            if (extendRatio >= 1f)
            {
                _isExtending = false;
                extendRatio = 1f;
            }
        }
        else
        {
            // �̹� ���� �ڿ��� 1�� ����(������ ��� ����)
            extendRatio = 1f;
        }

        // ��鸲�� ���� waveTimer ����
        _waveTimer += Time.deltaTime * waveSpeed;

        Vector3 direction = (_grapplePoint - gunTip.position); // gunTip���� grapplePoint������ ����
        float fullDist = direction.magnitude; // ���� �����ؾ� �� �Ÿ�
        direction.Normalize();

        // ���� ���� ���
        Vector3 perp = Vector3.Cross(direction, Vector3.up);
        if (perp.sqrMagnitude < 0.001f)
        {
            // ���� ���� ���迩�� cross�� 0������ �ٸ� ������ ����
            perp = Vector3.Cross(direction, Vector3.right);
        }
        perp.Normalize();

        // �� ���׸�Ʈ�� ��ġ ������Ʈ
        for (int i = 0; i < ropeSegmentCount; i++)
        {
            // 0 ~ 1 �������� ���� ���׸�Ʈ�� �����ϴ� ����
            float t = (float)i / (ropeSegmentCount - 1);

            // 1. ������ ����� ���� - ��ü �Ÿ� �� ���� extendRatio��ŭ�� ���
            float segmentDist = (fullDist * extendRatio) * t;
            // �� ���׸�Ʈ�� ������ �̷��� ��ġ
            Vector3 basePos = gunTip.position + direction * segmentDist;

            // 2. �ִϸ��̼� Ŀ�긦 �̿��� ������ ���ư� �� ��鸮�� ����
            // t = 0 (�� ù ��)�� ���� ������ = 0 (gunTip ����)
            // t = 1 (������ ��)�� ���� �ִ� �ĵ�
            // ropeCurve�� ���� �ĵ����� ���, waveAmplitude * t �� ũ�� ����
            float curveValue = ropeCurve.Evaluate(t + _waveTimer);

            // gunTip ��( t=0 )�� 0�� ������, grapple ��( t=1 )�� waveAmplitude�� ������
            float waveSize = curveValue * waveAmplitude * t;

            // perp(��������)�� ���̺� ������ ����
            Vector3 waveOffset = perp * waveSize;

            // ���� ��ġ
            Vector3 finalPos = basePos + waveOffset;

            // 3. ������ ���׸�Ʈ�� ������ ������ ���� ���¶�� _grapplePoint�� ����
            if (extendRatio >= 1f && i == ropeSegmentCount - 1)
            {
                finalPos = _grapplePoint;
            }

            // ���η������� ����
            _lineRenderer.SetPosition(i, finalPos);
        }

        transform.LookAt(_grapplePoint); // ���� �׷��� ������ �ٶ󺸵��� ����
    }

    // ���� �� ���߿��� ������ ó��
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
