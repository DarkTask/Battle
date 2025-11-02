# Battle 스크립트 설정 가이드

## ✅ 생성된 파일

```
Assets/Scripts/Battle/
├── Data/
│   ├── ChampionData.cs              ✅ 생성 완료
│   ├── ChampionDatabase.cs          ✅ 생성 완료
│   └── PlayerGameData.cs            ✅ 생성 완료
├── UI/
│   ├── CharacterElement.cs          ✅ 생성 완료
│   └── CharacterSelectUI.cs         ✅ 생성 완료
├── BattleGameManager.cs             ✅ 생성 완료
└── GameState.cs                     ✅ 생성 완료
```

---

## 🚀 빠른 시작 (5단계)

### Step 1: ChampionDatabase 생성 (2분)

1. **Unity Project 창에서**:
   ```
   Assets/Scripts/Battle/Data 폴더에서 우클릭
   → Create > Battle > Champion Database
   ```

2. **이름 변경**: `ChampionDB`

3. **Inspector에서 우클릭**:
   ```
   ChampionDB 선택 → Inspector 상단 우클릭
   → "Initialize 12 Champions" 클릭
   ```
   
   → 자동으로 12개 챔피언 데이터 생성됨! ✅

---

### Step 2: BattleGameManager GameObject 생성 (1분)

1. **씬 열기**: `Assets/Scenes/Mirror/LTF_MirrorMultipleMatches.unity`

2. **Hierarchy에서**:
   ```
   빈 곳에서 우클릭 → Create Empty
   이름: BattleGameManager
   ```

3. **컴포넌트 추가**:
   ```
   Inspector → Add Component → Battle Game Manager
   ```

4. **설정**:
   ```
   ✅ Solo Test Mode: 체크
   Champion DB: ChampionDB 드래그
   ```

---

### Step 3: MatchControllerEx 설정 (3분)

1. **MatchControllerEx 선택** (Hierarchy에서)

2. **CharacterSelectUI 컴포넌트 추가**:
   ```
   Add Component → Character Select UI
   ```

3. **Inspector 설정**:
   ```
   Champion DB: ChampionDB 드래그
   ```

4. **자동 연결 실행** (편리 기능):
   ```
   CharacterSelectUI 컴포넌트 우클릭
   → "Auto Find Character Elements" 클릭
   ```
   
   → Grid 하위 12개 자동 연결! ✅

5. **수동 연결** (나머지):
   ```
   Game Text: Panels > GameText 드래그
   Player A Panel: Panels > Panel_Left 드래그
   Player B Panel: Panels > Panel_Right 드래그
   
   Player A Slots (3개):
   - Panel_Left 하위 Card_Red (또는 Card_Orange) 3개 찾아서 드래그
   
   Player B Slots (3개):
   - Panel_Right 하위 Card_Blue 3개 찾아서 드래그
   ```

---

### Step 4: Grid 하위 CharacterElement 설정 (5분)

1. **Grid 하위 첫 번째 오브젝트 선택**

2. **CharacterElement 컴포넌트 추가**:
   ```
   Add Component → Character Element
   ```

3. **자동 연결** (편리 기능):
   ```
   CharacterElement 컴포넌트 우클릭
   → "Auto Connect References" 클릭
   ```
   
   → 자동으로 Card, Icon, Star 등 연결! ✅

4. **Sprites 할당** (중요):
   ```
   Card Orange: 오렌지 카드 스프라이트 드래그
   Card Red: 빨간 카드 스프라이트 드래그
   Card Blue: 파란 카드 스프라이트 드래그
   ```

5. **복사**:
   ```
   설정 완료된 CharacterElement 컴포넌트 우클릭
   → Copy Component
   ```

6. **나머지 11개에 붙여넣기**:
   ```
   Grid 하위 나머지 11개 오브젝트 각각 선택
   → Inspector 빈 곳에서 우클릭
   → Paste Component As New
   ```

---

### Step 5: 테스트! (1분)

1. **Play 버튼 클릭** ▶️

2. **확인사항**:
   - ✅ Console에 "캐릭터 선택 시작!" 출력
   - ✅ 챔피언 카드 클릭 가능
   - ✅ 클릭 시 색상 변경 (Orange → Red/Blue)
   - ✅ 3초 대기 시 자동 선택
   - ✅ F1 키 누르면 자동 완료

---

## 🎮 디버그 기능

### 단축키
```
F1: 모든 챔피언 자동 선택 완료
```

### Inspector 메뉴 (우클릭)
```
BattleGameManager:
- Debug: Auto Select All  (모든 선택 완료)
- Debug: Reset Game       (게임 리셋)

ChampionDatabase:
- Initialize 12 Champions (12개 챔피언 생성)

CharacterElement:
- Auto Connect References (참조 자동 연결)

CharacterSelectUI:
- Auto Find Character Elements (Grid 자식 자동 찾기)
```

---

## 🐛 트러블슈팅

### 문제 1: "ChampionDatabase가 할당되지 않았습니다!"
**해결**: 
- BattleGameManager와 CharacterSelectUI에 ChampionDB 드래그

### 문제 2: "CharacterElement[X]가 null입니다!"
**해결**:
- CharacterSelectUI에서 "Auto Find Character Elements" 실행
- 또는 수동으로 12개 드래그

### 문제 3: 카드 클릭이 안 됨
**해결**:
- EventSystem이 씬에 있는지 확인
- Canvas에 GraphicRaycaster 있는지 확인

### 문제 4: 참조가 None으로 표시
**해결**:
- CharacterElement에서 "Auto Connect References" 실행
- 안 되면 수동으로 연결

---

## 📊 체크리스트

- [ ] ChampionDB 생성 및 초기화
- [ ] BattleGameManager GameObject 생성
- [ ] BattleGameManager에 ChampionDB 할당
- [ ] MatchControllerEx에 CharacterSelectUI 추가
- [ ] CharacterSelectUI에 ChampionDB 할당
- [ ] CharacterSelectUI 자동 연결 실행
- [ ] Game Text, Panels 수동 연결
- [ ] Player A/B Slots 연결 (각 3개씩)
- [ ] Grid 하위 12개에 CharacterElement 추가
- [ ] CharacterElement 자동 연결 실행
- [ ] Card Sprites 할당 (Orange, Red, Blue)
- [ ] 나머지 11개에 컴포넌트 복사
- [ ] 테스트 실행

---

## 💡 다음 단계

Phase 1 완료 후:
- [ ] Phase 2: 전투 순서 지정 UI
- [ ] Phase 3: 전투 시스템
- [ ] Phase 4: 결과 화면

---

**준비 완료!** 이제 Play 버튼만 누르면 됩니다! 🚀

