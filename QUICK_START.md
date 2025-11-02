# 🚀 Quick Start - 개발 시작 가이드

## 📌 빠른 개요

이 프로젝트는 **1v1 턴제 배틀 시스템**을 개발 중입니다.

```
캐릭터 선택 → 전투 순서 지정 → 1v1 x3 + 3v3 전투 → 결과 표시 → 반복
```

---

## 🎯 현재 개발 단계

| 단계 | 상태 | 우선순위 |
|------|------|----------|
| 1. 캐릭터 선택 시스템 | 🔄 개발 중 | 🔴 High |
| 2. 전투 순서 지정 | ⏳ 대기 | 🔴 High |
| 3. 전투 시스템 | ⏳ 대기 | 🟡 Medium |
| 4. 결과 및 재시작 | ⏳ 대기 | 🟢 Low |

---

## 📁 프로젝트 구조 (개발 필요)

### 현재 상태
```
Assets/
├── Scenes/Mirror/
│   └── LTF_MirrorMultipleMatches.unity  ✅ 기본 씬
├── Scripts/                              ❌ 비어있음 (개발 필요!)
├── Prefabs/
│   └── CharacterElement.prefab           ✅ 존재
└── LOL/Aatrox/                          ✅ 1개 챔피언
```

### 개발 후 구조 (목표)
```
Assets/
├── Scripts/Battle/                       🎯 생성 필요
│   ├── BattleGameManager.cs
│   ├── ChampionDatabase.cs
│   ├── PlayerGameData.cs
│   ├── UI/
│   │   ├── CharacterSelectUI.cs
│   │   ├── OrderSetupUI.cs
│   │   ├── BattleUI.cs
│   │   └── ResultUI.cs
│   └── Battle/
│       ├── BattleController.cs
│       └── ChampionSpawner.cs
├── Prefabs/Champions/                    🎯 생성 필요
│   ├── Aatrox.prefab
│   └── ... (19개 더)
└── Prefabs/UI/                          🎯 생성 필요
    ├── CharacterSelectPanel.prefab
    ├── OrderSetupPanel.prefab
    └── ResultPanel.prefab
```

---

## ⚡ 즉시 시작하기 (3단계)

### Step 1: 폴더 생성 (1분)
Unity 프로젝트에서:
```
1. Assets/Scripts/Battle 폴더 생성
2. Assets/Scripts/Battle/UI 폴더 생성
3. Assets/Prefabs/Champions 폴더 생성
4. Assets/Prefabs/UI 폴더 생성
```

### Step 2: 핵심 스크립트 생성 (5분)
다음 3개 스크립트를 먼저 생성:

#### 1. `BattleGameManager.cs`
```csharp
using UnityEngine;
using Mirror;

public class BattleGameManager : NetworkBehaviour
{
    public static BattleGameManager Instance;
    
    [Header("Test Settings")]
    public bool soloTestMode = true; // Inspector에서 설정
    
    [Header("Game State")]
    [SyncVar] public GameState currentState;
    [SyncVar] public int currentTurn;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
    }
    
    void Start()
    {
        if (soloTestMode)
            Debug.Log("🎮 Solo Test Mode: 1인 테스트 모드 활성화");
    }
}

public enum GameState
{
    Lobby,
    CharacterSelect,
    OrderSetup,
    Battle_Round1,
    Battle_Round2,
    Battle_Round3,
    Battle_Final,
    Result
}
```

#### 2. `PlayerGameData.cs`
```csharp
using System.Collections.Generic;

[System.Serializable]
public class PlayerGameData
{
    public int playerIndex;                      // 0 = A, 1 = B
    public List<ChampionData> selectedChampions = new List<ChampionData>();
    public int[] battleOrder = new int[3];       // 전투 순서
    public int totalScore;
}

[System.Serializable]
public class ChampionData
{
    public string championName;
    public int championIndex;
    public bool isAlive = true;
}
```

