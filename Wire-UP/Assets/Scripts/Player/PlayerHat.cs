using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHat : MonoBehaviour
{
    public GameObject hat;

    private void Start()
    {
        hat.SetActive(false);
    }
    
    // 모자 획득 시 호출
    public void PickUpHat()
    {
        hat.SetActive(true);
        GameManager.instance.OnGameClear(); // 게임 클리어
    }

    public void PutDownHat()
    {
        hat.SetActive(false);
    }
}
