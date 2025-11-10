# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 기반 멀티플레이어 1v1 턴제 배틀 게임입니다. TopDown Engine과 Mirror 네트워킹을 사용하여 리그 오브 레전드 스타일의 챔피언 선택과 AI 자동 전투를 구현합니다.

- **Unity 버전**: 6000.2.10f1 (Unity 6)
- **렌더 파이프라인**: URP (Universal Render Pipeline) 17.2.0
- **네트워킹**: Mirror Networking
- **게임 엔진**: TopDown Engine (MoreMountains)
- **언어**: C#

## 핵심 아키텍처

### 게임 플로우
```
캐릭터 선택 (6턴, A→B 교대)
  → 전투 순서 지정 (각자 1,2,3번 슬롯 배치)
  → 전투 (Round 1~3: 1v1, Final: 3v3)
  → 결과 표시
  → 반복
```

### 주요 컴포넌트

**BattleGameManager** (`Assets/Scripts/Battle/BattleGameManager.cs`)
- 게임 전체 상태 관리 (GameState enum: Lobby → CharacterSelect → OrderSetup → Battle → Result)
- 캐릭터 선택 로직 (6턴, 3초 타이머, 자동 선택)
- NetworkBehaviour 기반, SyncVar로 상태 동기화
- 싱글톤 패턴 사용 (`BattleGameManager.Instance`)
- **1인 테스트 모드**: `soloTestMode = true`로 Host가 A, B 플레이어 모두 제어

**BattleArenaManager** (`Assets/Scripts/Battle/BattleArenaManager.cs`)
- 전투 실행 및 라운드 관리
- 캐릭터 스폰 (playerASpawnPoint, playerBSpawnPoint)
- AI 전투 또는 턴제 전투 지원
- 승패 판정 및 점수 집계

**MatchNetworkManager** (`Assets/Scripts/MatchNetworkManager.cs`)
- Mirror의 NetworkManager 확장
- CanvasController와 연계하여 매치 생성/참가 관리
- 서버/클라이언트 생명주기 관리

**ChampionDatabase** (`Assets/Scripts/Battle/Data/ChampionDatabase.cs`)
- ScriptableObject로 12개 챔피언 데이터 저장
- 챔피언별 스탯 (strength, dexterity, constitution)
- 프리팹, 아이콘, 이미지 참조

**PlayerGameData** (`Assets/Scripts/Battle/Data/PlayerGameData.cs`)
- 선택한 챔피언 3개 저장 (selectedChampions)
- 전투 순서 배열 (battleOrder[3])
- 점수 추적 (totalScore)

### 네트워킹 구조

- **Mirror Multiple Matches 패턴**: 한 서버에서 여러 1v1 매치 동시 실행
- **NetworkMatch**: matchId 기반 매치 격리
- **SyncVar**: 서버-클라이언트 상태 동기화
- **ClientRpc**: 서버→클라이언트 UI 업데이트
- **Command**: 클라이언트→서버 요청 (예: CmdSelectChampion)

### UI 구조

**LTF_MirrorMultipleMatches 씬**의 MatchControllerEx:
- **CharacterSelectUI**: 12개 챔피언 그리드, 턴 표시, 타이머, 양측 선택 슬롯
- **BattleOrderUI**: 드래그 앤 드롭으로 전투 순서 지정 (1,2,3번 슬롯)
- **BattleHUD**: 라운드 정보, 점수 표시
- **ResultPanel**: 최종 승자 및 점수 표시

## 개발 환경 설정

### Unity 에디터에서 실행
1. `Assets/Scenes/Mirror/LTF_MirrorMultipleMatches.unity` 씬 열기
2. BattleGameManager 오브젝트 선택
3. Inspector에서 `Solo Test Mode` 체크 확인
4. Play 버튼 클릭
5. 챔피언 카드 클릭 또는 3초 대기(자동 선택)

### 빠른 테스트
- **F1 키**: 모든 챔피언 자동 선택 완료 (디버그 모드)
- **Context Menu**: BattleGameManager 우클릭 → "Debug: Auto Select All"

### NavMesh 설정 (전투 시스템 개발 시)
```
Window > AI > Navigation
바닥 오브젝트에 NavMeshSurface 추가 후 Bake
```

### 팀 레이어 설정 (AI 전투 시)
- Layer 24: TeamA
- Layer 25: TeamB
- AIBrain의 타겟 레이어 마스크를 상대 팀으로 설정

