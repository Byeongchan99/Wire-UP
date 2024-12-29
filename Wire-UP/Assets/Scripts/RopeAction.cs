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
    public bool aimMode = false; // ���� ���
    public float maxGrappleDistance; // �ִ� �׷��� �Ÿ�
    public bool canGrapple; // �׷��� ���� ����
    private SpringJoint _joint; // ������ ����Ʈ
    private Vector3 _grapplePoint; // �׷��� ����

    [Header("Cooldown")]
    public float grapplingCooldown; // �׷��ø� ��ٿ�
    private float _grapplingCooldownTimer; // �׷��ø� ��ٿ� Ÿ�̸�

    [Header("Swinging")]
    public float forceMultiplier; // ���߿��� ������ �� �������� ��
    private bool _connectedRope; // ���� ���� ����

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

        // ���߿��� �������� ���� �Ұ��� - ���߿� �뷱�� ����
        if (Input.GetMouseButtonDown(0) && canGrapple)
        {
            Debug.Log("StartGrapple");
            StartGrapple();
        }

        if (_grapplingCooldownTimer > 0)
            _grapplingCooldownTimer -= Time.deltaTime;

        // �߰��� �׷��ø� ���߱�
        if (Input.GetMouseButtonUp(0) && playerController.isSwinging)
        {
            StopGrapple();
        }
    }

    private void FixedUpdate()
    {
        // FixedUpdate���� ���� ó��
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

            // SpringJoint�� �÷��̾ �߰�
            _joint = playerRb.gameObject.AddComponent<SpringJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = _grapplePoint;

            float distanceFromPoint = Vector3.Distance(playerRb.position, _grapplePoint);

            // �׷��� ����Ʈ �ɼ� ����
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

        // ���� ���� ó��
        playerController.OnSwingEnd();

        // SpringJoint ����
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
            //Debug.Log("gunTip ȸ��: " + gunTip.rotation.eulerAngles);
        }
    }

    public void airMovement()
    {
        // �Էµ� �̵� ���� �����ɴϴ�.
        Vector2 moveInput = _input.move;

        // �Է��� ������ �������� �������� �ʽ��ϴ�.
        if (moveInput == Vector2.zero)
            return;

        // ī�޶��� ȸ���� ������� �̵� ������ ����մϴ�.
        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;

        // ����鿡 �����Ͽ� y���� 0���� ����ϴ�.
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // �̵� ���� ���
        Vector3 moveDirection = camForward * moveInput.y + camRight * moveInput.x;
        moveDirection.Normalize();

        // �� ���ϱ�
        playerRb.AddForce(moveDirection * forceMultiplier * Time.deltaTime, ForceMode.Force);
    }
}
