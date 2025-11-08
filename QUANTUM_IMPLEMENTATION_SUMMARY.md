# Quantum 구현 요약 (Implementation Summary)

## 프로젝트 개요

Mirror 네트워킹에서 **Photon Quantum 3**로 마이그레이션한 1v1 턴제 배틀 게임입니다.

- **프레임워크**: Photon Quantum 3 (Deterministic ECS)
- **아키텍처**: Simulation 레이어(결정론적 로직) + View 레이어(Unity 렌더링)
- **게임 플로우**: 캐릭터 선택 (6턴) → 전투 순서 지정 → 전투 (Round 1~3: 1v1, Round 4: 3v3) → 결과

---

## 구현 완료 항목 ✅

### 1. DSL 정의 (Battle.qtn)

**위치**: `C:\Git\Battle\Assets\QuantumUser\Simulation\Battle.qtn`

**주요 구성요소**:

#### Input (플레이어 입력)
```qtn
input {
    Int32 SelectedChampionId;      // 선택한 챔피언 ID
    Int32 OrderSlot1;              // 전투 순서 1번 슬롯
    Int32 OrderSlot2;              // 전투 순서 2번 슬롯
    Int32 OrderSlot3;              // 전투 순서 3번 슬롯
    button OrderSubmit;            // 전투 순서 제출
}
```

#### Global (게임 전역 상태)
```qtn
global {
    Int32 CurrentPhase;            // 현재 게임 단계 (Lobby/CharSelect/OrderSetup/Battle/Result)
    Int32 SelectTurn;              // 캐릭터 선택 턴 (1~6)
    FP SelectTimer;                // 선택 타이머
    Int32 CurrentRound;            // 현재 라운드 (1~4)
    FP BattleTimer;                // 전투 타이머
    Int32 PlayerAScore;            // Player A 점수
    Int32 PlayerBScore;            // Player B 점수
}
```

#### Components (엔티티 컴포넌트)

**PlayerGameData**: 플레이어 게임 데이터
```qtn
component PlayerGameData {
    PlayerRef PlayerRef;           // 플레이어 참조
    list<Int32> SelectedChampions; // 선택한 챔피언 3개
    Int32 SelectedCount;           // 선택 완료 개수
    list<Int32> BattleOrder;       // 전투 순서 (슬롯 인덱스)
    Boolean OrderSubmitted;        // 순서 제출 완료 여부
    Int32 TeamId;                  // 팀 ID (0=A, 1=B)
}
```

**ChampionStats**: 챔피언 기본 스탯
```qtn
component ChampionStats {
    Int32 ChampionId;              // 챔피언 ID (0~11)
    FP Strength;                   // 힘
    FP Dexterity;                  // 민첩
    FP Constitution;               // 체력
    FP MaxHealth;                  // 최대 체력
    FP AttackPower;                // 공격력
    FP AttackSpeed;                // 공격 속도
    FP MoveSpeed;                  // 이동 속도
}
```

**BattleState**: 전투 중 상태
```qtn
component BattleState {
    FP Health;                     // 현재 체력
    Boolean IsAlive;               // 생존 여부
    Int32 TeamId;                  // 팀 ID
    Int32 SlotIndex;               // 슬롯 인덱스 (0~2)
    FP AttackCooldown;             // 공격 쿨다운
    EntityRef CurrentTarget;       // 현재 타겟
}
```

**SimpleAI**: AI 설정
```qtn
component SimpleAI {
    FP SearchRadius;               // 적 탐색 반경
    FP AttackRange;                // 공격 사거리
    FP ThinkTimer;                 // 사고 주기
}
```

#### Signals (이벤트)
```qtn
signal OnChampionSelected(PlayerRef player, Int32 championId);
signal OnOrderSubmitted(PlayerRef player);
signal OnBattleStart(Int32 round);
signal OnBattleEnd(Int32 winnerTeam);
signal OnChampionDeath(EntityRef champion);
signal OnPhaseChanged(Int32 newPhase);
```

---

### 2. 게임 시스템 (7개)