## 주요 씬

- **LTF_MirrorMultipleMatches.unity**: 메인 게임 씬 (캐릭터 선택 + 전투)
- **SELECT_CHAMPION.unity**: 챔피언 선택 화면 (레거시)
- **RoomOnline.unity / RoomOffline.unity**: Mirror 룸 시스템 테스트

## 코딩 규칙

### Mirror 네트워킹
- 모든 네트워크 오브젝트에 `NetworkIdentity` 필수
- 서버 로직: `[Server]` 또는 `isServer` 체크
- 클라이언트 RPC: `[ClientRpc]` 메서드명은 `Rpc` 접두사
- Command: `[Command]` 메서드명은 `Cmd` 접두사
- 상태 동기화: `[SyncVar]` 사용

### 상태 관리
- `BattleGameManager.Instance.currentState`로 현재 게임 상태 확인
- 상태 변경: `ChangeState(GameState)` 메서드 사용 (서버에서만)
- 상태 전환 시 `RpcOnStateChanged`로 클라이언트 UI 업데이트

### 디버그 로그
- 이모지 사용: 🎮 (게임), ⚔️ (전투), ✅ (성공), ❌ (에러), 🔴 (Player A), 🔵 (Player B)
- 중요 이벤트: `Debug.Log($"✅ [CharacterSelect] Champion selected: {championName}")`

### ScriptableObject 사용
- ChampionDatabase는 싱글톤 에셋 (Project 창에서 생성)
- `[CreateAssetMenu]` 속성으로 생성 가능하게 설정

## 자주 사용하는 명령어

### Unity 빌드
```bash
# Unity 에디터에서: File > Build Settings > Build
# 또는 커맨드라인:
# Unity.exe -quit -batchmode -projectPath "C:\Git\Battle" -executeMethod BuildScript.Build
```

### 테스트
- Unity Test Framework 사용 (com.unity.test-framework 1.6.0)
- Play Mode 테스트: Window > General > Test Runner

### Git
- 대용량 에셋은 `.gitignore`에 포함 (이미 설정됨)
- Mirror, TopDownEngine은 에셋 스토어에서 별도 설치

## 문제 해결

### AI가 움직이지 않을 때
- NavMesh Bake 확인 (`Window > AI > Navigation`)
- AI Navigation 패키지 설치 확인 (com.unity.ai.navigation 2.0.9)

### AI가 적을 인식하지 못할 때
- 레이어 설정 확인 (TeamA: Layer 24, TeamB: Layer 25)
- AIBrain 컴포넌트의 타겟 레이어 마스크 확인
- DamageOnTouch의 타겟 레이어 마스크 확인

### 네트워크 동기화 문제
- NetworkIdentity 컴포넌트 확인
- SyncVar 필드가 올바르게 선언되었는지 확인
- Command/ClientRpc 메서드 시그니처 확인

### 캐릭터가 보이지 않을 때
- Renderer 컴포넌트가 활성화되어 있는지 확인
- 스폰 위치가 카메라 시야 내에 있는지 확인
- `BattleArenaManager.ActivateAllRenderers` 호출 확인

## 개발 문서

프로젝트 루트의 마크다운 문서들:
- **QUICK_START.md**: 10분 안에 개발 환경 구축
- **실전_개발_가이드_12개챔피언.md**: Phase 1 개발 가이드 (최우선)
- **개발_요구사항_및_구현_가이드.md**: 전체 시스템 설계 (20개 챔피언 목표)
- **LTF_MirrorMultipleMatches_구조분석.md**: Mirror 매칭 시스템 상세
- **MatchControllerEx_UI분석.md**: 기존 UI 구조 분석
- **AI_Battle_Setup_Guide.md**: AI 전투 설정 방법
- **PROJECT_OVERVIEW.md**: 프로젝트 전체 개요

## 현재 개발 상태

✅ **완료**:
- 캐릭터 선택 시스템 (6턴, 타이머, 자동 선택)
- 전투 순서 지정 UI (드래그 앤 드롭)
- 전투 아레나 (라운드 관리, 승패 판정)
- 1인 테스트 모드
- **Photon Quantum 3 전투 시스템 (2025-11-11)**
  - SimpleAI 시스템 (타겟 찾기, 이동, 공격)
  - 자동 전투 AI (적 탐색, 이동, 공격 반복)
  - 공격 쿨다운 시스템
  - 체력/데미지 시스템

