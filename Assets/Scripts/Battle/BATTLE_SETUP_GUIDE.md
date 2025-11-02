# ⚔️ 전투 시스템 설정 가이드

전투 순서 지정 완료 후, 3라운드 전투를 진행하는 시스템입니다.

---

## 📋 구현 완료 항목

- ✅ **BattleArenaManager.cs**: 전투 아레나 관리, 라운드 진행
- ✅ **BattleCharacterController.cs**: 개별 캐릭터 HP, 애니메이션, 데미지 처리
- ✅ **MatchController**: 전투 순서 저장 및 BattleArenaManager 호출
- ✅ **모든 캐릭터 동일 모델 사용**: Character01.prefab

---

## 🛠️ Unity Scene 설정

### 1. BattleArenaManager GameObject 생성

```
Hierarchy:
└─ BattleArenaManager (새로 생성)
   └─ Canvas (BattleCanvas)
      ├─ RoundText (TextMeshProUGUI)
      ├─ ResultText (TextMeshProUGUI)
      └─ (기타 전투 UI)
```

#### GameObject 생성
1. Hierarchy 우클릭 → Create Empty
2. 이름: `BattleArenaManager`
3. Add Component → `BattleArenaManager` 스크립트
4. Add Component → `Network Identity`

#### Inspector 설정

```
BattleArenaManager 컴포넌트:

[Battle Arena Settings]
├─ Player A Spawn Point: (빈 GameObject, 위치: -3, 0, 0)
├─ Player B Spawn Point: (빈 GameObject, 위치: 3, 0, 0)
└─ Default Character Prefab: Character01 프리팹 드래그

[References]
├─ Battle Canvas: Canvas 연결
├─ Round Text: RoundText 연결
└─ Result Text: ResultText 연결
```

---

### 2. Spawn Points 생성

```
Hierarchy:
├─ PlayerASpawnPoint (Empty GameObject)
│  └─ Position: (-3, 0, 0)
│  └─ Rotation: (0, 90, 0) ← 오른쪽 바라봄
└─ PlayerBSpawnPoint (Empty GameObject)
   └─ Position: (3, 0, 0)
   └─ Rotation: (0, -90, 0) ← 왼쪽 바라봄
```

---

### 3. Character01 Prefab 설정

`Assets/Prefabs/Character01.prefab`을 열고:

#### 필수 컴포넌트 추가

1. **BattleCharacterController** 스크립트 추가
2. **Network Identity** 추가

#### Inspector 설정

```
BattleCharacterController:

[UI]
├─ Health Canvas: (캐릭터 위 Canvas) ← 새로 생성 필요
├─ Health Text: (TextMeshProUGUI)
└─ Health Bar: (Image, Fill Amount)

[Visual]
├─ Animator: (있으면 연결, 없으면 null)
└─ Character Renderer: (MeshRenderer 또는 SkinnedMeshRenderer)
```

#### Health Canvas 생성 (캐릭터 위)

```
Character01 (Prefab):
└─ HealthCanvas (Canvas)
   ├─ Render Mode: World Space
   ├─ Position: (0, 2, 0) ← 캐릭터 머리 위
   ├─ Scale: (0.01, 0.01, 0.01)
   └─ HealthPanel (Image - 배경)
      ├─ HealthBar (Image - Fill)
      └─ HealthText (TextMeshProUGUI)
```

---

### 4. Battle Canvas UI 설정

```
BattleCanvas (Screen Space - Overlay):
├─ RoundText (TextMeshProUGUI)
│  ├─ Position: 상단 중앙
│  ├─ Font Size: 48
│  └─ Alignment: Center
│
├─ ResultText (TextMeshProUGUI)
│  ├─ Position: 중앙
│  ├─ Font Size: 36
│  └─ Alignment: Center
│
└─ (선택) ScorePanel
   ├─ PlayerAScore (Text)
   └─ PlayerBScore (Text)
```

초기 상태: **BattleCanvas는 비활성화** (코드가 자동으로 켬)

---

## 🎮 전투 시스템 동작 흐름

### 1. 전투 순서 제출 완료
```
양쪽 플레이어 순서 제출
    ↓
MatchController.RpcStartBattle()
    ↓
BattleArenaManager.StartBattle(this)
```

### 2. Round 1 시작
```
BattleCanvas 활성화
    ↓
"Round 1" 표시
    ↓
battleOrder[0]에 따라 챔피언 결정
    ↓
Character01.prefab 양쪽에 스폰
    ↓
BattleCharacterController 초기화
    ↓
전투 시작!
```

### 3. 전투 진행 (간단한 턴제)
```
Turn 1: Player A 공격 (10~20 데미지)
    ↓
Player B 체력 감소
    ↓
Turn 2: Player B 공격 (10~20 데미지)
    ↓
Player A 체력 감소
    ↓
... 반복 ...
    ↓
한쪽 HP 0 → 라운드 종료
```

### 4. 승패 판정
```
Round 종료 → 승자 결정 (Player A or B)
    ↓
3라운드 중 2승?
    Yes → 전투 종료
    No  → 다음 라운드
```

