# 프로젝트 소개

개인 프로젝트로 제작한 Unity 기반 3D 플랫포머 게임입니다.

Wire Up은 인기 게임 "온리 업!"에서 영감을 받아 제작되었습니다.

플레이어는 높은 곳으로 계속해서 올라가야 하는 목표를 가지고 있으며, 이번 게임에서는 특별히 와이어를 이용한 스윙 액션이 추가되었습니다. 

이 스윙 액션을 통해 플레이어는 특정 지점에 와이어를 발사하고 이를 이용해 빠르게 이동하거나 장애물을 극복할 수 있습니다.

Window로 빌드되어 아래 사이트에서 다운받아 직접 플레이해보실 수 있습니다.

</br>

# 세부 사항

### 기간

2024.11.03 ~ 2025.02.02

### 주요 업무

게임의 기획, UI/UX 디자인 및 프로그래밍을 포함한 모든 개발 과정을 직접 담당하였습니다.

### itch.io 사이트

[와이어 업](https://harrrypoter.itch.io/wire-up)

### 플레이 영상

[와이어 업 플레이 영상](https://youtu.be/SaEnooSEL6w?si=wKoJR3-56rKfwwWn)

</br>

# 게임 구조

![Image](https://github.com/user-attachments/assets/31a4e610-871a-4dbd-a6dd-7463f7647534)

게임의 전체적인 구조입니다.

3개의 일반 스테이지와 엔딩 이벤트를 볼 수 있는 엔딩 스테이지가 있습니다.

플레이어는 파쿠르 동작과 스윙 액션을 활용하여 최대한 높이, 그리고 빠르게 올라가야 합니다.

### 초기 기획 및 설계

[와이어 업 기획 및 설계](https://www.notion.so/1334e320564d80debea0c690418888fc?pvs=4)

</br>

# 주요 기능

1. **파쿠르 동작**

<br/>

![Image](https://github.com/user-attachments/assets/4d646be9-4b88-4ba5-b879-0febe3f50a00)

<br/>

![Image](https://github.com/user-attachments/assets/ff4fe55e-3524-4ae6-a3da-af1072e9cdac)

<br/>

[와이어 업 개발 일지 1 - 기본 동작 구현(파쿠르, 경사로 이동)](https://www.notion.so/1-1574e320564d803f8de3eeef35d9d668?pvs=4) 

<br/>

2. **스윙 액션**

<br/>

![Image](https://github.com/user-attachments/assets/9adcdee7-5631-4fd0-ad89-b3e8aac51a99)

<br/>

![Image](https://github.com/user-attachments/assets/b590d08c-1a3a-4a42-90b5-70b7a3e06d61)

<br/>

[와이어 업 개발 일지 2 - 스윙 액션](https://www.notion.so/2-15e4e320564d80bfbda0ec141dafe51c?pvs=4) 

<br/>

# 프로젝트 경험

[와이어 업 개발 개선점 및 아쉬웠던 점](https://www.notion.so/1864e320564d80958debe83da4937d6f?pvs=21) 

<br/>

- 물리 기반 캐릭터 컨트롤러 구현하기
    - Character Controller, Rigidbody 다뤄보기
    - 경사로 움직임 구현하기
    - 마찰력, 중력, 속력, 질량 다루기
    - Spring Joint로 스윙 액션 구현하기
    - Raycast로 파쿠르 동작 구현하기

<br/>

- 최적화 기법 다뤄보기
    - 중요하지 않은 게임 오브젝트들 삭제
    - 복잡한 Mesh Collider를 단순한 형태의 Box Collider로 변경
    - 가만히 있는 게임 오브젝트들 Static으로 변경
    - Occlusion Culling 적용
    - Baked Lighting 적용
    - LOD(Level of Detail) 적용 - 멀어지면 Culled되어 렌더링되지 않음

<br/>