#### 3. `CharacterSelectUI.cs`
```csharp
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Text turnIndicatorText;
    public Text timerText;
    
    [Header("Champion Buttons")]
    public Button[] championButtons = new Button[20];
    
    void Start()
    {
        Debug.Log("✅ Character Select UI 초기화");
    }
    
    public void OnChampionButtonClicked(int index)
    {
        Debug.Log($"Champion {index} selected!");
        // TODO: BattleGameManager에 선택 전달
    }
}
```

### Step 3: 씬에 적용 (3분)
1. `LTF_MirrorMultipleMatches.unity` 씬 열기
2. 빈 GameObject 생성 → "BattleGameManager" 이름 변경
3. `BattleGameManager.cs` 컴포넌트 추가
4. Inspector에서 `Solo Test Mode` 체크 ✅

---

## 🎨 UI 제작 가이드 (상세)

### 📌 UI 구조 전체 개요

```
Canvas (Screen Space - Overlay)
├── CharacterSelectPanel      [Phase 1] 🔴 High
├── OrderSetupPanel           [Phase 2] 🟡 Medium
├── BattleHUD                 [Phase 3] 🟢 Low
└── ResultPanel               [Phase 4] 🟢 Low
```

---

### 1️⃣ 캐릭터 선택 UI (Phase 1 - 필수)

#### 📐 최종 구조
```
CharacterSelectPanel
├── Background (Image - 반투명 검정)
├── Header
│   ├── TitleText "캐릭터 선택"
│   ├── TurnIndicator "Player A의 차례"
│   └── TimerText "03초"
├── ChampionGrid (5x4 그리드)
│   └── ChampionButton x20
│       ├── ChampionIcon (Image)
│       ├── ChampionName (Text)
│       └── SelectedOverlay (Image)
└── PlayerSelection
    ├── PlayerA_Panel
    │   ├── LabelText "Player A"
    │   └── Slots (3개)
    │       ├── Slot1 (Image)
    │       ├── Slot2 (Image)
    │       └── Slot3 (Image)
    └── PlayerB_Panel
        ├── LabelText "Player B"
        └── Slots (3개)
```

#### 🛠️ 제작 단계 (Step by Step)

##### Step 1: Canvas 생성 (1분)

1. **Hierarchy 우클릭** → `UI` → `Canvas`
   - 이름: "UICanvas"

2. **Canvas 설정**
   - Inspector에서:
     - Render Mode: `Screen Space - Overlay`
     - UI Scale Mode: `Scale With Screen Size`
     - Reference Resolution: `1920 x 1080`
     - Match: `0.5` (Width-Height 중간)

3. **EventSystem 자동 생성 확인**
   - Hierarchy에 "EventSystem"이 자동으로 생성되었는지 확인

##### Step 2: CharacterSelectPanel 생성 (5분)

1. **Panel 생성**
   - Canvas 우클릭 → `UI` → `Panel`
   - 이름: "CharacterSelectPanel"

2. **Background 설정**
   - Inspector → Image 컴포넌트:
     - Color: `R:0, G:0, B:0, A:200` (반투명 검정)
   - RectTransform:
     - Anchors: `Stretch-Stretch` (전체 화면)
     - Left: `0`, Right: `0`, Top: `0`, Bottom: `0`

##### Step 3: Header 영역 (3분)

1. **Header Panel 생성**
   ```
   CharacterSelectPanel 우클릭 → Create Empty
   이름: "Header"
   ```

2. **Header 설정**
   - RectTransform:
     - Anchors: `Top-Stretch`
     - Height: `150`
     - Left: `0`, Right: `0`, Top: `0`

3. **Title Text 추가**
   ```
   Header 우클릭 → UI → Text
   이름: "TitleText"
   ```
   - Text 설정:
     - Text: "캐릭터 선택"
     - Font Size: `60`
     - Alignment: `Center`, `Middle`
     - Color: `White`
   - RectTransform:
     - Width: `400`, Height: `80`
     - PosX: `0`, PosY: `-30`

