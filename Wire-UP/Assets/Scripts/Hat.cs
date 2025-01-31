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
        // y�� �߽����� ȸ��
        transform.Rotate(Vector3.up * Time.deltaTime * 100);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            PlayerHat playerHat = other.GetComponent<PlayerHat>();
            GameManager.instance.OnGameClear(); // ���� Ŭ���� ����
            playerHat.PickUpHat();
            Destroy(gameObject);
        }
    }
}
