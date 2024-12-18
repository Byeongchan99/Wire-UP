using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class StarterAssetsInputs : MonoBehaviour
{
    [Header("Character Input Values")]
    public Vector2 move;
    public Vector2 look;
    public bool jump;
    public bool sprint;
    public bool mantle;

    [Header("Movement Settings")]
    public bool analogMovement;

    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;

    [Header("Character Controller Reference")]
    //public ThirdPersonControllerWithMantle characterController; // ĳ���� ��Ʈ�ѷ� ����
    public PlayerMovement characterController;

    [Header("Mantle Settings")]
    private bool mantleAttempted = false;  // ��Ʋ ���� �õ��� �����ϱ� ���� �÷���

#if ENABLE_INPUT_SYSTEM
    public void OnMove(InputValue value)
    {
        MoveInput(value.Get<Vector2>());
    }

    public void OnLook(InputValue value)
    {
        if (cursorInputForLook)
        {
            LookInput(value.Get<Vector2>());
        }
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && !mantleAttempted)
        {
            mantleAttempted = true;

            // ������ �ԷµǾ��� �� ��Ʈ�ѷ����� Mantle ���� ���θ� üũ
            if (characterController.CanPerformMantle())
            {
                //Debug.Log("Mantle");
                MantleInput(true);
                characterController.StartMantle();
            }
            else
            {
                //Debug.Log("Jump");
                JumpInput(true);
            }
        }

        // ��ư�� ������ �� �÷��� �ʱ�ȭ
        if (!value.isPressed)
        {
            mantleAttempted = false;
            JumpInput(false);
        }
    }

    public void OnSprint(InputValue value)
    {
        SprintInput(value.isPressed);
    }
#endif

    public void MoveInput(Vector2 newMoveDirection)
    {
        move = newMoveDirection;
    }

    public void LookInput(Vector2 newLookDirection)
    {
        look = newLookDirection;
    }

    public void JumpInput(bool newJumpState)
    {
        jump = newJumpState;
    }

    public void MantleInput(bool newMantleState)
    {
        mantle = newMantleState;
    }

    public void SprintInput(bool newSprintState)
    {
        sprint = newSprintState;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
	