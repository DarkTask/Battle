# LTF_MirrorMultipleMatches 씬 구조 분석

## 개요

`LTF_MirrorMultipleMatches.unity` 씬은 Mirror 네트워킹의 **MultipleMatches 예제**를 기반으로 구현된 멀티플레이어 1v1 매칭 시스템입니다. 이 씬은 여러 플레이어가 동시에 1:1 매치를 진행할 수 있는 구조로 설계되었습니다.

## 핵심 개념: Multiple Matches

> "하나의 게임 서버 인스턴스에서 여러 개의 독립적인 게임 매치를 동시에 실행"

- 카드 게임, 보드 게임, 퍼즐 게임, 아케이드 게임에 적합
- 물리 연산이 없는 게임에 최적화
- 각 매치는 **matchId**로 구분되며, 같은 matchId를 가진 플레이어들만 서로의 데이터를 주고받음
- 다른 매치의 데이터는 전혀 수신하지 않음

## 씬 구조

### 주요 게임 오브젝트

```
Scene Hierarchy
├── Managers --------------------------------------------------------
│   ├── NetworkManager              # 네트워크 매니저 (MatchNetworkManager)
│   ├── GameManager                 # TopDown Engine 게임 매니저 (비활성)
│   ├── LevelManager                # TopDown Engine 레벨 매니저
│   └── ChampionImageManager        # 챔피언 이미지 관리자
│
├── Level
│   └── Ground                      # 게임 맵 바닥
│
├── Main Camera                     # 메인 카메라
├── DirectionalLight                # 조명
│
├── Canvas (UI)
│   ├── LobbyView                   # 로비 UI
│   │   ├── JoinButton              # 매치 참가 버튼
│   │   ├── CreateButton            # 매치 생성 버튼
│   │   └── Match List              # 매치 목록
│   │
│   └── MatchGUI                    # 인게임 UI
│       ├── CharacterSelection      # 챔피언 선택 패널
│       ├── GameText                # 게임 상태 텍스트
│       ├── WinCounter              # 승리 카운터
│       └── Buttons (Exit, PlayAgain)
│
└── Players (런타임에 생성)
    ├── PatrolSeekAndSwordAI A Player (TeamA, Layer 24)
    └── PatrolSeekAndSwordAI B Player (TeamB, Layer 25)
```

## 핵심 컴포넌트 분석

### 1. NetworkManager (MatchNetworkManager)

**위치**: Mirror 예제 기반 (`Mirror.Examples.MultipleMatch` 네임스페이스)
**파일**: `Assets/Mirror/Examples/MultipleMatches/Scripts/MatchNetworkManager.cs`

**역할**:
- 네트워크 연결 관리
- 서버/클라이언트 생명주기 관리
- CanvasController를 통한 UI 제어

**주요 메서드**:
```csharp
- OnServerReady(conn)          # 클라이언트 준비 완료 시
- OnServerDisconnect(conn)     # 연결 끊김 처리
- OnClientDisconnect()         # 클라이언트측 연결 끊김
- OnStartServer()              # 서버 시작
- OnStartClient()              # 클라이언트 시작
```

**Transport 설정**:
- **KCP Transport** 사용 (포트 7777)
- NoDelay: 활성화
- 최대 메시지 크기: 297KB (신뢰성) / 1.2KB (비신뢰성)

### 2. MatchController

**위치**: `Assets/Mirror/Examples/MultipleMatches/Scripts/MatchController.cs`
**역할**: 개별 매치의 게임 로직 관리

**핵심 기능**:

#### A. 플레이어 관리
```csharp
internal NetworkIdentity player1;
internal NetworkIdentity player2;
internal NetworkIdentity currentPlayer;        // 현재 턴의 플레이어
internal NetworkIdentity startingPlayer;       // 시작 플레이어

// 플레이어 데이터 동기화
internal SyncDictionary<NetworkIdentity, MatchPlayerData> matchPlayerData;
```

#### B. 챔피언 선택 시스템 (커스텀 추가)
```csharp
// 캐릭터 선택 데이터
internal Dictionary<int, CharacterElement> DicCharacterElement;

// 카드 요소 (플레이어별)
internal Dictionary<int, List<CardElement>> DicCardElement;

[Command]
CmdCharacterClick(int index)  # 클라이언트가 챔피언 선택
[ClientRpc]
RpcUpdateIndex()               # 모든 클라이언트에 선택 반영
[ClientRpc]
RpcDisablePanel()              # 선택 완료 시 패널 비활성화
```

