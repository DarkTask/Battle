# TopDown Engine 1v1 AI 대전 설정 최종 가이드

이 문서는 TopDown Engine에서 두 AI 캐릭터가 서로를 적으로 인식하고 싸우게 만드는 과정의 최종 해결 방법을 정리한 가이드입니다.

## 1. 목표

`PatrolSeekAndSwordAI` 프리팹을 기반으로 한 두 AI(AI 'A', AI 'B')가 서로 다른 팀(`TeamA`, `TeamB`)으로 나뉘어 1:1 전투를 벌이게 한다.

## 2. 핵심 해결 방법

AI가 상대를 '인식'하는 단계와 '공격'하는 단계의 조건이 `AIBrain` 내에 별도로 존재하므로, **두 단계의 타겟 레이어를 각각 설정**해주는 것이 핵심입니다.

### 단계 1: 팀 구분을 위한 레이어 설정

1.  `Edit > Project Settings > Tags and Layers`에서 `TeamA`와 `TeamB`라는 두 개의 새로운 사용자 레이어를 생성합니다.
2.  AI 'A' 캐릭터와 그 모든 자식 오브젝트들의 레이어를 `TeamA`로 설정합니다.
3.  AI 'B' 캐릭터와 그 모든 자식 오브젝트들의 레이어를 `TeamB`로 설정합니다.

### 단계 2: AI 두뇌 설정 (AIBrain Configuration)

각 AI의 `AIBrain` 컴포넌트에서 아래 두 가지 상태 전환(Transition) 설정을 모두 수정해야 합니다.

#### 1. '추적'을 위한 설정 (Patrolling -> Seeking)

-   **위치**: `AIBrain` > `States` > `Patrolling` > `Transitions` 목록 > `Seeking`으로 가는 Transition
-   **조치**: 해당 Transition 안의 `AIDecisionDetectTargetRadius3D` 컴포넌트를 찾아서, `Target Layer Mask` 속성을 **상대 팀의 레이어**로 설정합니다. (예: AI 'A'는 `TeamB`를 선택)
-   **역할**: 넓은 반경으로 적을 최초로 '발견'하고 '추적' 상태로 넘어가는 역할을 합니다.

#### 2. '공격'을 위한 설정 (Seeking -> Destroying)

-   **위치**: `AIBrain` > `States` > `Seeking` > `Transitions` 목록 > `Destroying`으로 가는 Transition (이름이 `AttackRadius`일 확률이 높음)
-   **조치**: 해당 Transition 안의 `AIDecisionDetectTargetRadius3D` 컴포넌트를 찾아서, `Target Layer Mask` 속성을 **상대 팀의 레이어**로 동일하게 설정합니다.
-   **역할**: 좁은 공격 사정거리 안으로 적이 들어왔는지 '판단'하고 '공격' 상태로 넘어가는 역할을 합니다.

---

## 3. 추가 문제 해결 과정 요약

위 방법으로 해결되지 않을 경우, 아래 문제들도 확인되었습니다.

-   **AI가 움직이지 않을 때**: `AIActionPathfinderToTarget3D` 액션은 **NavMesh**를 요구합니다. `Window > AI > Navigation`에서 NavMesh를 Bake 해야 합니다.
    -   최신 유니티 버전에서는 `Window > Package Manager`에서 **AI Navigation** 패키지를 설치해야 Bake 탭이 보입니다.
    -   바닥 오브젝트에 `NavMeshSurface` 컴포넌트를 추가하고 Bake 해야 합니다.

-   **AI가 공격은 하지만 피해를 주지 못할 때**: AI의 무기(예: 칼) 오브젝트에 붙어있는 **`DamageOnTouch`** 컴포넌트의 `Target Layer Mask`도 상대 팀 레이어로 설정해야 합니다.