### 5. 최종 결과
```
최종 승자 표시
    ↓
5초 대기
    ↓
로비로 복귀 (TODO)
```

---

## 🔍 주요 특징

### 1. 모든 캐릭터가 동일 모델 사용
- `Character01.prefab`을 Player A와 Player B 양쪽에 스폰
- **색상으로 구분**: Player A=빨강, Player B=파랑
- 나중에 `ChampionData.championPrefab`을 활용하여 다양한 모델 사용 가능

### 2. 전투 순서에 따른 챔피언 선택
```csharp
// 예: Player A의 전투 순서 = [1, 0, 2]
Round 1 → setupCards[1] (2번째로 선택한 챔피언)
Round 2 → setupCards[0] (1번째로 선택한 챔피언)
Round 3 → setupCards[2] (3번째로 선택한 챔피언)
```

### 3. 네트워크 동기화
- 서버에서 전투 진행
- ClientRpc로 UI 업데이트
- SyncVar로 HP 동기화

---

## 📝 테스트 방법

### 1. Scene 준비
- [x] BattleArenaManager GameObject 생성 및 설정
- [x] PlayerASpawnPoint, PlayerBSpawnPoint 생성
- [x] Character01.prefab에 BattleCharacterController 추가
- [x] BattleCanvas 생성 (초기 비활성화)

### 2. 실행
1. Host 시작
2. Client 접속
3. 캐릭터 6개 선택
4. 전투 순서 지정
5. 양쪽 확인 버튼 클릭
6. **전투 자동 시작!**

### 3. 예상 로그
```
⚔️ 전투 시작!
🚪 전투 순서 지정 UI 비활성화
🔹 Player A 전투 순서: [1, 0, 2]
🔹 Player B 전투 순서: [2, 1, 0]
🥊 BattleArenaManager: 전투 시작!
⚔️ Round 1 시작!
✅ 캐릭터 스폰: Archer vs Mage
⚔️ 전투 시작!
🔴 Archer attacks for 15 damage!
🔵 Mage attacks for 18 damage!
...
🔴 Round 1: Player A 승리!
⚔️ Round 2 시작!
...
🏆 전투 종료! Winner: Player A
📊 최종 스코어: Player A 2 - 1 Player B
```

---

## 🎨 커스터마이징

### 애니메이션 추가
```csharp
// BattleCharacterController.cs에서:
animator.SetTrigger("Attack");  // 공격 애니메이션
animator.SetTrigger("Hit");     // 피격 애니메이션
animator.SetTrigger("Death");   // 사망 애니메이션
```

### 전투 로직 변경
```csharp
// BattleArenaManager.RunBattle()에서:
int damage = Random.Range(10, 20);  // 랜덤 데미지
// → ChampionData.attackPower 사용하도록 변경 가능
```

### 다양한 모델 사용 (향후)
```csharp
// BattleArenaManager.SpawnCharacters()에서:
GameObject prefab = championData.championPrefab ?? defaultCharacterPrefab;
currentPlayerACharacter = Instantiate(prefab, spawnPosA, rotation);
```

---

## ⚠️ 주의사항

### 1. Network Identity 필수
- BattleArenaManager
- Character01.prefab

### 2. Scene에 하나만 존재
- BattleArenaManager (Singleton)

### 3. Character Prefab 등록
- Character01.prefab을 Network Manager의 Spawnable Prefabs에 추가

```
Network Manager:
└─ Spawnable Prefabs
   └─ Character01 추가!
```

---

## 🐛 문제 해결

### Q: 캐릭터가 스폰되지 않아요
**A**: 
1. Character01.prefab에 NetworkIdentity가 있는지 확인
2. Network Manager → Spawnable Prefabs에 등록했는지 확인

### Q: HP가 동기화되지 않아요
**A**: 
- BattleCharacterController의 currentHealth가 [SyncVar]로 선언되어 있는지 확인

### Q: 전투가 시작되지 않아요
**A**:
1. BattleArenaManager.Instance가 null인지 로그 확인
2. Scene에 BattleArenaManager GameObject가 있는지 확인

### Q: 캐릭터가 같은 위치에 스폰돼요
**A**:
- PlayerASpawnPoint, PlayerBSpawnPoint의 위치 확인
- BattleArenaManager Inspector에서 Spawn Points 연결 확인

---

## 🚀 다음 단계

1. **애니메이션 추가**: 공격, 피격, 사망 애니메이션
2. **스킬 시스템**: 각 챔피언마다 고유 스킬
3. **다양한 모델**: ChampionData.championPrefab 활용
4. **전투 UI 개선**: 스킬 버튼, 체력바, 이펙트
5. **로비 복귀**: 전투 종료 후 자동으로 로비로

---

## 📄 관련 파일

- `Assets/Scripts/Battle/BattleArenaManager.cs`
- `Assets/Scripts/Battle/BattleCharacterController.cs`
- `Assets/Scripts/MatchController.cs`
- `Assets/Prefabs/Character01.prefab`

---

**구현 완료! 이제 Scene 설정만 하면 전투가 시작됩니다!** 🎉

