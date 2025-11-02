# MatchControllerEx UI 구조 분석

## 📌 개요

**`MatchControllerEx.prefab`** - 이미 구현된 고퀄리티 챔피언 선택 UI 프리팹입니다.

**위치**: `Assets/Mirror/Examples/MultipleMatches/Prefabs/MatchControllerEx.prefab`

**특징**:
- ✅ 프로페셔널한 LOL 스타일 디자인
- ✅ TextMesh Pro 사용
- ✅ 2560x1440 해상도 기준 (Scale With Screen Size)
- ✅ NetworkIdentity + NetworkMatch + MatchController 완전 통합
- ✅ 12개 챔피언 카드 UI 구현

---

## 🎨 UI 구조 (Hierarchy)

```
MatchControllerEx (Root)
├── [Canvas] (Screen Space - Camera)
│   └── Panels
│       ├── GameText                    # 게임 상태 텍스트 ("Your Turn" 등)
│       ├── Choose_Character ---------- # 챔피언 선택 영역 (구분선)
│       │   └── Grid                    # 챔피언 카드 그리드
│       │       ├── CharacterElement (x12) # 챔피언 카드들
│       │       │   ├── Card_Red / Card_Blue / Card_Orange
│       │       │   ├── Character_Mask  # 캐릭터 이미지
│       │       │   ├── Icon            # 챔피언 아이콘
│       │       │   ├── Stats_Group     # 스탯 (STR, DEX, CON)
│       │       │   ├── Star_On/Off     # 선택 상태 별
│       │       │   └── Text_Name       # 챔피언 이름
│       ├── Panel_Right                 # 오른쪽 패널 (선택된 챔피언)
│       │   ├── Card_Blue (x3)          # Player B 선택 카드
│       │   └── Slider_Blue             # Player B 체력/상태 바
│       ├── Panel_Left                  # 왼쪽 패널 (선택된 챔피언)
│       │   ├── Card_Red (x3)           # Player A 선택 카드
│       │   └── Slider_Yellow           # Player A 체력/상태 바
│       ├── LocalWinCount               # 로컬 플레이어 승수
│       ├── OpponentWinCount            # 상대 플레이어 승수
│       ├── BackButton                  # 나가기 버튼
│       └── ReplayButton                # 다시하기 버튼
```

---

## 🔧 주요 컴포넌트

### 1. MatchController 스크립트 연결

```yaml
Component: MatchController (Mirror.Examples.MultipleMatch)
Fields:
  - canvasGroup: {Canvas의 CanvasGroup}
  - gameText: {GameText TextMeshPro}
  - exitButton: {BackButton}
  - playAgainButton: {ReplayButton}
  - winCountLocal: {LocalWinCount TextMeshPro}
  - winCountOpponent: {OpponentWinCount TextMeshPro}
  - Panels: {Panels GameObject}
  
Network Components:
  - NetworkIdentity (assetId: 2040896761)
  - NetworkMatch
```

### 2. 챔피언 카드 구조 (CharacterElement)

각 챔피언 카드는 다음 요소로 구성:

```
CharacterElement
├── Card_Red/Blue/Orange (Background)
│   └── BackGlow (선택 시 빛나는 효과)
├── Character_Mask (원형 마스크)
│   └── Character (챔피언 전신 이미지)
├── Icon (챔피언 초상화)
├── Stats_Group
│   ├── Text: "STR:10"
│   ├── Text: "DEX:15"
│   └── Text: "CON:20"
├── Star_On (선택 시 표시)
├── Star_Off (미선택 시 표시)
└── Text_Name (챔피언 이름)
```

**색상 구분**:
- 🔴 **Card_Red**: Player A 선택 카드
- 🔵 **Card_Blue**: Player B 선택 카드
- 🟠 **Card_Orange**: 미선택 카드

### 3. Grid Layout

```yaml
Component: Grid Layout Group
Settings:
  - Cell Size: 자동 (Content Size Fitter)
  - Spacing: 적절한 간격
  - Constraint: Flexible (화면 크기에 따라 조정)
```

**현재 구성**: 12개 챔피언 카드 (4행 x 3열 또는 3행 x 4열)

### 4. Side Panels (선택된 챔피언 표시)

#### Panel_Left (Player A)
```
- Card_Red (x3) : 선택된 3개 챔피언 표시
- Slider_Yellow : 상태 바 (체력 또는 게임 진행도)
```

#### Panel_Right (Player B)
```
- Card_Blue (x3) : 선택된 3개 챔피언 표시
- Slider_Blue : 상태 바
```

---

## 📊 현재 구현된 기능

### ✅ 이미 완료된 기능

1. **UI 레이아웃**
   - ✅ 프로페셔널한 디자인
   - ✅ 반응형 레이아웃 (Scale With Screen Size)
   - ✅ 12개 챔피언 카드 슬롯

