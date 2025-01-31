using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHat : MonoBehaviour
{
    public GameObject hat;

    private void Awake()
    {
        Debug.Log("모자 할당");
        hat = GameObject.Find("Player Hat").gameObject;
    }

    // 모자 획득 시 호출
    public void PickUpHat()
    {
        Debug.Log("모자 착용");
        hat.SetActive(true);
    }

    public void PutDownHat()
    {
        Debug.Log("모자 제거");
        hat.SetActive(false);
    }
}
