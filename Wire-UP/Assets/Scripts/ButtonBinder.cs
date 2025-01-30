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
        // �ߺ� ���� ���� ���� ������ ����
        if (startButton != null)
            startButton.onClick.RemoveAllListeners();
        if (exitButton != null)
            exitButton.onClick.RemoveAllListeners();

        // Start ��ư
        if (startButton != null)
        {
            startButton.onClick.AddListener(() =>
            {
                GameManager.instance.StartGame();
            });
        }

        // Exit ��ư
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() =>
            {
                GameManager.instance.ExitGame();
            });
        }

        // Back ��ư
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(() =>
            {
                GameManager.instance.ResumeGame();
            });
        }

        // Main ��ư
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() =>
            {
                GameManager.instance.RestartGame();
            });
        }
    }
}
