using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private GameObject timerUI; // Ÿ�̸� UI

    // Ÿ�̸� UI Ȱ��ȭ
    public void EnableTimer()
    {
        timerUI.SetActive(true);
    }

    // Ÿ�̸� UI ��Ȱ��ȭ
    public void DisableTimer()
    {
        timerUI.SetActive(false);
    }
}
