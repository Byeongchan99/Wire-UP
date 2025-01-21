using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNewInput : MonoBehaviour
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

    [Header("Player Controller Reference")]
    //public ThirdPersonControllerWithMantle characterController; // 캐릭터 컨트롤러 참조
    //public PlayerMovement characterController;
    public PlayerController playerController;

    [Header("Mantle Settings")]
    private bool mantleAttempted = false;  // 맨틀 동작 시도를 추적하기 위한 플래그

    [Header("UI Settings")]
    public bool pause;

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
            //Debug.Log("OnJump");
            mantleAttempted = true;

            // 점프가 입력되었을 때 컨트롤러에서 Mantle 가능 여부를 체크
            if (playerController.CanPerformMantle())
            {
                //Debug.Log("Mantle");
                MantleInput(true);
                playerController.StartMantle();
            }
            else
            {
                //Debug.Log("Jump");
                JumpInput(true);
            }
        }

        // 버튼을 놓았을 때 플래그 초기화
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

    public void OnPause(InputValue value)
    {
        PauseInput(value.isPressed);
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
        //Debug.Log("JumpInput");
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

    public void PauseInput(bool newPauseState)
    {
        if (GameManager.instance.isPaused) // 현재 일시정지 상태라면
        {
            GameManager.instance.ResumeGame(); // 게임 재개
        }
        else // 일시정지 상태가 아니라면
        {
            GameManager.instance.PauseGame(); // 게임 일시정지
        }
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