2. **시각적 요소**
   - ✅ 챔피언 카드 3가지 색상 (Red, Blue, Orange)
   - ✅ 선택 효과 (Star, BackGlow)
   - ✅ 스탯 표시 (STR, DEX, CON)
   - ✅ 상태 바 (Slider)

3. **네트워크 통합**
   - ✅ NetworkIdentity
   - ✅ NetworkMatch (matchId 기반)
   - ✅ MatchController 연결

4. **게임 플로우 UI**
   - ✅ GameText (턴 표시)
   - ✅ 승수 카운터 (LocalWinCount, OpponentWinCount)
   - ✅ 버튼 (BackButton, ReplayButton)

### ⚠️ 추가 필요한 기능

1. **챔피언 데이터 연결**
   - ⏳ 20개 챔피언 데이터 할당 (현재 12개 슬롯)
   - ⏳ ChampionImageManager 연동
   - ⏳ 챔피언 아이콘 이미지 로드

2. **선택 로직**
   - ⏳ 클릭 이벤트 핸들러
   - ⏳ 턴제 선택 시스템 (A→B→A→B...)
   - ⏳ 3초 타이머 추가
   - ⏳ 자동 선택 기능

3. **상태 관리**
   - ⏳ 선택된 챔피언 추적
   - ⏳ 카드 색상 변경 (Orange → Red/Blue)
   - ⏳ Star On/Off 토글
   - ⏳ 선택 완료 시 Panel_Left/Right 업데이트

---

## 🎯 기존 UI와 새 요구사항 매핑

### 현재 UI → 요구사항 매핑

| 요구사항 | 현재 UI | 상태 | 필요 작업 |
|---------|---------|------|----------|
| 20개 챔피언 선택 | Grid (12개 슬롯) | ⚠️ 부족 | +8개 슬롯 추가 |
| 턴제 선택 시스템 | GameText | ✅ 가능 | 로직 구현 |
| 3초 타이머 | ❌ 없음 | ⏳ 추가 필요 | Timer Text 추가 |
| 선택한 3개 표시 | Panel_Left/Right (3개씩) | ✅ 완벽 | 데이터 연결만 |
| 전투 순서 지정 | ❌ 없음 | ⏳ 새 Panel 필요 | OrderSetupPanel 생성 |
| 승수 표시 | LocalWinCount, OpponentWinCount | ✅ 완벽 | 사용 가능 |

---

## 🔨 수정 가이드

### 1. 챔피언 슬롯 20개로 확장

**현재**: 12개 CharacterElement
**목표**: 20개 CharacterElement

**방법**:
1. Grid 선택
2. 기존 CharacterElement 복제 8개
3. Grid Layout Group 설정 조정
   - 4행 x 5열 또는 5행 x 4열

```csharp
// Grid Layout Group 설정 예시
Constraint: Fixed Column Count
Column Count: 5
Cell Size: 자동 조정 또는 (220, 300)
Spacing: (10, 10)
```

### 2. 타이머 추가

**위치**: GameText 위 또는 옆

```
Header (새로 생성)
├── GameText "Player A의 차례"
└── TimerText "03초"  ← 추가
```

**스크립트**:
```csharp
public class MatchController : NetworkBehaviour
{
    [Header("UI - Timer")]
    public TextMeshProUGUI timerText;
    
    [SyncVar] private float turnTimer = 3f;
    
    void Update()
    {
        if (currentState == GameState.CharacterSelect)
        {
            timerText.text = $"{Mathf.CeilToInt(turnTimer)}초";
        }
    }
}
```

### 3. 챔피언 선택 로직 구현

```csharp
public class CharacterElement : MonoBehaviour
{
    public int championIndex;
    public Image card;           // Card_Red/Blue/Orange
    public GameObject starOn;
    public GameObject starOff;
    public TextMeshProUGUI championName;
    
    private bool isSelected = false;
    
    public void OnCardClicked()
    {
        if (isSelected) return;
        
        // MatchController에 선택 알림
        MatchController.Instance.CmdSelectChampion(championIndex);
    }
    
    public void SetSelected(bool selected, int playerIndex)
    {
        isSelected = selected;
        starOn.SetActive(selected);
        starOff.SetActive(!selected);
        
        if (selected)
        {
            // 색상 변경
            if (playerIndex == 0) // Player A
                card.sprite = cardRedSprite;
            else // Player B
                card.sprite = cardBlueSprite;
        }
    }
}
```

### 4. Side Panel 업데이트

선택한 챔피언을 Panel_Left/Right에 표시:

