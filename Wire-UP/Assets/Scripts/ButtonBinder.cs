using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonBinder : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button startButton;
    public Button exitButton;
    public Button resumeButton;
    public Button restartButton;

    private void Awake()
    {
        // 중복 방지 위해 기존 리스너 제거
        if (startButton != null)
            startButton.onClick.RemoveAllListeners();
        if (exitButton != null)
            exitButton.onClick.RemoveAllListeners();

        // Start 버튼
        if (startButton != null)
        {
            startButton.onClick.AddListener(() =>
            {
                GameManager.instance.StartGame();
            });
        }

        // Exit 버튼
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() =>
            {
                GameManager.instance.ExitGame();
            });
        }

        // Back 버튼
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(() =>
            {
                GameManager.instance.ResumeGame();
            });
        }

        // Main 버튼
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() =>
            {
                GameManager.instance.RestartGame();
            });
        }
    }
}
