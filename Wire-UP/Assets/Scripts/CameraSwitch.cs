using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraSwitch : MonoBehaviour
{
    public CinemachineVirtualCamera normalCam; // 기본 모드 카메라
    public CinemachineVirtualCamera aimCam; // 조준 모드 카메라
    public RopeAction ropeAction; // RopeAction 스크립트 참조

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // 오른쪽 마우스 버튼을 누르면 조준 모드
        {
            ropeAction.aimMode = true; // RopeAction 스크립트의 aimMode 변수를 true로 변경
            aimCam.Priority = 20; // 조준 카메라 우선순위를 높여 활성화
            normalCam.Priority = 10; // 일반 카메라 우선순위를 낮춤
        }
        else if (Input.GetMouseButtonUp(1)) // 버튼을 놓으면 기본 모드로 돌아감
        {
            ropeAction.aimMode = false; // RopeAction 스크립트의 aimMode 변수를 false로 변경
            aimCam.Priority = 10; // 조준 카메라 우선순위를 낮춤
            normalCam.Priority = 20; // 일반 카메라 우선순위를 높여 활성화
        }
    }
}
