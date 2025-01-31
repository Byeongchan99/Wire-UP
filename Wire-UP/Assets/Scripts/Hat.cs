using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hat : MonoBehaviour
{
    private SphereCollider sphereCollider;

    void Start()
    {
        sphereCollider = GetComponent<SphereCollider>();    
    }

    void Update()
    {
        // y축 중심으로 회전
        transform.Rotate(Vector3.up * Time.deltaTime * 100);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            PlayerHat playerHat = other.GetComponent<PlayerHat>();
            GameManager.instance.OnGameClear(); // 게임 클리어 저장
            playerHat.PickUpHat();
            Destroy(gameObject);
        }
    }
}
