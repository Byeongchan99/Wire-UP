using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHat : MonoBehaviour
{
    public GameObject hat;

    private void Start()
    {
        hat.SetActive(false);
    }
    
    // ���� ȹ�� �� ȣ��
    public void PickUpHat()
    {
        hat.SetActive(true);
        GameManager.instance.OnGameClear(); // ���� Ŭ����
    }

    public void PutDownHat()
    {
        hat.SetActive(false);
    }
}
