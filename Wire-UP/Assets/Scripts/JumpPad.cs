using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPad : MonoBehaviour
{
    BoxCollider _boxColider;
    [SerializeField] float jumpForce = 0.005f; // �����е忡�� �÷��̾ ������ �� �������� ���� ����
    [SerializeField] float jumpDuration = 0.5f; // �����е忡�� �÷��̾ ������ �� �������� ���� ���ӽð�

    void Start()
    {
        _boxColider = GetComponent<BoxCollider>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            StartCoroutine(JumpOverTime(rb, jumpForce));
        }
    }

    IEnumerator JumpOverTime(Rigidbody rb, float totalForce)
    {
        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            // �̹� �����ӿ��� �� ��
            float frameForce = (totalForce / jumpDuration) * Time.deltaTime;
            rb.AddForce(Vector3.up * frameForce, ForceMode.Force);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

}
