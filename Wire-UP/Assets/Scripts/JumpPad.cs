using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPad : MonoBehaviour
{
    private BoxCollider _boxColider;
    [SerializeField] private float _jumpForce = 100f; // �����е忡�� �÷��̾ ������ �� �������� ���� ����
    [SerializeField] private float _jumpDuration = 0.5f; // �����е忡�� �÷��̾ ������ �� �������� ���� ���ӽð�

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
            //StartCoroutine(JumpOverTime(rb, _jumpForce));
            rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        }
    }

    IEnumerator JumpOverTime(Rigidbody rb, float totalForce)
    {
        float elapsed = 0f;
        while (elapsed < _jumpDuration)
        {
            // �̹� �����ӿ��� �� ��
            float frameForce = (totalForce / _jumpDuration) * Time.deltaTime;
            rb.AddForce(Vector3.up * frameForce, ForceMode.Force);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

}