4. **Turn Indicator 추가**
   ```
   Header 우클릭 → UI → Text
   이름: "TurnIndicator"
   ```
   - Text 설정:
     - Text: "Player A의 차례"
     - Font Size: `40`
     - Alignment: `Center`, `Middle`
     - Color: `Yellow (255, 255, 0)`
   - RectTransform:
     - Width: `400`, Height: `50`
     - PosX: `0`, PosY: `-90`

5. **Timer Text 추가**
   ```
   Header 우클릭 → UI → Text
   이름: "TimerText"
   ```
   - Text 설정:
     - Text: "03초"
     - Font Size: `50`
     - Alignment: `Center`, `Middle`
     - Color: `Red (255, 0, 0)`
   - RectTransform:
     - Width: `200`, Height: `60`
     - PosX: `0`, PosY: `-130`

##### Step 4: Champion Grid 생성 (10분)

1. **Grid Panel 생성**
   ```
   CharacterSelectPanel 우클릭 → Create Empty
   이름: "ChampionGrid"
   ```

2. **Grid 설정**
   - RectTransform:
     - Anchors: `Middle-Center`
     - Width: `1200`, Height: `600`
     - PosX: `0`, PosY: `0`

3. **Grid Layout Group 추가**
   - `Add Component` → `Grid Layout Group`
   - 설정:
     - Cell Size: `X:280, Y:140`
     - Spacing: `X:10, Y:10`
     - Constraint: `Fixed Column Count`
     - Column: `5`
     - Child Alignment: `Middle Center`

4. **첫 번째 Champion Button 생성**
   ```
   ChampionGrid 우클릭 → UI → Button
   이름: "ChampionButton_0"
   ```

5. **Button 상세 설정**
   - **Button 컴포넌트:**
     - Interactable: ✅
     - Transition: `Color Tint`
     - Normal Color: `White`
     - Highlighted Color: `Light Yellow (255, 255, 200)`
     - Pressed Color: `Light Blue (200, 255, 255)`
     - Selected Color: `Light Green (200, 255, 200)`
     - Disabled Color: `Gray (128, 128, 128, 128)`

6. **Button 자식 요소 설정**
   
   a. **기본 Text 수정** (Button 하위에 자동 생성된 Text)
   ```
   이름: "ChampionName"
   ```
   - Text: "Aatrox"
   - Font Size: `24`
   - Alignment: `Center`, `Bottom`
   - Color: `White`
   - RectTransform:
     - Anchors: `Bottom-Stretch`
     - Left: `5`, Right: `5`, Bottom: `5`
     - Height: `30`

   b. **Icon Image 추가**
   ```
   ChampionButton_0 우클릭 → UI → Image
   이름: "ChampionIcon"
   ```
   - Source Image: (나중에 할당)
   - Color: `White`
   - Preserve Aspect: ✅
   - RectTransform:
     - Anchors: `Top-Stretch`
     - Left: `10`, Right: `10`, Top: `10`
     - Height: `90`

   c. **Selected Overlay 추가**
   ```
   ChampionButton_0 우클릭 → UI → Image
   이름: "SelectedOverlay"
   ```
   - Source Image: `None` (배경색만)
   - Color: `Green (0, 255, 0, 100)` (반투명)
   - Raycast Target: ❌ (비활성화)
   - RectTransform:
     - Anchors: `Stretch-Stretch`
     - Left: `0`, Right: `0`, Top: `0`, Bottom: `0`
   - **GameObject 비활성화** (기본 숨김)

