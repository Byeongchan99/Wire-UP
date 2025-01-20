using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullscreenUI : MonoBehaviour
{
    // UIView�� ���� ��ġ�� ������ �ʵ�
    private Vector2 _originalPosition;
    // RectTransform ������Ʈ�� ���� ����
    private RectTransform rectTransform;

    /// <summary> ���� �� UIView�� ���� ��ġ ���� </summary>
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        _originalPosition = rectTransform.anchoredPosition;
        //Hide();
    }

    /// <summary> UI ��Ҹ� �����ִ� �޼��� </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        // ȭ�� �߾����� �̵�
        //_rectTransform.DOAnchorPos(Vector2.zero, 0.5f).OnComplete(() => _state = VisibleState.Appeared);

        rectTransform.anchoredPosition = Vector2.zero;
    }

    /// <summary> UI ��Ҹ� ����� �޼��� </summary>
    public void Hide()
    {
        // ���� ��ġ�� �̵�
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