#### GamePhaseSystem.cs
**역할**: 게임 단계 관리 (Lobby → CharacterSelect → OrderSetup → Battle → Result)

**주요 기능**:
- 각 단계별 Update 로직 (타이머 관리, 자동 진행 등)
- `ChangePhase()`: 단계 전환 + OnPhaseChanged 시그널 발생

**파일**: `C:\Git\Battle\Assets\QuantumUser\Simulation\GamePhaseSystem.cs`

---

#### CharacterSelectSystem.cs
**역할**: 6턴 캐릭터 선택 로직 (A→B→B→A→A→B)

**주요 기능**:
- `SelectChampion()`: 챔피언 선택 검증 + SelectedChampions에 추가
- `NextTurn()`: 다음 턴 진행, 6턴 완료 시 OrderSetup으로 전환
- `GetCurrentTurnPlayer()`: 현재 턴 플레이어 결정
- `IsChampionSelected()`: 중복 선택 방지

**파일**: `C:\Git\Battle\Assets\QuantumUser\Simulation\CharacterSelectSystem.cs`

---

#### OrderSetupSystem.cs
**역할**: 전투 순서 지정 및 제출

**주요 기능**:
- `SubmitOrder()`: 1/2/3번 슬롯에 챔피언 배치
- `AllPlayersReady()`: 양 플레이어 제출 완료 확인
- 제출 완료 시 Battle 단계로 전환

**파일**: `C:\Git\Battle\Assets\QuantumUser\Simulation\OrderSetupSystem.cs`

---

#### BattleInputSystem.cs
**역할**: 플레이어 입력 처리

**주요 기능**:
- CharacterSelect 단계: `SelectedChampionId` 입력 → `CharacterSelectSystem.SelectChampion()` 호출
- OrderSetup 단계: `OrderSubmit` 버튼 → `OrderSetupSystem.SubmitOrder()` 호출

**파일**: `C:\Git\Battle\Assets\QuantumUser\Simulation\BattleInputSystem.cs`

---

#### BattleSystem.cs
**역할**: 전투 라운드 관리, 챔피언 스폰, 승패 판정

**주요 기능**:
- `StartRound()`: Round 1~3는 1v1 스폰, Round 4는 3v3 스폰
- `SpawnChampion()`: ChampionData에서 스탯 로드 → Entity 생성 → ChampionStats, BattleState, SimpleAI, Transform3D 설정
- `CheckRoundEnd()`: 한 팀 전멸 시 라운드 종료 → 다음 라운드 또는 Result 단계
- `EndBattle()`: 최종 승자 결정 (점수 기반)

**파일**: `C:\Git\Battle\Assets\QuantumUser\Simulation\BattleSystem.cs`

---

#### SimpleAISystem.cs
**역할**: AI 전투 로직

**주요 기능**:
- `FindNearestEnemy()`: 가장 가까운 적 탐색
- `AttackTarget()`: 사거리 내 타겟 공격 (쿨다운 적용)
- 타겟이 사거리 밖이면 이동 (NavMeshPathfinder 활용)

**파일**: `C:\Git\Battle\Assets\QuantumUser\Simulation\SimpleAISystem.cs`

---

#### PlayerManagementSystem.cs
**역할**: 플레이어 연결/해제 관리

**주요 기능**:
- `OnPlayerAdded()`: PlayerGameData 컴포넌트 생성, TeamId 할당 (0=A, 1=B)
- `OnPlayerRemoved()`: 플레이어 데이터 정리

**파일**: `C:\Git\Battle\Assets\QuantumUser\Simulation\PlayerManagementSystem.cs`

---

### 3. Asset 클래스 (Quantum 3 방식)

#### BattleGameConfig.cs
**역할**: 게임 전역 설정

**필드**:
```csharp
public AssetRef<EntityPrototype>[] ChampionPrototypes;  // 12개 챔피언 프로토타입
public FP SelectTimeLimit = FP._3;                      // 선택 제한 시간 (3초)
public FP BattleTimeLimit = FP.FromFloat_UNSAFE(60);    // 전투 제한 시간 (60초)
public FPVector3[] SpawnPositions;                      // 스폰 위치 (6개: A팀 3개, B팀 3개)
```

