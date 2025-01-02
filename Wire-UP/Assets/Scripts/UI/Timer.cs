using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public Text timerText;         // �ð� ǥ�ÿ� UI Text
    private float elapsedTime;     // ��� �ð�(��)
    private bool isPaused = false; // �Ͻ����� ����

    private void Start()
    {
        timerText = GetComponent<Text>();
    }

    void Update()
    {
        // �Ͻ����� ���°� �ƴ� ���� �ð� ����
        if (!isPaused)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    /// UI(Text)�� ��� �ð� ���
    private void UpdateTimerUI()
    {
        // ��:��:�ʸ� ������ ��ȯ
        int hours = (int)(elapsedTime / 3600f);
        int minutes = (int)((elapsedTime % 3600f) / 60f);
        int seconds = (int)(elapsedTime % 60f);

        // "00:00:00" �������� �����ֱ�
        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
    }

    /// Ÿ�̸� �Ͻ�����
    public void PauseTimer()
    {
        isPaused = true;
    }

    /// Ÿ�̸� �ٽ� ����
    public void ResumeTimer()
    {
        isPaused = false;
    }

    /// Ÿ�̸� ����
    public void ResetTimer()
    {
        elapsedTime = 0f;
        UpdateTimerUI();
    }
}
