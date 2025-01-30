using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {      
        Init();
    }

    private void OnDestroy()
    {
        // �ߺ� ���/�޸� ���� ����
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ���� �ε�� ��, ���� ������ FullscreenUIManager ã�Ƽ� �Ҵ�
        fullscreenUIManager = FindObjectOfType<FullscreenUIManager>();      
    }

    public void OnGameClear()
    {
        PlayerPrefs.SetInt("GameCleared", 1);

        // ��� ��ũ�� �ݿ�
        PlayerPrefs.Save();
    }

    public void Init()
    {
        Debug.Log("���� �ʱ�ȭ");
        int clearValue = PlayerPrefs.GetInt("GameCleared");

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

        isPaused = true;
        Time.timeScale = 0f;
        MouseOn();
        //fullscreenUIManager.OnMain();
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

    public void RestartGame()
    {
        Debug.Log("���� �����");
        //UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Init();
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(); // ���ø����̼� ����
#endif
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
