using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndingEvent : MonoBehaviour
{
    public SphereCollider sphereCollider;
    // ��ƼŬ �ý����� ������ ���� ������Ʈ ����Ʈ
    public List<GameObject> particleObjects = new List<GameObject>();

    private void Start()
    {
        // �ڽ� ������Ʈ �� ��ƼŬ �ý����� ������ ���� ������Ʈ ã��
        foreach (Transform child in transform)
        {
            if (child.GetComponentInChildren<ParticleSystem>() != null)
            {
                // ��ƼŬ �ý����� ������ ���� ������Ʈ ����Ʈ�� �߰�
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
            // �÷��̾ EndingEvent�� �����ϸ� ��ƼŬ �ý����� Ȱ��ȭ
            foreach (GameObject particleObject in particleObjects)
            {
                // ���� ������Ʈ�� ��ƼŬ ��������
                ParticleSystem particleSystem = particleObject.GetComponentInChildren<ParticleSystem>();
                // ��ƼŬ �ý��� Ȱ��ȭ
                particleSystem.Play();
            }
        }
    }
}
