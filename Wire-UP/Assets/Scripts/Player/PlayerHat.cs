using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHat : MonoBehaviour
{
    public GameObject hat;
    public GameManager gameManager;

    private void Start()
    {
        hat.SetActive(false);
    }
    
    // ���� ȹ�� �� ȣ��
    public void PickUpHat()
    {
        hat.SetActive(true);
        gameManager.OnGameClear(); // ���� Ŭ����
    }

    public void PutDownHat()
    {
        hat.SetActive(false);
    }
}
