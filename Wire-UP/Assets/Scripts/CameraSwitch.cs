using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraSwitch : MonoBehaviour
{
    public CinemachineVirtualCamera normalCam; // �⺻ ��� ī�޶�
    public CinemachineVirtualCamera aimCam; // ���� ��� ī�޶�
    public RopeAction ropeAction;

    void Update()
    {
        // ī�޶��� �켱 ������ �����Ͽ� ī�޶� ����
        if (Input.GetMouseButtonDown(1)) // ������ ���콺 ��ư�� ������ ���� ���
        {
            ropeAction.aimMode = true;
            aimCam.Priority = 20;
            normalCam.Priority = 10;
        }
        else if (Input.GetMouseButtonUp(1)) // ��ư�� ������ ���� ��� ��Ȱ��ȭ
        {
            ropeAction.aimMode = false;
            aimCam.Priority = 10;
            normalCam.Priority = 20;
        }
    }
}
