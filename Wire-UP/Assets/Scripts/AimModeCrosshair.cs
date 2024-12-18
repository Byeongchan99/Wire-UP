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
        if (ropeAction.aimMode)
        {
            crosshair.enabled = true;

            Ray ray = ropeAction.cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, ropeAction.maxGrappleDistance, ropeAction.whatIsGrappleable))
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
