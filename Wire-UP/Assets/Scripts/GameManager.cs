using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance; // 싱글톤을 할당할 전역 변수
    public PlayerHat playerHat;
    public bool isPaused = false; // 일시정지 여부

    private void Awake()
    {
        if (instance == null)
        {
            instance = this; // 첫 번째 생성된 GameManager 인스턴스에 대해 전역 변수 할당
            DontDestroyOnLoad(gameObject); // 씬 전환이 되더라도 삭제되지 않게 함
        }
        else
        {
            Destroy(gameObject); // 두 번째 이후 생성된 GameManager 인스턴스는 삭제
        }
    }

    void Start()
    {
        int clearValue = PlayerPrefs.GetInt("GameCleared", 0); // 기본값 0

        if (clearValue == 1)
        {
            Debug.Log("게임 클리어");
            playerHat.PickUpHat();
        }
        else
        {
            Debug.Log("게임 클리어 실패");
            playerHat.PutDownHat();
        }
    }

    public void OnGameClear()
    {
        PlayerPrefs.SetInt("GameCleared", 1);

        // 즉시 디스크에 반영
        PlayerPrefs.Save();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // 시간 흐름을 멈춤
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // 정상 속도로 돌아옴
    }
}
