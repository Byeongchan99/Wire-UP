using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private GameObject timerUI; // 타이머 UI

    // 타이머 UI 활성화
    public void EnableTimer()
    {
        timerUI.SetActive(true);
    }

    // 타이머 UI 비활성화
    public void DisableTimer()
    {
        timerUI.SetActive(false);
    }
}