7. **Button 복제 (19개 더)**
   - `ChampionButton_0` 선택
   - `Ctrl+D` 19번 눌러서 복제
   - 자동으로 Grid에 배치됨
   - 각 버튼 이름 변경: `ChampionButton_1`, `ChampionButton_2`, ... `ChampionButton_19`
   - 각 ChampionName Text 변경:
     ```
     Aatrox, Ahri, Ashe, Caitlyn, Galio,
     Garen, Irelia, Jhin, Kassadin, KogMaw,
     Lucian, MasterYi, Mordekaiser, Orianna, Ornn,
     Shen, Vi, Xerath, Zed, Ziggs
     ```

##### Step 5: Player Selection Slots (8분)

1. **PlayerSelection Panel 생성**
   ```
   CharacterSelectPanel 우클릭 → Create Empty
   이름: "PlayerSelection"
   ```
   - RectTransform:
     - Anchors: `Bottom-Stretch`
     - Left: `0`, Right: `0`, Bottom: `20`
     - Height: `150`

2. **PlayerA_Panel 생성**
   ```
   PlayerSelection 우클릭 → UI → Panel
   이름: "PlayerA_Panel"
   ```
   - Image 컴포넌트:
     - Color: `Blue (100, 150, 255, 150)`
   - RectTransform:
     - Anchors: `Left-Stretch`
     - Left: `50`, Right: `50%`, Top: `0`, Bottom: `0`

3. **PlayerA Label**
   ```
   PlayerA_Panel 우클릭 → UI → Text
   이름: "LabelText"
   ```
   - Text: "Player A"
   - Font Size: `30`
   - Alignment: `Center`, `Top`
   - Color: `White`
   - RectTransform:
     - Anchors: `Top-Stretch`
     - Left: `0`, Right: `0`, Top: `10`
     - Height: `40`

4. **PlayerA Slot Container**
   ```
   PlayerA_Panel 우클릭 → Create Empty
   이름: "SlotsContainer"
   ```
   - RectTransform:
     - Anchors: `Middle-Stretch`
     - Left: `20`, Right: `20`, Top: `55`, Bottom: `10`

5. **Horizontal Layout Group 추가**
   - SlotsContainer 선택
   - `Add Component` → `Horizontal Layout Group`
   - 설정:
     - Spacing: `10`
     - Child Alignment: `Middle Center`
     - Child Force Expand: Width ✅, Height ✅
     - Child Control Size: Width ✅, Height ✅

6. **Slot 생성 (3개)**
   ```
   SlotsContainer 우클릭 → UI → Image
   이름: "Slot_1"
   ```
   - Image:
     - Color: `Dark Gray (50, 50, 50)`
     - Source Image: `None`
   - RectTransform:
     - Width: `100` (Horizontal Layout이 자동 조정)
     - Height: `100`
   
   - `Slot_1` 복제 2번 → `Slot_2`, `Slot_3`

7. **PlayerB_Panel 생성**
   - `PlayerA_Panel` 전체 복제 (`Ctrl+D`)
   - 이름: "PlayerB_Panel"
   - Image Color: `Red (255, 100, 100, 150)`
   - LabelText: "Player B"
   - RectTransform:
     - Anchors: `Right-Stretch`
     - Left: `50%`, Right: `50`, Top: `0`, Bottom: `0`

##### Step 6: CharacterSelectUI 스크립트 연결 (5분)

1. **스크립트 추가**
   - `CharacterSelectPanel` 선택
   - `Add Component` → `Character Select UI` (이전에 만든 스크립트)

2. **UI 요소 연결** (Inspector에서 드래그)
   ```
   Turn Indicator Text: TurnIndicator 드래그
   Timer Text: TimerText 드래그
   ```

3. **Champion Buttons 배열 할당**
   - `Champion Buttons` 배열 크기: `20`
   - 각 버튼을 순서대로 드래그:
     - Element 0: ChampionButton_0
     - Element 1: ChampionButton_1
     - ...
     - Element 19: ChampionButton_19