**헬퍼 메서드**:
- `GetChampionPrototype(int championId)`: ID로 프로토타입 가져오기
- `GetSpawnPosition(int teamId, int slotIndex)`: 팀/슬롯 기반 스폰 위치

**파일**: `C:\Git\Battle\Assets\QuantumUser\Simulation\BattleGameConfig.cs`

---

#### ChampionData.cs
**역할**: 개별 챔피언 데이터

**필드**:
```csharp
public FP Strength;                            // 힘 (공격력에 영향)
public FP Dexterity;                           // 민첩 (속도에 영향)
public FP Constitution;                        // 체력 (최대 HP에 영향)
public AssetRef<EntityPrototype> Prefab;       // 챔피언 Entity 프리팹
```

**계산 메서드**:
- `CalculateMaxHealth()`: Constitution * 10
- `CalculateAttackPower()`: Strength * 2
- `CalculateAttackSpeed()`: 1 + (Dexterity / 10)
- `CalculateMoveSpeed()`: 3 + (Dexterity / 20)

**파일**: `C:\Git\Battle\Assets\QuantumUser\Simulation\ChampionData.cs`

---

### 4. 헬퍼 및 설정 파일

#### Frame.User.cs
**역할**: Frame 확장 (Globals 접근 간편화)

```csharp
public _globals_* Globals => (_globals_*)_globals;
```

**파일**: `C:\Git\Battle\Assets\QuantumUser\Simulation\Frame.User.cs`

---

#### RuntimeConfig.User.cs
**역할**: 런타임 설정 확장

```csharp
public AssetRef<BattleGameConfig> GameConfig;  // BattleGameConfig 참조
```

**파일**: `C:\Git\Battle\Assets\QuantumUser\Simulation\RuntimeConfig.User.cs`

---

#### SystemSetup.User.cs
**역할**: 시스템 등록 순서 정의

```csharp
systems.Add(new PlayerManagementSystem());
systems.Add(new GamePhaseSystem());
systems.Add(new CharacterSelectSystem());
systems.Add(new OrderSetupSystem());
systems.Add(new BattleInputSystem());
systems.Add(new BattleSystem());
systems.Add(new SimpleAISystem());
```

**파일**: `C:\Git\Battle\Assets\QuantumUser\Simulation\SystemSetup.User.cs`

---

## 해결한 주요 이슈 🔧

### 1. Asset 지시문 폐기 (Quantum 3)
- **문제**: `asset ChampionData`, `asset BattleGameConfig` 사용 시 컴파일 에러
- **해결**: `AssetObject` 상속 클래스로 변환

### 2. Globals 타입 불일치
- **문제**: `Globals` 타입이 존재하지 않음 (Quantum은 `_globals_` 생성)
- **해결**: `Frame.User.cs`에 `public _globals_* Globals` 프로퍼티 추가

### 3. FP 상수 누락
- **문제**: `FP._60`, `FP._20` 등 존재하지 않음
- **해결**: `FP.FromFloat_UNSAFE(60)`, `FP.FromFloat_UNSAFE(20)` 사용

### 4. QListPtr 인덱싱 방식
- **문제**: `data->SelectedChampions[i]`, `.Get(i)`, `.Set(i, value)` 모두 작동 안 함
- **해결**: `f.ResolveList(data->SelectedChampions)[i]` 또는 `.Add()` 사용

### 5. PlayerGameData 컴포넌트 미추가
- **문제**: `f.Create()` 후 `GetPointer<PlayerGameData>()`만 호출 → 컴포넌트 없음
- **해결**: `f.Add<PlayerGameData>(entity)` 후 `GetPointer` 사용

### 6. Asteroids 예제 충돌
- **문제**: QuantumAsteroids 폴더의 코드가 Battle 코드와 충돌
- **해결**: 폴더를 Unity 프로젝트 외부로 이동 (`C:\Git\QuantumAsteroids.backup`)

