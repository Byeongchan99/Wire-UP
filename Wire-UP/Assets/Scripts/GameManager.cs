using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager instance; // �̱����� �Ҵ��� ���� ����
    public PlayerHat playerHat;
    public FullscreenUIManager fullscreenUIManager;
    public HUDManager HUDManager;

    public bool isPaused; // �Ͻ����� ����

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

        Init();
    }

    public void OnGameClear()
    {
        PlayerPrefs.SetInt("GameCleared", 1);

        // ��� ��ũ�� �ݿ�
        PlayerPrefs.Save();
    }

    public void Init()
    {
        isPaused = true;
        Time.timeScale = 0f;
        MouseOn();
        fullscreenUIManager.OnMain();
    }

    public void StartGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        MouseOff();
        fullscreenUIManager.OnResume();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // �ð� �帧�� ����
        MouseOn();
        fullscreenUIManager.OnPause();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // ���� �ӵ��� ���ƿ�
        MouseOff();
        fullscreenUIManager.OnResume();
    }

    public void MouseOn()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void MouseOff()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
