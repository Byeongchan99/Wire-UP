using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance; // �̱����� �Ҵ��� ���� ����
    public PlayerHat playerHat;
    public bool isPaused = false; // �Ͻ����� ����

    private void Awake()
    {
        if (instance == null)
        {
            instance = this; // ù ��° ������ GameManager �ν��Ͻ��� ���� ���� ���� �Ҵ�
            DontDestroyOnLoad(gameObject); // �� ��ȯ�� �Ǵ��� �������� �ʰ� ��
        }
        else
        {
            Destroy(gameObject); // �� ��° ���� ������ GameManager �ν��Ͻ��� ����
        }
    }

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

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // �ð� �帧�� ����
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // ���� �ӵ��� ���ƿ�
    }
}
