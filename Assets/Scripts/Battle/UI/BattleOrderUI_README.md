# 전투 순서 지정 시스템 구현 완료

## 📋 개요
캐릭터 선택 완료 후, 각 플레이어가 선택한 3개 캐릭터의 출전 순서를 지정하는 시스템입니다.
**상대방에게 순서는 비공개**로 처리되며, 1번/2번/3번 슬롯에 배치됩니다.

---

## 🎯 주요 기능

### 1. **비공개 순서 지정**
- 각 플레이어는 자신의 캐릭터 순서만 볼 수 있음
- 상대방의 순서는 전투 시작 전까지 비공개
- Mirror 네트워킹의 `TargetRpc`를 사용하여 구현

### 2. **간단한 클릭 방식 UI**
- 선택한 캐릭터를 클릭 → 빈 슬롯에 자동 배치
- 배치된 슬롯을 클릭 → 배치 취소
- 모든 슬롯 배치 완료 시 확인 버튼 활성화

### 3. **동기화 및 대기**
- 한 플레이어가 순서를 제출하면 "상대방 기다리는 중" 표시
- 양쪽 플레이어 모두 제출하면 자동으로 전투 시작

---

## 📂 구현된 파일

### 1. **BattleOrderUI.cs** (새로 생성)
```
위치: Assets/Scripts/Battle/UI/BattleOrderUI.cs
```

**주요 클래스:**
- `BattleOrderUI`: 전투 순서 지정 UI 메인 컴포넌트
- `BattleOrderSlot`: 개별 슬롯 (캐릭터 표시용)

**주요 메서드:**
- `ShowOrderSetupUI(int playerIndex)`: 순서 지정 UI 표시
- `OnChampionSlotClicked(int championIndex)`: 캐릭터 클릭 → 배치
- `OnBattleOrderSlotClicked(int slotIndex)`: 슬롯 클릭 → 배치 취소
- `OnConfirmButtonClicked()`: 확인 버튼 → 서버로 순서 전송

---

### 2. **MatchController.cs** (수정)
```
위치: Assets/Scripts/MatchController.cs
```

**추가된 내용:**
- `Instance` 싱글톤 추가
- `player1OrderSubmitted`, `player2OrderSubmitted`: 제출 상태 추적

**주요 네트워크 메서드:**
```csharp
// 캐릭터 선택 완료 후 자동 호출
[ClientRpc]
public void RpcDisablePanel()
{
    Panels.SetActive(false);
    StartBattleOrderSetup(); // 전투 순서 UI 표시
}

// 클라이언트 → 서버: 순서 제출
[Command(requiresAuthority = false)]
public void CmdSubmitBattleOrder(int slot1, int slot2, int slot3, NetworkConnectionToClient sender = null)
{
    // 유효성 검사
    // BattleGameManager에 순서 저장
    // 양쪽 모두 제출하면 RpcStartBattle() 호출
}

// 서버 → 특정 클라이언트: 제출 확인 (비공개)
[TargetRpc]
void TargetBattleOrderConfirmed(NetworkConnection target, int slot1, int slot2, int slot3)

// 서버 → 모든 클라이언트: 전투 시작
[ClientRpc]
void RpcStartBattle()
```

---

### 3. **BattleGameManager.cs** (수정)
```
위치: Assets/Scripts/Battle/BattleGameManager.cs
```

**추가된 메서드:**
```csharp
[Server]
public void StartBattle()
{
    // 양쪽 전투 순서 완료 확인
    // GameState.Battle_Round1로 전환
}

void OnBattleStart()
{
    // 전투 시작 시 호출
    // 전투 순서 디버그 로그 출력
}
```

---

### 4. **PlayerGameData.cs** (이미 존재)
```
위치: Assets/Scripts/Battle/Data/PlayerGameData.cs
```

이미 구현되어 있던 항목:
- `battleOrder`: int[3] 배열 (순서 저장)
- `IsBattleOrderComplete()`: 순서 완료 여부 확인
- `GetChampionAtSlot(int slotIndex)`: 특정 슬롯의 챔피언 가져오기

---

## 🎮 사용 흐름

### 1단계: 캐릭터 선택 (기존)
```
Player A 선택 → Player B 선택 → ... (총 6턴)
```

### 2단계: 선택 완료
```
MatchController.CmdCharacterClick() 6번 호출
→ cnt == 6
→ RpcDisablePanel() 호출
→ Panels 비활성화
```

### 3단계: 전투 순서 지정 (NEW!)
```
각 클라이언트:
  1. StartBattleOrderSetup() 호출
  2. BattleOrderUI.ShowOrderSetupUI() 표시
  3. 플레이어가 3개 캐릭터를 1/2/3번 슬롯에 배치
  4. 확인 버튼 클릭
  5. CmdSubmitBattleOrder() 호출 (서버로 전송)
```

### 4단계: 양쪽 대기
```
서버:
  - player1OrderSubmitted = true
  - player2OrderSubmitted = true
  - 양쪽 모두 true → RpcStartBattle() 호출
```

### 5단계: 전투 시작
```
RpcStartBattle()
→ BattleOrderUI 숨김
→ BattleGameManager.StartBattle() (서버)
→ GameState.Battle_Round1로 전환
→ 전투 로직 시작
```