---

## 아키텍처 다이어그램

```
┌─────────────────────────────────────────────────────────────┐
│                    Quantum Simulation Layer                 │
│                     (Deterministic Logic)                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────┐  ┌──────────────────┐               │
│  │ Battle.qtn (DSL) │→ │ CodeGen (Auto)   │               │
│  │ - Components     │  │ - Generated/     │               │
│  │ - Signals        │  │ - qtn.cs files   │               │
│  │ - Input/Global   │  └──────────────────┘               │
│  └──────────────────┘                                      │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Systems (7개)                                       │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │ 1. PlayerManagementSystem  (플레이어 연결/해제)     │   │
│  │ 2. GamePhaseSystem         (단계 관리)              │   │
│  │ 3. CharacterSelectSystem   (6턴 선택)               │   │
│  │ 4. OrderSetupSystem        (순서 지정)              │   │
│  │ 5. BattleInputSystem       (입력 처리)              │   │
│  │ 6. BattleSystem            (전투 관리, 스폰)        │   │
│  │ 7. SimpleAISystem          (AI 전투)                │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Assets (ScriptableObject)                           │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │ - BattleGameConfig (게임 설정, 12개 프로토타입)     │   │
│  │ - ChampionData (챔피언 스탯, 12개)                  │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
                              ↕
┌─────────────────────────────────────────────────────────────┐
│                      Unity View Layer                       │
│                   (Rendering & UI - TODO)                   │
├─────────────────────────────────────────────────────────────┤
│  - CharacterSelectUI: 12개 챔피언 그리드, 타이머            │
│  - BattleOrderUI: 드래그 앤 드롭 슬롯 (1/2/3번)             │
│  - BattleHUD: 라운드, 점수 표시                             │
│  - ResultPanel: 최종 승자                                   │
└─────────────────────────────────────────────────────────────┘
```

---

## 게임 플로우

```
Phase 0: Lobby
  ↓
  플레이어 2명 연결
  ↓
Phase 1: CharacterSelect (6턴)
  ↓
  Turn 1: Player A 선택
  Turn 2: Player B 선택
  Turn 3: Player B 선택
  Turn 4: Player A 선택
  Turn 5: Player A 선택
  Turn 6: Player B 선택
  ↓
Phase 2: OrderSetup
  ↓
  Player A: 1/2/3번 슬롯 배치
  Player B: 1/2/3번 슬롯 배치
  ↓
Phase 3: Battle
  ↓
  Round 1: 1번 슬롯 챔피언 1v1 (승리팀 +1점)
  Round 2: 2번 슬롯 챔피언 1v1 (승리팀 +1점)
  Round 3: 3번 슬롯 챔피언 1v1 (승리팀 +1점)
  Round 4: 전체 챔피언 3v3 (승리팀 +2점)
  ↓
Phase 4: Result
  ↓
  최종 점수 집계 (총 5점 만점)
  승자 표시
```

---

## 다음 단계 (TODO) 📋

### 1. Unity 에셋 생성
- [ ] BattleGameConfig 에셋 생성 (Project 창 우클릭 → Create → Quantum → BattleGameConfig)
  - ChampionPrototypes[12] 배열 설정
  - SpawnPositions[6] 배열 설정 (A팀: 0,1,2 / B팀: 3,4,5)

- [ ] ChampionData 에셋 12개 생성
  - 각 챔피언별 Strength/Dexterity/Constitution 설정
  - Prefab 참조 연결

### 2. Entity Prototype 생성
- [ ] 챔피언 프로토타입 12개 생성
  - EntityPrototype 에셋 생성
  - Transform3D, NavMeshPathfinder, NavMeshSteeringAgent, NavMeshAvoidanceAgent 컴포넌트 추가
  - ChampionStats, BattleState, SimpleAI는 런타임에 BattleSystem이 추가

### 3. RuntimeConfig 설정
- [ ] RuntimeConfig 에셋에서 GameConfig 필드에 BattleGameConfig 연결

