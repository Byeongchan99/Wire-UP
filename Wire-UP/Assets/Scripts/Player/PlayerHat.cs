using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHat : MonoBehaviour
{
    public GameObject hat;

    private void Awake()
    {
        Debug.Log("���� �Ҵ�");
        hat = GameObject.Find("Player Hat").gameObject;
    }

    // ���� ȹ�� �� ȣ��
    public void PickUpHat()
    {
        Debug.Log("���� ����");
        hat.SetActive(true);
    }

    public void PutDownHat()
    {
        Debug.Log("���� ����");
        hat.SetActive(false);
    }
}
