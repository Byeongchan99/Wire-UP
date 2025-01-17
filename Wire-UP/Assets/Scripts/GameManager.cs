using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public PlayerHat playerHat;

    void Start()
    {
        int clearValue = PlayerPrefs.GetInt("GameCleared", 0); // �⺻�� 0

        if (clearValue == 1)
        {
            Debug.Log("���� Ŭ����");
            playerHat.PickUpHat();
        }
        else
        {
            Debug.Log("���� Ŭ���� ����");
            playerHat.PutDownHat();
        }
    }

    public void OnGameClear()
    {
        PlayerPrefs.SetInt("GameCleared", 1);

        // ��� ��ũ�� �ݿ�
        PlayerPrefs.Save();
    }
}