### 4. 테스트 씬 구성
- [ ] Quantum 맵 생성 (NavMesh 포함)
- [ ] 스폰 포인트 6개 배치 (FPVector3로 변환하여 BattleGameConfig에 저장)

### 5. View 레이어 구현
- [ ] Input Polling: Unity Input → Quantum Input 변환
- [ ] UI 업데이트: Signal 리스너로 UI 갱신
- [ ] 챔피언 렌더링: EntityView로 Entity-GameObject 연결

### 6. 테스트
- [ ] 로컬 2인 플레이 테스트 (ParrelSync 또는 빌드)
- [ ] 선택 → 순서 → 전투 → 결과 전체 플로우 검증
- [ ] AI 전투 동작 확인

---

## 기술 스택 요약

| 구분 | 기술 |
|------|------|
| 게임 엔진 | Unity 6000.2.10f1 |
| 네트워킹 | Photon Quantum 3 |
| 아키텍처 | Deterministic ECS |
| 물리 | Quantum Physics (FPVector3, FPQuaternion) |
| AI | NavMesh (Quantum NavMeshPathfinder) |
| 상태 관리 | Global State + Component System |
| 입력 | Quantum Input Struct |
| 이벤트 | Quantum Signals |

---

## 개발자 참고사항

### Quantum 3 주요 API 패턴

**전역 상태 접근**:
```csharp
var globals = f.Globals;  // Frame.User.cs의 헬퍼 프로퍼티
globals->CurrentPhase = (int)Phase.Battle;
```

**컴포넌트 추가 및 접근**:
```csharp
var entity = f.Create();
f.Add<PlayerGameData>(entity);
var data = f.Unsafe.GetPointer<PlayerGameData>(entity);
data->PlayerRef = player;
```

**QListPtr 사용**:
```csharp
// 추가
f.ResolveList(data->SelectedChampions).Add(championId);

// 읽기
int value = f.ResolveList(data->SelectedChampions)[i];

// 쓰기
f.ResolveList(data->BattleOrder)[0] = slot1;

// 개수
int count = f.ResolveList(data->SelectedChampions).Count;

// 초기화
f.ResolveList(data->BattleOrder).Clear();
```

**Asset 로드**:
```csharp
var config = f.FindAsset<BattleGameConfig>(f.RuntimeConfig.GameConfig.Id);
var championData = f.FindAsset<ChampionData>(assetRef.Id);
```

**Entity 스폰**:
```csharp
var entity = f.Create(prototypeRef);
if (f.Unsafe.TryGetPointer<Transform3D>(entity, out var transform))
{
    transform->Position = spawnPos;
}
```

**Signal 발생**:
```csharp
f.Signals.OnChampionSelected(player, championId);
f.Signals.OnPhaseChanged((int)newPhase);
```

---

## 컴파일 상태 ✅

**현재 상태**: **컴파일 성공** (모든 에러 해결 완료)

**확인된 파일**:
- ✅ Battle.qtn (CodeGen 완료)
- ✅ GamePhaseSystem.cs
- ✅ CharacterSelectSystem.cs
- ✅ OrderSetupSystem.cs
- ✅ BattleInputSystem.cs
- ✅ BattleSystem.cs
- ✅ SimpleAISystem.cs
- ✅ PlayerManagementSystem.cs
- ✅ BattleGameConfig.cs
- ✅ ChampionData.cs
- ✅ Frame.User.cs
- ✅ RuntimeConfig.User.cs
- ✅ SystemSetup.User.cs

**삭제된 충돌 에셋**:
- ❌ QuantumAsteroids (프로젝트 외부로 이동)

---

## 문의 및 참고

- **Quantum 공식 문서**: https://doc.photonengine.com/quantum
- **Quantum SDK 버전**: Quantum 3 (최신)
- **프로젝트 루트**: `C:\Git\Battle`
- **Simulation 코드 위치**: `C:\Git\Battle\Assets\QuantumUser\Simulation\`

---

**작성일**: 2025-11-08
**작성자**: Claude (AI Assistant)
**프로젝트**: Battle Game (Photon Quantum 3)
