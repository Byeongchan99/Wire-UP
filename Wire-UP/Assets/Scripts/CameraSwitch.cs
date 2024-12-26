using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraSwitch : MonoBehaviour
{
    public CinemachineVirtualCamera normalCam; // 기본 모드 카메라
    public CinemachineVirtualCamera aimCam; // 조준 모드 카메라
    public RopeAction ropeAction;

    void Update()
    {
        // 카메라의 우선 순위를 조절하여 카메라 변경
        if (Input.GetMouseButtonDown(1)) // 오른쪽 마우스 버튼을 누르면 조준 모드
        {
            ropeAction.aimMode = true;
            aimCam.Priority = 20;
            normalCam.Priority = 10;
        }
        else if (Input.GetMouseButtonUp(1)) // 버튼을 놓으면 조준 모드 비활성화
        {
            ropeAction.aimMode = false;
            aimCam.Priority = 10;
            normalCam.Priority = 20;
        }
    }
}