4. **Player Slots 연결**
   - PlayerA Slots 배열 크기: `3`
     - Element 0: PlayerA_Panel/SlotsContainer/Slot_1
     - Element 1: PlayerA_Panel/SlotsContainer/Slot_2
     - Element 2: PlayerA_Panel/SlotsContainer/Slot_3
   - PlayerB Slots 배열 크기: `3`
     - 동일하게 PlayerB 슬롯 할당

5. **Button 이벤트 연결**
   - 각 ChampionButton 선택
   - Button 컴포넌트 → OnClick() 이벤트:
     - `+` 버튼 클릭
     - CharacterSelectPanel 드래그
     - 함수: `CharacterSelectUI.OnChampionButtonClicked(int)`
     - 파라미터: 해당 버튼 인덱스 (0~19)

   **빠른 방법:** 스크립트에서 자동 연결
   ```csharp
   void Start()
   {
       for (int i = 0; i < championButtons.Length; i++)
       {
           int index = i; // 클로저 문제 방지
           championButtons[i].onClick.AddListener(() => OnChampionButtonClicked(index));
       }
   }
   ```

##### ✅ Phase 1 UI 완성!

**테스트:**
1. Play 버튼 클릭
2. 캐릭터 버튼 클릭 시 Console에 로그 출력 확인

---

### 2️⃣ 전투 순서 지정 UI (Phase 2 - 중요)

#### 📐 최종 구조
```
OrderSetupPanel
├── Background
├── TitleText "전투 순서 지정"
├── InstructionText "드래그하여 순서를 정하세요"
├── PlayerA_OrderPanel
│   ├── LabelText "Player A"
│   ├── ChampionsContainer (선택한 3개)
│   │   ├── DraggableCard_1
│   │   ├── DraggableCard_2
│   │   └── DraggableCard_3
│   └── SlotsContainer (1, 2, 3번 슬롯)
│       ├── OrderSlot_1
│       ├── OrderSlot_2
│       └── OrderSlot_3
├── PlayerB_OrderPanel (동일 구조)
└── ConfirmButton
```

#### 🛠️ 제작 단계 (Step by Step)

##### Step 1: OrderSetupPanel 생성 (3분)

1. **Panel 생성**
   ```
   UICanvas 우클릭 → UI → Panel
   이름: "OrderSetupPanel"
   ```
   - Image Color: `Black (0, 0, 0, 220)`
   - RectTransform: 전체 화면 (Stretch-Stretch)
   - **GameObject 비활성화** (기본 숨김)

2. **Title 추가**
   ```
   OrderSetupPanel 우클릭 → UI → Text
   이름: "TitleText"
   ```
   - Text: "전투 순서 지정"
   - Font Size: `60`
   - Color: `White`
   - RectTransform:
     - Anchors: `Top-Center`
     - PosX: `0`, PosY: `-50`
     - Width: `600`, Height: `80`

3. **Instruction 추가**
   ```
   OrderSetupPanel 우클릭 → UI → Text
   이름: "InstructionText"
   ```
   - Text: "드래그하여 1, 2, 3번 슬롯에 배치하세요"
   - Font Size: `30`
   - Color: `Yellow`
   - RectTransform:
     - Anchors: `Top-Center`
     - PosX: `0`, PosY: `-130`
     - Width: `800`, Height: `40`

##### Step 2: PlayerA Order Panel (10분)

1. **Panel 생성**
   ```
   OrderSetupPanel 우클릭 → UI → Panel
   이름: "PlayerA_OrderPanel"
   ```
   - Image Color: `Blue (100, 150, 255, 100)`
   - RectTransform:
     - Anchors: `Left-Middle`
     - PosX: `240`, PosY: `0`
     - Width: `400`, Height: `600`

2. **Label**
   ```
   PlayerA_OrderPanel 우클릭 → UI → Text
   이름: "LabelText"
   ```
   - Text: "Player A"
   - Font Size: `40`
   - RectTransform:
     - Anchors: `Top-Center`
     - PosY: `-30`

