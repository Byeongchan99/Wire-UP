using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public Text timerText;         // 시간 표시용 UI Text
    private float elapsedTime;     // 경과 시간(초)
    private bool isPaused = false; // 일시정지 여부

    private void Start()
    {
        timerText = GetComponent<Text>();
    }

    void Update()
    {
        // 일시정지 상태가 아닐 때만 시간 증가
        if (!isPaused)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    /// UI(Text)에 경과 시간 출력
    private void UpdateTimerUI()
    {
        // 시:분:초를 정수로 변환
        int hours = (int)(elapsedTime / 3600f);
        int minutes = (int)((elapsedTime % 3600f) / 60f);
        int seconds = (int)(elapsedTime % 60f);

        // "00:00:00" 형식으로 맞춰주기
        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
    }

    /// 타이머 일시정지
    public void PauseTimer()
    {
        isPaused = true;
    }

    /// 타이머 다시 시작
    public void ResumeTimer()
    {
        isPaused = false;
    }

    /// 타이머 리셋
    public void ResetTimer()
    {
        elapsedTime = 0f;
        UpdateTimerUI();
    }
}
