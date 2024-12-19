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
    public LineRenderer lr;
    public PlayerMovement characterController;
    public Transform playerTransform;
    public Rigidbody playerRb;
    public PlayerNewInput _input;

    [Header("Grappling")]
    public float maxGrappleDistance;
    public float grappleDelayTime;
    public float overshootYAxis;

    private Vector3 grapplePoint;

    [Header("Swinging")]
    //public float horizontalThrustForce;
    //public float forwardThrustForce;
    //public float extendCableSpeed;
    public float forceMultiplier;
    public bool connectedRope;
    public bool canGrapple;

    [Header("Cooldown")]
    public float grapplingCd;
    private float grapplingCdTimer;

    [Header("Input")]
    public KeyCode grappleKey = KeyCode.Mouse1;

    public bool aimMode = false; // ���� ���
    private SpringJoint joint;
    public Rigidbody rb;

    public float pullForce = 200f; // �ʿ信 ���� ���� ������ �� ������/������ �̵�

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (aimMode == true && characterController.swinging == false && characterController.isSwingEnded == false)
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

        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;

        // �߰��� �׷��ø� ���߱�
        if (Input.GetMouseButtonUp(0) && characterController.swinging)
        {
            StopGrapple();
        }

        if (joint != null && !characterController.grounded)
        {
            airMovement();
        }
    }

    private void FixedUpdate()
    {
        // FixedUpdate���� �׷��ø� ���� ����
        if (characterController.swinging)
        {
            //ApplyGrapplingForce();
        }
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    private void StartGrapple()
    {
        if (grapplingCdTimer > 0)
            return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point;

            // SpringJoint�� �÷��̾ �߰�
            joint = rb.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            float distanceFromPoint = Vector3.Distance(rb.position, grapplePoint);
          
            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;
            
            /*
            joint.maxDistance = distanceFromPoint;
            joint.minDistance = distanceFromPoint;
            */

            joint.spring = 5f;
            joint.damper = 50f;
            joint.massScale = 1f;

            lr.positionCount = 2;
            lr.SetPosition(0, gunTip.position);
            lr.SetPosition(1, grapplePoint);
            lr.enabled = true;

            characterController.swinging = true;
            connectedRope = true;

            // ExecuteGrapple�� FixedUpdate���� ���������� ó���ϵ��� ����
            Invoke(nameof(ExecuteGrapple), grappleDelayTime);
        }       
    }

    private void ExecuteGrapple()
    {
        // ExecuteGrapple�� �ʱ� �������� �ʿ��ϰ�,
        // �������� �� ������ FixedUpdate���� ó���մϴ�.
        if (joint == null) return;

        // �׷��ø� ���� �������� �ణ�� ������ �� �� ���� ���������� ����
        characterController.swinging = true;
    }

    /*
    private void ApplyGrapplingForce()
    {
        // �÷��̾� Rigidbody�� ���� ����
        if (rb != null && isGrappling)
        {
            Vector3 directionToGrapplePoint = (grapplePoint - rb.position).normalized;

            // ��ǥ �������� ĳ���͸� ���� ���� ������ ���� ����         
            rb.AddForce(directionToGrapplePoint * pullForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
    }
    */

    public void StopGrapple()
    {
        characterController.swinging = false;
        connectedRope = false;

        // ���� ���� ó��
        characterController.OnSwingEnd();

        // SpringJoint ����
        if (joint != null)
        {
            Destroy(joint);
        }

        lr.positionCount = 0;
        lr.enabled = false;

        grapplingCdTimer = grapplingCd;
    }

    private void DrawRope()
    {
        if (connectedRope)
        {
            lr.SetPosition(0, gunTip.position);
            lr.SetPosition(1, grapplePoint);
            transform.LookAt(grapplePoint);
            //Debug.Log("gunTip ȸ��: " + gunTip.rotation.eulerAngles);
        }
    }

    public bool IsGrappling()
    {
        return characterController.swinging;
    }

    public Vector3 GetGrapplePoint()
    {
        return grapplePoint;
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
        rb.AddForce(moveDirection * forceMultiplier * Time.deltaTime, ForceMode.Force);
    }
}