#### C. 게임 흐름 제어
```csharp
[Command]
CmdMakePlay()          # 플레이어 행동 처리

[ServerCallback]
CheckWinner()          # 승리 조건 확인

[ClientRpc]
RpcShowWinner()        # 승자 표시

RestartGame()          # 게임 재시작
RequestExitGame()      # 매치 종료 요청
ServerEndMatch()       # 서버측 매치 종료
```

#### D. 턴 기반 시스템
- `currentPlayer` SyncVar로 현재 턴 추적
- 턴이 바뀔 때마다 UI 업데이트 (`UpdateGameUI`)
- 플레이어1 → 플레이어2 → 플레이어1 순환

### 3. ChampionImageManager

**위치**: `Assets/Mirror/Examples/MultipleMatches/Scripts/Dark/ChampionImageManager.cs`
**역할**: 챔피언 이미지 리소스 관리

**지원 챔피언 목록** (20개):
```
Aatrox, Ahri, Ashe, Caitlyn, Galio, Garen, Irelia, Jhin,
Kassadin, KogMaw, Lucian, MasterYi, Mordekaiser, Orianna,
Ornn, Shen, Vi, Xerath, Zed, Ziggs
```

**주요 기능**:
```csharp
LoadAllChampions()                  # 모든 챔피언 스프라이트 로드
ShowChampion(Champion champion)     # 특정 챔피언 표시
GetSprite(Champion champion)        # 챔피언 스프라이트 가져오기
ShowChampionByName(string name)     # 이름으로 챔피언 표시
```

**리소스 경로**:
```
Resources/Icons/Splash/{ChampionName}_0 (1).jpg
```

### 4. AI 캐릭터 프리팹

#### PatrolSeekAndSwordAI A Player
- **레이어**: 24 (TeamA)
- **프리팹 원본**: `Assets/TopDownEngine/Demos/Loft3D/Prefabs/AI/PatrolSeekAndSwordAI.prefab`
- **역할**: 플레이어 1의 AI 캐릭터

#### PatrolSeekAndSwordAI B Player
- **레이어**: 25 (TeamB)
- **프리팹 원본**: 동일
- **역할**: 플레이어 2의 AI 캐릭터

**AI 행동 패턴** (TopDown Engine):
1. **Patrol** (순찰): 기본 상태, 지정된 경로 순찰
2. **Seek** (추적): 적 발견 시 추적
3. **Destroy** (공격): 공격 범위 내 적 공격

## 게임 플로우

### 1. 로비 단계
```
1. 플레이어 서버 접속
   ↓
2. 로비 화면 표시
   - 현재 매치 목록 확인
   - 매치 생성 또는 참가
   ↓
3. 매치 생성/참가
   - 2명이 모이면 자동 시작
```

### 2. 챔피언 선택 단계
```
1. 매치 시작 → CharacterSelection 패널 활성화
   ↓
2. 턴제 선택 (각 플레이어 3개씩 선택)
   - Player1 선택 → Player2 선택 → ... (6번 반복)
   ↓
3. 선택 완료 (cnt == 6)
   - 패널 비활성화
   - 게임 시작
```

### 3. 게임 진행 단계
```
1. AI 캐릭터 스폰
   - TeamA vs TeamB
   ↓
2. AI 자동 전투
   - AIBrain 상태 전환
   - 적 탐지 → 추적 → 공격
   ↓
3. 승패 결정
   - 승자 표시
   - 승리 카운트 증가
   ↓
4. 재시작 또는 종료
   - Play Again: 게임 재시작
   - Exit: 로비로 복귀
```

## 네트워크 동기화 구조

### SyncVar 사용
```csharp
[SyncVar(hook = nameof(UpdateGameUI))]
internal NetworkIdentity currentPlayer;  // 현재 턴 플레이어
```
- 서버에서 값 변경 시 모든 클라이언트에 자동 동기화
- hook 메서드를 통해 UI 업데이트

### SyncDictionary 사용
```csharp
internal SyncDictionary<NetworkIdentity, MatchPlayerData> matchPlayerData;
```
- 플레이어별 데이터 (인덱스, 점수, 승수) 동기화
- 변경사항을 실시간으로 모든 클라이언트에 전파

