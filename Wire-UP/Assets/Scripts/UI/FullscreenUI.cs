using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullscreenUI : MonoBehaviour
{
    // UIView의 원래 위치를 저장할 필드
    private Vector2 _originalPosition;
    // RectTransform 컴포넌트에 대한 참조
    private RectTransform rectTransform;

    /// <summary> 시작 시 UIView의 원래 위치 저장 </summary>
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        _originalPosition = rectTransform.anchoredPosition;
        //Hide();
    }

    /// <summary> UI 요소를 보여주는 메서드 </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        // 화면 중앙으로 이동
        //_rectTransform.DOAnchorPos(Vector2.zero, 0.5f).OnComplete(() => _state = VisibleState.Appeared);

        rectTransform.anchoredPosition = Vector2.zero;
    }

    /// <summary> UI 요소를 숨기는 메서드 </summary>
    public void Hide()
    {
        // 원래 위치로 이동
        /*
        _rectTransform.DOAnchorPos(_originalPosition, 0.5f).OnComplete(() =>
        {
            _state = VisibleState.Disappeared;
        });
        */
        rectTransform.anchoredPosition = _originalPosition;
        gameObject.SetActive(false);
    }
}