3. **Champions Container** (선택된 챔피언 카드)
   ```
   PlayerA_OrderPanel 우클릭 → Create Empty
   이름: "ChampionsContainer"
   ```
   - RectTransform:
     - Anchors: `Top-Stretch`
     - Left: `20`, Right: `20`, Top: `80`
     - Height: `150`
   
   - `Add Component` → `Horizontal Layout Group`
     - Spacing: `10`
     - Child Alignment: `Upper Center`

4. **Draggable Card 프리팹 생성**
   ```
   ChampionsContainer 우클릭 → UI → Image
   이름: "DraggableCard"
   ```
   - Image:
     - Color: `White`
     - Width: `110`, Height: `140`
   
   - **Icon 추가**
     ```
     DraggableCard 우클릭 → UI → Image
     이름: "Icon"
     ```
     - RectTransform:
       - Anchors: `Top-Stretch`
       - Left: `5`, Right: `5`, Top: `5`
       - Height: `100`
   
   - **Name Text 추가**
     ```
     DraggableCard 우클릭 → UI → Text
     이름: "Name"
     ```
     - Text: "Aatrox"
     - Font Size: `18`
     - RectTransform:
       - Anchors: `Bottom-Stretch`
       - Left: `5`, Right: `5`, Bottom: `5`
       - Height: `25`
   
   - **DraggableChampion 스크립트 추가**
     ```csharp
     using UnityEngine;
     using UnityEngine.EventSystems;
     
     public class DraggableChampion : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
     {
         public int championIndex;
         private Vector3 startPosition;
         private Transform startParent;
         private CanvasGroup canvasGroup;
         
         void Awake()
         {
             canvasGroup = GetComponent<CanvasGroup>();
             if (canvasGroup == null)
                 canvasGroup = gameObject.AddComponent<CanvasGroup>();
         }
         
         public void OnBeginDrag(PointerEventData eventData)
         {
             startPosition = transform.position;
             startParent = transform.parent;
             canvasGroup.blocksRaycasts = false;
             canvasGroup.alpha = 0.6f;
         }
         
         public void OnDrag(PointerEventData eventData)
         {
             transform.position = Input.mousePosition;
         }
         
         public void OnEndDrag(PointerEventData eventData)
         {
             canvasGroup.blocksRaycasts = true;
             canvasGroup.alpha = 1f;
             
             // 슬롯 확인
             OrderSlot slot = GetSlotUnderPointer(eventData);
             if (slot != null && slot.IsEmpty())
             {
                 slot.AssignCard(this);
                 transform.SetParent(slot.transform);
                 transform.localPosition = Vector3.zero;
             }
             else
             {
                 // 원래 위치로 복귀
                 transform.position = startPosition;
                 transform.SetParent(startParent);
             }
         }
         
         OrderSlot GetSlotUnderPointer(PointerEventData eventData)
         {
             GameObject obj = eventData.pointerCurrentRaycast.gameObject;
             if (obj != null)
                 return obj.GetComponent<OrderSlot>();
             return null;
         }
     }
     ```
   
   - Canvas Group 컴포넌트 추가 (스크립트가 자동 추가하지만 미리 추가 가능)

5. **Slots Container** (1, 2, 3번 슬롯)
   ```
   PlayerA_OrderPanel 우클릭 → Create Empty
   이름: "SlotsContainer"
   ```
   - RectTransform:
     - Anchors: `Middle-Stretch`
     - Left: `20`, Right: `20`, PosY: `-50`
     - Height: `300`
   
   - `Add Component` → `Vertical Layout Group`
     - Spacing: `20`
     - Child Alignment: `Upper Center`