### Command/ClientRpc 패턴
```csharp
[Command]                        // 클라이언트 → 서버
CmdCharacterClick(int index)

[ClientRpc]                      // 서버 → 모든 클라이언트
RpcUpdateIndex(int index, ...)
```

## Network Match Checker

각 매치는 **NetworkMatch** 컴포넌트로 구분됩니다:
- 매치 시작 시 고유한 **matchId** 생성 (GUID)
- MatchController와 Player 객체 모두 같은 matchId 할당
- Mirror는 같은 matchId를 가진 객체들만 서로 통신하도록 필터링
- 다른 매치의 네트워크 메시지는 수신하지 않음

## 주요 데이터 구조

### MatchPlayerData
```csharp
public struct MatchPlayerData
{
    public int playerIndex;      // 플레이어 인덱스 (0 or 1)
    public int wins;             // 승수
    public CellValue currentScore;  // 현재 점수
}
```

### CharacterElement
캐릭터 선택 UI 요소를 나타냄
- 플레이어 ID 저장
- 챔피언 정보 표시

### CardElement
플레이어가 선택한 챔피언 카드
- 각 플레이어당 3개씩 선택

## 레이어 설정

| 레이어 번호 | 레이어 이름 | 용도 |
|------------|------------|------|
| 24 | TeamA | 플레이어 1의 AI 캐릭터 |
| 25 | TeamB | 플레이어 2의 AI 캐릭터 |

**중요**: AI가 서로를 인식하려면 `AIBrain`의 타겟 레이어 마스크를 상대 팀 레이어로 설정해야 합니다.

## 특징 및 장점

### 1. 확장성
- 여러 매치를 동시에 실행 가능
- 서버 리소스 효율적 사용
- 매치 수에 따라 동적으로 스케일

### 2. 독립성
- 각 매치는 완전히 독립적
- 한 매치의 문제가 다른 매치에 영향 없음
- matchId 기반 격리

### 3. 턴제 시스템
- 공정한 게임 진행
- 네트워크 트래픽 최소화
- 치트 방지 (서버 권한)

### 4. 유연한 게임 종료
- 플레이어 연결 끊김 처리
- 재시작 기능
- 로비 복귀 기능

## 개선 가능 사항

### 1. AI 실제 스폰 구현
현재는 씬에 배치된 AI를 사용하지만, 다음과 같이 개선 가능:
```csharp
// MatchController.cs에서
[Server]
void SpawnPlayerCharacters()
{
    // 선택한 챔피언에 따라 프리팹 스폰
    GameObject char1 = Instantiate(championPrefab1, spawnPoint1);
    GameObject char2 = Instantiate(championPrefab2, spawnPoint2);
    
    NetworkServer.Spawn(char1);
    NetworkServer.Spawn(char2);
}
```

### 2. 챔피언별 스탯 시스템
```csharp
[System.Serializable]
public class ChampionStats
{
    public string championName;
    public int health;
    public int attack;
    public int defense;
    public float moveSpeed;
}
```

### 3. 스킬 시스템
선택한 챔피언에 따라 다른 스킬 사용

### 4. 매치메이킹 시스템
- ELO/MMR 기반 매칭
- 대기열 시스템
- 자동 매칭

### 5. 관전 모드
다른 플레이어의 매치를 관전할 수 있는 기능

## 참고 자료

- [Mirror MultipleMatches 예제 README](./Assets/Mirror/Examples/MultipleMatches/README.md)
- [Mirror 문서 - Network Match](https://mirror-networking.gitbook.io/docs/components/network-match)
- [TopDown Engine AI 설정 가이드](./AI_Battle_Setup_Guide.md)

## 요약

`LTF_MirrorMultipleMatches` 씬은:
- ✅ Mirror의 Multiple Matches 패턴을 활용한 멀티플레이어 시스템
- ✅ 턴제 챔피언 선택 시스템
- ✅ 1v1 AI 대전 기능
- ✅ 확장 가능한 매치 시스템
- ✅ 20개의 LOL 챔피언 지원

이 구조는 카드 게임, 턴제 전략 게임, 퍼즐 게임 등 다양한 1v1 게임에 적용할 수 있는 견고한 기반을 제공합니다.

