using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndingEvent : MonoBehaviour
{
    public SphereCollider sphereCollider;
    // 파티클 시스템이 장착된 게임 오브젝트 리스트
    public List<GameObject> particleObjects = new List<GameObject>();

    private void Start()
    {
        // 자식 오브젝트 중 파티클 시스템이 장착된 게임 오브젝트 찾기
        foreach (Transform child in transform)
        {
            if (child.GetComponentInChildren<ParticleSystem>() != null)
            {
                // 파티클 시스템이 장착된 게임 오브젝트 리스트에 추가
                particleObjects.Add(child.gameObject);
            }
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            Debug.Log("EndingEvent");
            // EndingEvent
            // 플레이어가 EndingEvent에 도달하면 파티클 시스템을 활성화
            foreach (GameObject particleObject in particleObjects)
            {
                // 게임 오브젝트의 파티클 가져오기
                ParticleSystem particleSystem = particleObject.GetComponentInChildren<ParticleSystem>();
                // 파티클 시스템 활성화
                particleSystem.Play();
            }
        }
    }
}