---

## 🔒 보안 및 비공개 처리

### 순서가 비공개로 유지되는 방법:
1. **클라이언트 → 서버**: `Command`로 순서 전송
2. **서버에만 저장**: `BattleGameManager.playerA/playerB.battleOrder`
3. **개별 확인**: `TargetRpc`로 본인에게만 확인 메시지 전송
4. **전투 시작 시**: 서버가 순서에 따라 챔피언을 공개하며 전투 진행

### 클라이언트는 볼 수 없는 정보:
- 상대방의 `battleOrder` 배열
- 상대방이 어떤 순서로 배치했는지

---

## 🛠️ Unity에서 설정해야 할 것

### Scene에 추가할 GameObject:
```
BattleOrderUI (새로 생성)
├─ OrderPanel (GameObject)
│  ├─ InstructionText (TextMeshProUGUI)
│  ├─ ConfirmButton (Button)
│  ├─ SelectedChampions (Container)
│  │  ├─ Slot_0 (BattleOrderSlot)
│  │  ├─ Slot_1 (BattleOrderSlot)
│  │  └─ Slot_2 (BattleOrderSlot)
│  └─ BattleOrderSlots (Container)
│     ├─ Slot_1st (BattleOrderSlot)
│     ├─ Slot_2nd (BattleOrderSlot)
│     └─ Slot_3rd (BattleOrderSlot)
```

### BattleOrderUI 컴포넌트 인스펙터 설정:
- `orderPanel`: OrderPanel GameObject 연결
- `instructionText`: 안내 텍스트 연결
- `confirmButton`: 확인 버튼 연결
- `selectedChampionSlots[3]`: 선택된 챔피언 슬롯 (드래그 소스)
- `battleOrderSlots[3]`: 1번/2번/3번 슬롯

### BattleOrderSlot 구조:
```
Slot (GameObject)
├─ Button (Button 컴포넌트)
├─ Icon (Image) - 챔피언 아이콘
└─ Text_Name (TextMeshProUGUI) - 챔피언 이름
```

---

## 📝 테스트 방법

### 1. 2개 클라이언트로 테스트
```
1. Host 클라이언트 시작
2. Client 클라이언트 접속
3. 캐릭터 6개 선택 완료
4. 각각 순서 지정 UI 확인
5. 순서 배치 후 확인 버튼 클릭
6. 양쪽 모두 제출 → 전투 시작 로그 확인
```

### 2. 확인할 디버그 로그
```
✅ Player A 전투 순서 제출: [0, 1, 2]
✅ Player B 전투 순서 제출: [2, 0, 1]
✅ 양쪽 플레이어 모두 전투 순서 제출 완료!
⚔️ 전투 시작!
✅ 전투 시작 조건 충족! Round 1 시작
🔹 Player A 전투 순서: 1번=챔피언A, 2번=챔피언B, 3번=챔피언C
🔹 Player B 전투 순서: 1번=챔피언X, 2번=챔피언Y, 3번=챔피언Z
```

---

## 🚀 다음 단계

전투 순서 지정이 완료되었으므로, 이제 다음을 구현할 수 있습니다:

1. **실제 전투 UI 제작**
   - 1 vs 1 전투 화면
   - 체력/공격/스킬 표시
   
2. **라운드별 전투 로직**
   - Round 1: battleOrder[0] 끼리 전투
   - Round 2: battleOrder[1] 끼리 전투
   - Round 3: battleOrder[2] 끼리 전투
   
3. **승패 판정**
   - 3라운드 중 2승 이상한 플레이어 승리
   - 결과 화면 표시

---

## ✅ 구현 완료 체크리스트

- [x] BattleOrderUI.cs 스크립트 생성
- [x] MatchController에 네트워크 로직 추가 (Command/TargetRpc)
- [x] BattleGameManager에 전투 순서 처리 로직 추가
- [x] 전투 순서 완료 후 다음 단계 전환 로직 구현
- [x] 상대방 비공개 처리 (TargetRpc)
- [x] 양쪽 플레이어 대기 동기화
- [x] 린터 오류 없음 확인

---

## 📞 문제 해결

### Q: BattleOrderUI가 표시되지 않아요
**A**: 
- Scene에 BattleOrderUI GameObject가 있는지 확인
- `orderPanel`이 Inspector에서 연결되었는지 확인
- `BattleOrderUI.Instance`가 null이 아닌지 확인

### Q: 순서를 제출했는데 전투가 시작되지 않아요
**A**:
- 상대방도 순서를 제출했는지 확인
- 서버 콘솔에서 "양쪽 플레이어 모두 전투 순서 제출 완료!" 로그 확인
- `player1OrderSubmitted`, `player2OrderSubmitted` 값 확인

### Q: 상대방의 순서가 보여요
**A**:
- 이것은 의도된 것이 아닙니다
- `CmdSubmitBattleOrder`가 서버에서만 실행되는지 확인
- `TargetRpc`가 올바른 클라이언트에게만 전송되는지 확인

---

## 📄 라이센스
이 프로젝트는 Battle 게임의 일부입니다.