6. **Order Slot 생성**
   ```
   SlotsContainer 우클릭 → UI → Image
   이름: "OrderSlot_1"
   ```
   - Image:
     - Color: `Dark Gray (70, 70, 70)`
     - Width: `120`, Height: `80`
   
   - **Slot Number Text**
     ```
     OrderSlot_1 우클릭 → UI → Text
     이름: "SlotNumber"
     ```
     - Text: "1번"
     - Font Size: `30`
     - Color: `Yellow`
     - RectTransform: Center
   
   - **OrderSlot 스크립트 추가**
     ```csharp
     using UnityEngine;
     using UnityEngine.EventSystems;
     
     public class OrderSlot : MonoBehaviour, IDropHandler
     {
         public int slotIndex; // 0, 1, 2
         private DraggableChampion assignedCard;
         
         public bool IsEmpty() => assignedCard == null;
         
         public void AssignCard(DraggableChampion card)
         {
             assignedCard = card;
         }
         
         public void ClearSlot()
         {
             assignedCard = null;
         }
         
         public void OnDrop(PointerEventData eventData)
         {
             // DraggableChampion에서 처리
         }
     }
     ```
   
   - Inspector에서 `Slot Index` 설정: `0`

7. **Slot 복제**
   - OrderSlot_1 복제 2번
   - OrderSlot_2: SlotNumber "2번", slotIndex: `1`
   - OrderSlot_3: SlotNumber "3번", slotIndex: `2`

##### Step 3: PlayerB Order Panel (2분)

- `PlayerA_OrderPanel` 전체 복제
- 이름: "PlayerB_OrderPanel"
- Image Color: `Red (255, 100, 100, 100)`
- Label: "Player B"
- RectTransform:
  - PosX: `-240` (오른쪽)

##### Step 4: Confirm Button (2분)

```
OrderSetupPanel 우클릭 → UI → Button
이름: "ConfirmButton"
```
- RectTransform:
  - Anchors: `Bottom-Center`
  - PosX: `0`, PosY: `50`
  - Width: `300`, Height: `60`
- Text: "순서 확정"
- Font Size: `30`
- Button Colors:
  - Normal: `Green (100, 255, 100)`
  - Highlighted: `Light Green (150, 255, 150)`

##### ✅ Phase 2 UI 완성!

---

### 3️⃣ 전투 HUD (Phase 3)

#### 빠른 제작 (5분)

```
UICanvas 우클릭 → UI → Panel
이름: "BattleHUD"
```

**구성:**
- Round Text: "Round 1/3"
- PlayerA Health Bar
- PlayerB Health Bar
- Score Display: "A: 0 vs B: 0"

(상세 구현은 Phase 3 진행 시 추가)

---

### 4️⃣ 결과 UI (Phase 4)

#### 빠른 제작 (5분)

```
UICanvas 우클릭 → UI → Panel
이름: "ResultPanel"
```

**구성:**
- Title: "게임 종료"
- Score: "Player A: 3승 vs Player B: 2승"
- Winner: "🏆 Winner: Player A 🏆"
- Restart Button

(상세 구현은 Phase 4 진행 시 추가)

---

### 💾 UI 프리팹 저장 (권장)

각 Panel을 프리팹으로 저장:
1. CharacterSelectPanel을 `Assets/Prefabs/UI/` 폴더로 드래그
2. OrderSetupPanel도 동일하게 프리팹화
3. 나중에 재사용 및 수정 용이

---

## 🧪 테스트 모드 사용법

### 1인 테스트 모드 (권장)
```csharp
// BattleGameManager Inspector에서
Solo Test Mode: ✅ 체크

// 결과:
// - Host가 A, B 플레이어 모두 제어
// - 네트워크 연결 불필요
// - 빠른 개발 및 테스트
```

### 디버그 기능 (추가 권장)
```csharp
[Header("Debug")]
public bool debugMode = true;
public KeyCode skipCharacterSelect = KeyCode.F1;
public KeyCode skipToBattle = KeyCode.F2;

void Update()
{
    if (debugMode && Input.GetKeyDown(skipCharacterSelect))
    {
        // 캐릭터 선택 건너뛰기
        AutoSelectAllChampions();
    }
}
```

---

## 📊 개발 체크리스트

