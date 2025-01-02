using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AimModeCrosshair : MonoBehaviour
{
    public RopeAction ropeAction;
    public Image crosshair;

    public void Update()
    {
        // 조준 모드일 때만 조준점을 활성화
        if (ropeAction.aimMode)
        {
            crosshair.enabled = true;

            Ray ray = ropeAction.cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            RaycastHit hit;

            // 조준점이 그래플링 가능한 물체에 닿았을 때 조준점 색상을 초록색으로 변경
            if (Physics.Raycast(ray, out hit, ropeAction.maxGrappleDistance, ropeAction.whatIsGrappleable) && ropeAction.canGrapple)
            {
                crosshair.color = Color.green;
            }
            else
            {
                crosshair.color = Color.red;
            }
        }
        else
        {
            crosshair.enabled = false;
        }
    }
}