🔄 **진행 중**:
- Quantum View 레이어 최적화 (움직임 보간)

⏳ **대기**:
- 3v3 단체전 로직
- 결과 화면 UI
- 12→20개 챔피언 확장
- 멀티플레이어 2인 테스트

## 중요 참고사항

- **1인 테스트 모드 우선**: 멀티플레이어는 나중에, 지금은 혼자서 빠르게 개발
- **기존 UI 재사용**: MatchControllerEx의 UI 구조를 최대한 활용
- **Quantum 전투 시스템**: NavMesh 대신 직접 Transform 조작으로 이동 구현
- **레이어 설정 중요**: TeamA/TeamB 레이어 없으면 AI가 적 인식 못함 (Mirror 시스템)
- **Mirror 동기화 주의**: NetworkBehaviour 상속, [SyncVar]/[Command]/[ClientRpc] 올바르게 사용

## 최근 디버깅 히스토리

### 2025-11-11: Photon Quantum 3 전투 시스템 구현

#### 개요
Photon Quantum 3의 결정론적 ECS 아키텍처를 사용하여 자동 전투 AI 시스템 구현 완료.

#### 구현 내용

**1. 캐릭터 선택 타이머 조정**
- 빠른 테스트를 위해 선택 타이머를 3초 → 1초 → 0.3초로 단축
- 파일: `CharacterSelectSystem.cs`, `GamePhaseSystem.cs`

**2. AI 이동 시스템**
- **문제**: NavMesh가 맵에 설정되지 않아 이동 불가
- **해결**: NavMesh 대신 Transform3D를 직접 조작하는 방식으로 변경
- 이동 속도: 최종 10 (FP._10) 설정
- 코드:
```csharp
FPVector3 direction = (targetTransform.Position - filter.Transform->Position).Normalized;
FP moveSpeed = FP._10;
FPVector3 newPosition = filter.Transform->Position + direction * moveSpeed * f.DeltaTime;
filter.Transform->Position = newPosition;
```

**3. 타겟 탐색 시스템**
- SearchRadius 내에서 가장 가까운 적 찾기
- 같은 팀, 죽은 적 필터링
- **버그 수정**: 거리 비교 조건을 `<`에서 `<=`로 변경하여 정확히 SearchRadius 거리의 적도 감지

**4. 공격 시스템**
- AttackRange(2 units) 내에 들어오면 공격 시작
- AttackCooldown 시스템: 1초 쿨다운 (1 / AttackSpeed)
- 데미지 적용 및 체력 감소
- 체력 0 이하 시 IsAlive = false 설정 및 OnChampionDeath 시그널 발생

**5. 타이밍 이슈 해결**
- **핵심 문제**: ThinkTimer(0.5초)와 AttackCooldown(1초) 충돌
  - ThinkTimer 만료 시에만 ProcessCombat 실행
  - 첫 공격 후 0.5초 후 ThinkTimer 만료 → 아직 쿨다운 0.5초 남음 → 대기
  - 다음 0.5초 후 쿨다운 만료되지만 ThinkTimer가 실행되지 않음 → 공격 안됨
- **해결**: ThinkTimer는 타겟 찾기에만 사용, ProcessCombat는 매 프레임 실행
```csharp
// ThinkTimer는 타겟 찾기에만 사용 (0.5초마다)
if (filter.AI->ThinkTimer <= FP._0)
{
    filter.AI->ThinkTimer = FP._0_50;
    if (타겟 없음 || 타겟 죽음)
    {
        FindNewTarget(f, ref filter);
    }
}

// 전투 처리는 매 프레임 실행 (AttackCooldown이 자체적으로 관리됨)
if (filter.BattleState->CurrentTarget != EntityRef.None)
{
    ProcessCombat(f, ref filter);
}
```

**6. 디버그 로그 정리**
- 매 프레임마다 출력되는 불필요한 로그 주석 처리
- 남은 중요 로그:
  - 🎯 타겟 찾기 성공
  - ⚔️ 공격 및 데미지
  - 💀 챔피언 사망

#### 주요 파일 수정

**SimpleAISystem.cs** (`Assets/QuantumUser/Simulation/SimpleAISystem.cs`)
- Update(): ThinkTimer와 ProcessCombat 분리
- FindNewTarget(): 거리 비교 조건 수정 (`<=`)
- ProcessCombat(): 매 프레임 실행, 이동/공격 로직
- AttackTarget(): 데미지 적용 및 사망 처리

