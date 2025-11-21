# 캐릭터 선택 UI 시스템 - 다음 작업 목록

> 커밋: `a36b2107` - 캐릭터 선택 UI 시스템 구현 (작업 중)
>
> 날짜: 2025-11-20

---

## 1. Unity 에디터 재시작

**목적**: `Assets/csc.rsp` 파일 적용 (unsafe 코드 허용)

- Unity 에디터 종료 후 재시작
- 또는 Assets → Reimport All 실행

---

## 2. ChampionDatabase 설정 (8개 캐릭터) ✅ 완료

**파일**: `Assets/QuantumUser/Resources/DB/ChampionDatabase.asset`

### 이미 설정된 8개 캐릭터

| id | 이름 | STR | DEX | CON | HP | ATK | SPD |
|----|------|-----|-----|-----|-----|-----|-----|
| 0 | Knight | 12 | 8 | 14 | 120 | 12 | 4 |
| 1 | Archer | 8 | 14 | 8 | 80 | 14 | 6 |
| 2 | Wizard | 6 | 10 | 8 | 70 | 16 | 5 |
| 3 | Paladin | 14 | 6 | 16 | 140 | 10 | 3 |
| 4 | CamoArcher | 8 | 16 | 6 | 70 | 15 | 7 |
| 5 | Mage | 6 | 12 | 6 | 60 | 18 | 5 |
| 6 | DeathKnight | 16 | 8 | 12 | 110 | 14 | 4 |
| 7 | DarkLord | 14 | 10 | 14 | 130 | 16 | 4 |

### 남은 작업: icon 스프라이트 연결 (선택)

Unity 에디터에서 각 챔피언의 `icon` 필드에 스프라이트 연결:
- 스프라이트 위치: `Assets/SmallScaleInt/TopDown 2D pixel Characters pack 1/Spritesheets/`
- 각 캐릭터 폴더의 `Idle.png` 사용 추천

**참고**: icon이 없어도 UI는 동작함 (빈 이미지로 표시)

---

## 3. Quantum 코드 생성 확인

**Quantum 3에서는 자동 생성됨**

### 확인 방법

1. `Assets/QuantumUser/Simulation/Battle.qtn` 파일 열기
2. 아무 변경 없이 저장 (Ctrl+S)
3. Unity가 자동으로 재컴파일

### 생성된 코드 확인

- 위치: `Assets/Photon/Quantum/Generated/`
- `EventOnChampionSelected` 타입이 있는지 확인

---

## 4. CharacterSelect.prefab에 컨트롤러 추가

**파일**: `Assets/QuantumUser/Resources/Prefabs/UI/CharacterSelect.prefab`

### 단계

1. **프리팹 열기**: Project 창에서 더블클릭

2. **컴포넌트 추가**:
   - Hierarchy에서 루트 GameObject 선택
   - Add Component → `CharacterSelectUIController`

3. **Inspector 필드 연결**:

| 필드 | 연결할 오브젝트 |
|------|----------------|
| Champion DB | ChampionDatabase.asset |
| Character Select Panel | 루트 또는 메인 패널 |
| Character Elements[0] | ScrollRect/Content/CharacterElement 0 |
| Character Elements[1] | ScrollRect/Content/CharacterElement 1 |
| Character Elements[2] | ScrollRect/Content/CharacterElement 2 |
| Character Elements[3] | ScrollRect/Content/CharacterElement 3 |
| Character Elements[4] | ScrollRect/Content/CharacterElement 4 |
| Character Elements[5] | ScrollRect/Content/CharacterElement 5 |
| Character Elements[6] | ScrollRect/Content/CharacterElement 6 |
| Character Elements[7] | ScrollRect/Content/CharacterElement 7 |
| Player A Panel | Panels/Choose_Character/Panel_Left |
| Player B Panel | Panels/Choose_Character/Panel_Right |
| Turn Text | Panels/GameText |
| Timer Text | (있으면 연결, 없으면 비움) |

4. **프리팹 저장**: Ctrl+S 또는 Apply

---

## 5. 테스트 씬에 CharacterSelect 배치

**씬**: `Assets/QuantumUser/Scenes/BattleTest/BattleTestScene_2D.unity`

### 방법 1: 프리팹 배치
1. 씬 열기
2. CharacterSelect.prefab을 Canvas 하위에 드래그
3. 위치/크기 조정

### 방법 2: GamePhase 시작점 변경
- `Assets/QuantumUser/Simulation/TestInitSystem.cs`에서:
```csharp
f.Global->CurrentPhase = (int)GamePhaseSystem.Phase.CharacterSelect;
```

---

## 6. 테스트 실행

### 확인 사항

- [ ] 컴파일 성공 (unsafe 에러 없음)
- [ ] 8개 카드에 아이콘 표시됨
- [ ] 턴 표시 텍스트 ("Player A의 턴")
- [ ] 타이머 카운트다운
- [ ] 카드 클릭 시 콘솔 로그: `🎮 Champion clicked: index=N`
- [ ] 선택한 카드 비활성화

### 예상 콘솔 로그
```
✅ 8개 챔피언 카드 초기화 완료
🎮 Champion clicked: index=2
```

---

## 7. 추가 작업 (선택)

### Signal 구독 활성화

Quantum 코드 생성 확인 후, `CharacterSelectUIController.cs`에서:

```csharp
// WaitForQuantumAndInitialize() 메서드에서 주석 해제:
QuantumEvent.Subscribe<EventOnChampionSelected>(this, OnChampionSelectedCallback);
```

### UpdatePlayerPanel 완성

선택된 챔피언을 좌우 패널에 표시하는 로직 구현 필요

---

## 문제 해결

| 문제 | 해결 방법 |
|------|-----------|
| unsafe 컴파일 에러 | Unity 에디터 재시작 |
| 아이콘 안 보임 | ChampionDatabase.asset의 icon 필드 확인 |
| 클릭 안 됨 | CharacterElement에 Button 컴포넌트 확인 |
| 턴 표시 안 됨 | Turn Text 필드 연결 확인 |
| Quantum Game null | QuantumRunner가 씬에 있는지 확인 |

---

## 관련 파일

- `Assets/QuantumUser/View/CharacterSelectUIController.cs` - UI 컨트롤러
- `Assets/QuantumUser/Resources/Prefabs/UI/CharacterSelect.prefab` - UI 프리팹
- `Assets/Scripts/Battle/Data/ChampionDatabase.cs` - 데이터베이스 클래스
- `Assets/Scripts/Dark/CharacterElement.cs` - 개별 카드 컴포넌트
- `Assets/QuantumUser/Simulation/CharacterSelectSystem.cs` - Quantum 선택 시스템
- `Assets/csc.rsp` - unsafe 코드 허용 설정