```csharp
public void UpdateSidePanel(int playerIndex, List<ChampionData> champions)
{
    Transform panel = playerIndex == 0 ? panelLeft : panelRight;
    
    for (int i = 0; i < 3 && i < champions.Count; i++)
    {
        Transform card = panel.GetChild(i); // Card_Red 또는 Card_Blue
        
        // 아이콘 업데이트
        Image icon = card.Find("Icon").GetComponent<Image>();
        icon.sprite = champions[i].icon;
        
        // 이름 업데이트
        TextMeshProUGUI nameText = card.Find("Text_Name").GetComponent<TextMeshProUGUI>();
        nameText.text = champions[i].championName;
    }
}
```

---

## 💡 활용 방법

### Option 1: 기존 UI 확장 (권장)

**장점**:
- ✅ 이미 완성도 높은 UI
- ✅ 네트워크 통합 완료
- ✅ 디자인 일관성 유지

**작업**:
1. 슬롯 12개 → 20개 확장
2. 타이머 UI 추가
3. 선택 로직 구현
4. OrderSetupPanel 별도 생성

### Option 2: 새로 제작

**장점**:
- ✅ 완전한 커스터마이징
- ✅ QUICK_START.md 가이드 활용

**단점**:
- ❌ 기존 고퀄리티 UI 버림
- ❌ 더 많은 시간 소요

---

## 🎨 디자인 요소 상세

### 색상 팔레트

```
- Card_Red:    Player A 선택 (빨간색 계열)
- Card_Blue:   Player B 선택 (파란색 계열)
- Card_Orange: 미선택 상태 (오렌지 계열)
- Glow Effects: 선택 시 빛나는 효과
```

### 폰트

- **TextMesh Pro** 사용
- 기본 Font Asset: `d9b454495b7d24a7a933eb61bdfa0607`
- 크기: 35.04 (조정 가능)

### 해상도

- **Reference Resolution**: 2560 x 1440
- **Match Mode**: Width-Height 중간 (0.5)
- 모든 주요 해상도 지원

---

## 📝 체크리스트

### 즉시 사용 가능한 것
- [x] UI 레이아웃
- [x] 네트워크 통합
- [x] 승수 카운터
- [x] 게임 상태 텍스트
- [x] Side Panel (3개 슬롯)

### 추가 필요한 것
- [ ] 슬롯 20개로 확장
- [ ] 타이머 UI 추가
- [ ] 선택 로직 구현
- [ ] ChampionImageManager 연동
- [ ] OrderSetupPanel 생성
- [ ] 전투 시스템 연동

---

## 🚀 빠른 시작 (기존 UI 활용)

### Step 1: 프리팹 인스턴스화
```
1. LTF_MirrorMultipleMatches 씬 열기
2. MatchControllerEx.prefab을 Hierarchy에 드래그
3. 기존 MatchController 비활성화 또는 삭제
```

### Step 2: 슬롯 확장
```
1. Grid 선택
2. CharacterElement 복제 8개
3. Grid Layout Group 설정 (5 columns)
```

### Step 3: 스크립트 연결
```csharp
// CharacterElement에 버튼 이벤트 추가
Button btn = GetComponent<Button>();
btn.onClick.AddListener(() => OnCardClicked());
```

### Step 4: 챔피언 데이터 할당
```csharp
// Start()에서 초기화
for (int i = 0; i < 20; i++)
{
    characterElements[i].championIndex = i;
    characterElements[i].championName.text = ChampionDatabase.GetName(i);
    characterElements[i].icon.sprite = ChampionDatabase.GetIcon(i);
}
```

---

## 📚 참고 스크립트

### 필요한 스크립트 목록

```
Assets/Scripts/Battle/
├── CharacterElement.cs          # 개별 카드 로직
├── ChampionDatabase.cs          # 20개 챔피언 데이터
├── MatchController.cs           # 이미 있음 (수정 필요)
└── UI/
    └── CharacterSelectManager.cs  # 선택 관리
```

---

## 🎯 결론

**현재 MatchControllerEx는 매우 완성도 높은 UI입니다.**

### 추천 접근 방법:

1. ✅ **기존 UI 활용** (80% 완성)
2. 🔧 **슬롯만 확장** (12 → 20개)
3. ➕ **타이머 추가**
4. 💻 **로직 구현**
5. 🎮 **테스트**

**예상 작업 시간**: 4-6시간
- 슬롯 확장: 1시간
- 타이머 추가: 30분
- 선택 로직: 2-3시간
- 데이터 연동: 1시간
- 테스트/버그 수정: 1시간

이 방법이 처음부터 만드는 것보다 **3배 빠릅니다**! 🚀

---

**다음 단계**: 
1. 슬롯 확장부터 시작하시겠습니까?
2. 아니면 새로운 요구사항에 맞춰 처음부터 제작하시겠습니까?