**CharacterSelectSystem.cs, GamePhaseSystem.cs**
- SelectTimer를 0.3초로 설정

**BattleSystem.cs**
- NavMeshSteeringAgent의 MaxSpeed 동적 설정 추가

#### 기술적 상세

**Photon Quantum 3 특성**:
- 결정론적 시뮬레이션: Fixed Point(FP) 연산 사용
- ECS 아키텍처: Entity + Component + System
- SystemMainThreadFilter: 특정 컴포넌트 조합을 가진 엔티티만 필터링
- 60 tick/sec 고정 프레임레이트 (f.DeltaTime ≈ 0.0166초)

**컴포넌트 구조**:
```
Filter {
    EntityRef Entity;
    SimpleAI* AI;              // SearchRadius, AttackRange, ThinkTimer
    BattleState* BattleState;  // Health, IsAlive, TeamId, AttackCooldown, CurrentTarget
    Transform3D* Transform;    // Position
    NavMeshPathfinder* Pathfinder;  // (사용 안 함, Filter 요구사항)
}
```

**성능 이슈**:
- 직접 Transform 조작으로 인한 떨림(jittering) 현상
- View 레이어가 Simulation의 위치 변경을 부드럽게 보간하지 못함
- 향후 EntityViewUpdater 설정 필요

#### 테스트 결과
- ✅ 챔피언 스폰 성공
- ✅ 타겟 탐색 성공
- ✅ 이동 시스템 작동
- ✅ 공격 반복 실행
- ✅ 쿨다운 시스템 정상 작동
- ✅ 사망 처리 정상 작동
- ⚠️ 움직임 떨림 현상 (View 레이어 이슈)

#### 다음 단계
1. View 레이어 보간 설정으로 움직임 부드럽게 개선
2. 라운드 종료 조건 구현 (한 팀 전멸 시)
3. 다음 라운드 자동 시작
4. 3v3 전투 테스트

## 이전 디버깅 히스토리

### 2025-11-07: 전투 시스템 네트워크 동기화 이슈 해결

#### 문제 1: TeamA 레이어 캐릭터가 적을 탐색하지 못하는 문제
**증상**: Host에서 전투 시 TeamB 캐릭터는 적을 찾아가지만, TeamA 캐릭터는 탐색하지 못함

**원인**:
- 양쪽 캐릭터가 모두 Layer 25로 스폰됨
- AIBrain의 TargetLayerMask가 Layer 24만 설정되어 있음
- Layer 25는 Layer 24를 찾을 수 있지만, Layer 24로 설정되지 않은 캐릭터는 아무도 못 찾음

**해결**: `BattleArenaManager.cs` 수정
```csharp
// SpawnCharacters() 메서드에서
SetLayerRecursively(playerAChar, 24);  // Player A → TeamA (Layer 24)
SetLayerRecursively(playerBChar, 25);  // Player B → TeamB (Layer 25)

// AIBrain의 TargetLayerMask를 동적으로 변경
SetAIBrainTargetLayer(playerAChar, 25);  // A는 B(Layer 25)를 타겟
SetAIBrainTargetLayer(playerBChar, 24);  // B는 A(Layer 24)를 타겟
```

**관련 파일**:
- `Assets/Scripts/Battle/BattleArenaManager.cs` (SetLayerRecursively, SetAIBrainTargetLayer 메서드 추가)

---

#### 문제 2: 클라이언트에서 스폰된 캐릭터가 보이지 않는 문제
**증상**: Host에서는 캐릭터 2개가 잘 보이고 전투하지만, Client에서는 아무것도 보이지 않음

**원인**:
- Mirror RPC에서 GameObject를 직접 파라미터로 전달하면 네트워크를 통해 올바르게 전달되지 않음
- `RpcActivateRenderers(GameObject)` 형태로 호출 시 클라이언트에서 null 받음

**해결**: GameObject 대신 netId 사용
```csharp
// Before (잘못된 방법)
RpcActivateRenderers(playerAChar);

// After (올바른 방법)
RpcActivateRenderers(playerAChar.GetComponent<NetworkIdentity>().netId);

[ClientRpc]
void RpcActivateRenderers(uint netId)
{
    if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity))
    {
        GameObject character = identity.gameObject;
        // Renderer 활성화 로직
    }
}
```

