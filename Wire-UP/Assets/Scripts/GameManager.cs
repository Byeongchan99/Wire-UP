using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public PlayerHat playerHat;

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
}