### ✅ 준비 단계
- [ ] Unity 프로젝트 열기
- [ ] 폴더 구조 생성
- [ ] 핵심 스크립트 3개 생성
- [ ] BattleGameManager 씬에 추가

### 🔴 Phase 1: 캐릭터 선택 (현재 단계)
- [ ] CharacterSelectUI 제작
- [ ] 20개 버튼 생성 및 연결
- [ ] 턴제 선택 로직 구현
- [ ] 3초 타이머 구현
- [ ] 자동 선택 기능
- [ ] 1인 테스트 모드 동작 확인

### 🟡 Phase 2: 전투 순서 지정
- [ ] OrderSetupUI 제작
- [ ] 드래그 앤 드롭 구현
- [ ] 슬롯 시스템 구현
- [ ] 순서 제출 로직

### 🟢 Phase 3: 전투 시스템
- [ ] BattleController 구현
- [ ] 챔피언 스폰 시스템
- [ ] 1v1 전투 로직
- [ ] 3v3 전투 로직
- [ ] 승패 판정

### ⚪ Phase 4: 결과 및 재시작
- [ ] ResultUI 제작
- [ ] 점수 집계
- [ ] 재시작 버튼

---

## 🔥 자주 묻는 질문 (FAQ)

### Q1: 어디서부터 시작해야 하나요?
**A:** Step 1-3을 순서대로 따라하세요. 10분이면 기본 구조 완성!

### Q2: 멀티플레이어 테스트는 언제 하나요?
**A:** Phase 1-4 완성 후! 지금은 1인 테스트 모드로 개발하세요.

### Q3: 20개 챔피언 프리팹이 없는데요?
**A:** 일단 1개(Aatrox)로 개발하고, 나머지는 임시 프리팹(큐브) 사용 가능.

### Q4: NavMesh 설정이 안 되어 있는데요?
**A:** 캐릭터 선택 단계는 NavMesh 불필요. 전투 단계 개발 시 설정하면 됩니다.

### Q5: 네트워크 동기화가 복잡해 보여요.
**A:** 1인 테스트 모드에서는 신경 쓰지 않아도 됩니다!

---

## 📚 다음 단계

### 개발 완료 후
1. ✅ Phase 1 완성
2. 📖 [`개발_요구사항_및_구현_가이드.md`](./개발_요구사항_및_구현_가이드.md) 참고하여 Phase 2 진행
3. 🎮 1인 테스트로 전체 게임 루프 확인
4. 🌐 멀티플레이어 모드 전환

### 도움이 필요하면
- [LTF_MirrorMultipleMatches 구조 분석](./LTF_MirrorMultipleMatches_구조분석.md) - 네트워크 구조 이해
- [AI 대전 설정 가이드](./AI_Battle_Setup_Guide.md) - AI 설정 방법
- [PROJECT_OVERVIEW.md](./PROJECT_OVERVIEW.md) - 전체 프로젝트 개요

---

## 💡 개발 팁

### 1. 단계별 테스트
```csharp
// 각 단계마다 로그 출력
Debug.Log("✅ [CharacterSelect] Champion selected: " + championName);
Debug.Log("✅ [OrderSetup] Order confirmed: " + string.Join(",", order));
Debug.Log("✅ [Battle] Round started: " + roundNumber);
```

### 2. Inspector 활용
```csharp
[Header("Debug Info")]
[ReadOnly] public string currentStateName;
[ReadOnly] public int selectedChampionCount;

void Update()
{
    currentStateName = currentState.ToString();
    selectedChampionCount = playerA.selectedChampions.Count + playerB.selectedChampions.Count;
}
```

### 3. 빠른 반복 개발
- F1: 캐릭터 선택 건너뛰기
- F2: 전투로 바로 이동
- F3: 결과 화면으로 이동

---

**Happy Coding! 🎮✨**

문제가 생기면 문서를 참고하거나 디버그 로그를 확인하세요!