**관련 파일**:
- `Assets/Scripts/Battle/BattleArenaManager.cs` (RpcActivateRenderers, RpcSetLayerRecursively 수정)

---

#### 문제 3: 클라이언트에서 BattleArenaManager.StartBattle()가 호출되지 않음
**증상**: 클라이언트 로그에 "⚔️ 전투 시작!" 메시지는 나오지만 캐릭터 스폰 로직이 실행되지 않음

**원인**:
- `MatchController.RpcStartBattle()`이 `[ClientRpc]`인데 내부에 `if (isServer)` 체크가 있음
- ClientRpc는 모든 클라이언트에서 실행되지만, isServer 체크로 인해 Host만 실행됨
- 순수 Client는 BattleArenaManager.StartBattle()를 호출하지 못함

**해결**: Server 로직과 Client 로직 분리
```csharp
// MatchController.cs

[Server]
void ServerStartBattle()
{
    // 서버에서만 실행: 캐릭터 스폰, 게임 로직
    if (BattleArenaManager.Instance != null)
    {
        BattleArenaManager.Instance.StartBattle(this);
    }
}

[ClientRpc]
void RpcStartBattle()
{
    // 모든 클라이언트에서 실행: UI 업데이트만
    Debug.Log("⚔️ 전투 시작! (Client)");
    if (BattleOrderUI.Instance != null)
    {
        BattleOrderUI.Instance.HideOrderSetupUI();
    }
}

// 전투 순서 제출 시
if (player1OrderSubmitted && player2OrderSubmitted)
{
    ServerStartBattle();  // 서버: 캐릭터 스폰
    RpcStartBattle();     // 모든 클라이언트: UI 업데이트
}
```

**관련 파일**:
- `Assets/Scripts/MatchController.cs` (ServerStartBattle, RpcStartBattle 분리)

---

#### 문제 4: 클라이언트에서 BattleArenaManager가 초기화되지 않음 (진행 중)
**증상**:
- 클라이언트 로그에 `BattleArenaManager.Awake()` 실행됨
- 하지만 `OnEnable()` → `OnDisable()` 사이클 발생
- `Start()`, `OnStartClient()` 호출되지 않음
- `isServer=False, isClient=False` 상태 유지 (Mirror 초기화 안됨)

**현재 상태**: 디버깅 중
- `OnDisable()`에 스택 트레이스 추가하여 무엇이 GameObject를 비활성화하는지 추적 중
- 가능성 1: MatchController의 `Panels.SetActive(false)` 호출 시 BattleArenaManager 포함 가능성
- 가능성 2: 씬 로딩/언로딩 과정에서 일시적 비활성화
- 가능성 3: 부모 GameObject가 비활성화되어 자식도 비활성화

**다음 단계**:
1. 클라이언트에서 테스트 실행
2. 콘솔의 `🔴 BattleArenaManager.OnDisable()` 로그와 스택 트레이스 확인
3. 스택 트레이스에서 호출 경로 분석
4. 해당 코드 수정

**임시 코드** (디버깅용):
```csharp
// BattleArenaManager.cs
void OnDisable()
{
    Debug.Log($"🔴 BattleArenaManager.OnDisable() (isServer={isServer}, isClient={isClient})");
    Debug.Log($"   호출 스택:\n{System.Environment.StackTrace}");
}
```

**관련 파일**:
- `Assets/Scripts/Battle/BattleArenaManager.cs` (디버그 로그 추가)
- `Assets/Scripts/MatchController.cs` (Panels 관련 조사 필요)

---

### 디버깅 팁

**Mirror NetworkBehaviour 디버깅**:
- `isServer`, `isClient`, `isLocalPlayer` 상태 확인
- NetworkIdentity의 `netId`, `sceneId` 확인
- `OnStartServer()`, `OnStartClient()` 호출 여부 확인

**RPC 디버깅**:
- GameObject 파라미터는 절대 사용하지 말 것 → netId 사용
- [ClientRpc]는 모든 클라이언트에서 실행됨 (Host 포함)
- [Server]는 서버에서만 실행 (Host의 서버 부분)

**ParrelSync 사용 권장**:
- 빌드 없이 에디터에서 Host와 Client 동시 테스트 가능
- Window → ParrelSync → Create Clone
- 클론에서 "Open in New Editor" 클릭
