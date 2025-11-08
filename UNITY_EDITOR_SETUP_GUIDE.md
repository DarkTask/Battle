# Unity 에디터 셋업 가이드 (Quantum Battle Game)

## 목차
1. [CodeGen 실행 (SpawnPoint 컴포넌트 반영)](#1-codegen-실행)
2. [프로젝트 폴더 구조 생성](#2-프로젝트-폴더-구조-생성)
3. [BattleGameConfig 에셋 생성](#3-battlegameconfig-에셋-생성)
4. [ChampionData 에셋 생성 (3개)](#4-championdata-에셋-생성)
5. [Champion EntityPrototype 생성 (3개)](#5-champion-entityprototype-생성)
6. [Quantum 맵 씬 생성](#6-quantum-맵-씬-생성)
7. [RuntimeConfig 설정](#7-runtimeconfig-설정)
8. [테스트 실행](#8-테스트-실행)

---

## 1. CodeGen 실행

SpawnPoint 컴포넌트가 Battle.qtn에 추가되었으므로 C# 코드를 재생성해야 합니다.

### 방법 1: 우클릭 Reimport (추천)
1. Unity 에디터 열기
2. Project 창에서 `Assets/QuantumUser/Simulation/Battle.qtn` 파일 찾기
3. **우클릭** → **Reimport**
4. Console 창에서 에러 없이 완료되는지 확인

### 방법 2: 메뉴에서 실행
1. 상단 메뉴: **Quantum > Code Generation > Run Qtn CodeGen**
2. Console 창 확인

### 확인사항
- Console에 에러 없음
- `Assets/QuantumUser/Simulation/Generated/` 폴더에 새 파일 생성됨
- 특히 `SpawnPoint` 관련 코드가 생성되었는지 확인

---

## 2. 프로젝트 폴더 구조 생성

Unity Project 창에서 다음 폴더들을 생성합니다.

### 폴더 구조
```
Assets/
├── QuantumUser/
│   ├── Resources/
│   │   ├── DB/
│   │   │   ├── Config/          (BattleGameConfig 저장)
│   │   │   ├── Champions/       (ChampionData 저장)
│   │   │   └── Prototypes/      (EntityPrototype 저장)
│   │   └── SimulationConfig     (RuntimeConfig 있는 곳, 이미 존재)
│   └── Scenes/
│       └── BattleTest/          (테스트 씬)
```

### 생성 방법
1. Project 창에서 `Assets/QuantumUser` 폴더 선택
2. **우클릭 > Create > Folder**로 다음 폴더들을 차례로 생성:
   - `Resources` (이미 있을 수 있음)
   - `Resources/DB`
   - `Resources/DB/Config`
   - `Resources/DB/Champions`
   - `Resources/DB/Prototypes`
   - `Scenes` (이미 있을 수 있음)
   - `Scenes/BattleTest`

---

## 3. BattleGameConfig 에셋 생성

### 3.1 에셋 생성
1. Project 창에서 `Assets/QuantumUser/Resources/DB/Config` 폴더 선택
2. **우클릭 > Create > Quantum > AssetObject > BattleGameConfig**
   - 메뉴에 없다면: **우클릭 > Create > Quantum > Custom AssetObject** 선택 후 타입에서 `BattleGameConfig` 선택
3. 파일명: **BattleGameConfig** (그대로 유지)

### 3.2 설정 (Inspector)
생성된 BattleGameConfig 에셋을 선택하면 Inspector에 다음 필드들이 보입니다:

#### 필수 설정:
```
Champion Prototypes: (Size: 12)
  - 일단 Size만 12로 설정 (나중에 EntityPrototype 만든 후 연결)
  - Element 0~11: 비워둠 (None)

Select Time Limit: 3
  - 이미 기본값 3으로 설정되어 있음

Battle Time Limit: 60
  - 이미 기본값 60으로 설정되어 있음

Spawn Positions: (Size: 6)
  - Size를 6으로 설정
  - 각 Element에 다음 값 입력:
    Element 0 (Player A, Slot 0): X: -10, Y: 0, Z: 0
    Element 1 (Player A, Slot 1): X: -10, Y: 0, Z: 2
    Element 2 (Player A, Slot 2): X: -10, Y: 0, Z: 4
    Element 3 (Player B, Slot 0): X: 10,  Y: 0, Z: 0
    Element 4 (Player B, Slot 1): X: 10,  Y: 0, Z: 2
    Element 5 (Player B, Slot 2): X: 10,  Y: 0, Z: 4
```

#### 설정 후:
- **Ctrl + S** 또는 **File > Save** (에셋 저장)

---

## 4. ChampionData 에셋 생성

테스트용으로 3개의 챔피언만 먼저 만듭니다.

### 4.1 Champion 1: 전사 (Warrior)
1. Project 창에서 `Assets/QuantumUser/Resources/DB/Champions` 폴더 선택
2. **우클릭 > Create > Quantum > AssetObject > ChampionData**
3. 파일명: **Champion_Warrior**

#### Inspector 설정:
```
Strength: 10
Dexterity: 5
Constitution: 8
Prefab: (비워둠, 나중에 연결)
```

### 4.2 Champion 2: 궁수 (Archer)
1. 같은 폴더에서 **우클릭 > Create > Quantum > AssetObject > ChampionData**
2. 파일명: **Champion_Archer**

#### Inspector 설정:
```
Strength: 6
Dexterity: 10
Constitution: 5
Prefab: (비워둠, 나중에 연결)
```

### 4.3 Champion 3: 탱커 (Tank)
1. 같은 폴더에서 **우클릭 > Create > Quantum > AssetObject > ChampionData**
2. 파일명: **Champion_Tank**

#### Inspector 설정:
```
Strength: 7
Dexterity: 3
Constitution: 12
Prefab: (비워둠, 나중에 연결)
```

#### 저장:
- **Ctrl + S** 저장

---

## 5. Champion EntityPrototype 생성

EntityPrototype은 Quantum Entity의 "설계도"입니다.

### 5.1 Warrior Prototype

#### 5.1.1 에셋 생성
1. Project 창에서 `Assets/QuantumUser/Resources/DB/Prototypes` 폴더 선택
2. **우클릭 > Create > Quantum > Entity Prototype**
3. 파일명: **ChampionPrototype_Warrior**

#### 5.1.2 컴포넌트 추가 (Inspector)
생성된 프로토타입을 선택하면 Inspector 하단에 **Add Component** 버튼이 있습니다.

**추가할 컴포넌트 (순서대로):**

1. **Transform3D**
   - 클릭: Add Component > Transform3D
   - Position: (0, 0, 0) - 기본값 유지
   - Rotation: (0, 0, 0) - 기본값 유지

2. **NavMeshPathfinder**
   - 클릭: Add Component > NavMeshPathfinder
   - 모든 값 기본값 유지

3. **NavMeshSteeringAgent**
   - 클릭: Add Component > NavMeshSteeringAgent
   - 모든 값 기본값 유지

4. **NavMeshAvoidanceAgent**
   - 클릭: Add Component > NavMeshAvoidanceAgent
   - 모든 값 기본값 유지

**주의**: ChampionStats, BattleState, SimpleAI는 **추가하지 마세요**!
→ 이 컴포넌트들은 BattleSystem이 런타임에 자동으로 추가합니다.

#### 5.1.3 ChampionData 연결
1. `Champion_Warrior` ChampionData 에셋 선택
2. Inspector에서 **Prefab** 필드에 **ChampionPrototype_Warrior** 드래그 앤 드롭

### 5.2 Archer Prototype
위와 동일한 방법으로:
1. **우클릭 > Create > Quantum > Entity Prototype**
2. 파일명: **ChampionPrototype_Archer**
3. 컴포넌트 추가: Transform3D, NavMeshPathfinder, NavMeshSteeringAgent, NavMeshAvoidanceAgent
4. `Champion_Archer` ChampionData의 Prefab 필드에 연결

### 5.3 Tank Prototype
위와 동일한 방법으로:
1. **우클릭 > Create > Quantum > Entity Prototype**
2. 파일명: **ChampionPrototype_Tank**
3. 컴포넌트 추가: Transform3D, NavMeshPathfinder, NavMeshSteeringAgent, NavMeshAvoidanceAgent
4. `Champion_Tank` ChampionData의 Prefab 필드에 연결

---

## 6. Quantum 맵 씬 생성

### 6.1 새 씬 생성
1. **File > New Scene**
2. **Basic (Built-in)** 또는 **Basic (URP)** 선택 (URP 추천)
3. **File > Save As...**
   - 경로: `Assets/QuantumUser/Scenes/BattleTest`
   - 파일명: **BattleTestScene**

### 6.2 기본 오브젝트 정리
Hierarchy에서:
- Main Camera: 유지
- Directional Light: 유지

### 6.3 Quantum Map 오브젝트 추가
1. Hierarchy 빈 공간에서 **우클릭 > Quantum > Map**
2. 이름: **QuantumMap** (기본값)

### 6.4 바닥(Ground) 생성
1. Hierarchy 우클릭 > **3D Object > Plane**
2. 이름: **Ground**
3. Inspector에서:
   - Position: (0, 0, 0)
   - Scale: (5, 1, 5) - 50x50 크기 바닥

### 6.5 NavMesh 설정

#### 6.5.1 Ground에 NavMesh Surface 추가
1. **Ground** 오브젝트 선택
2. Inspector 하단: **Add Component**
3. 검색: **NavMesh Surface** 입력
4. 컴포넌트 추가

#### 6.5.2 NavMesh Bake
1. Ground 오브젝트가 선택된 상태에서
2. NavMesh Surface 컴포넌트 Inspector에서
3. **Bake** 버튼 클릭
4. Scene 창에서 바닥이 파란색으로 표시되면 성공

**만약 NavMesh Surface가 없다면:**
- **Window > Package Manager**
- 좌측 상단: **Unity Registry** 선택
- 검색: **AI Navigation**
- **Install** 클릭 (버전 2.0 이상)

### 6.6 카메라 위치 조정
1. **Main Camera** 선택
2. Inspector에서:
   - Position: (0, 20, -15)
   - Rotation: (45, 0, 0)
   - 이렇게 하면 바닥 전체가 보임

### 6.7 씬 저장
- **Ctrl + S** 저장

---

## 7. RuntimeConfig 설정

### 7.1 RuntimeConfig 에셋 찾기
1. Project 창에서 검색창에 `RuntimeConfig` 입력
2. `Assets/QuantumUser/Resources/SimulationConfig` 폴더 안에 있을 것임
3. **SimulationConfig** 에셋 선택 (이름이 다를 수 있음, 타입이 RuntimeConfig인 에셋)

### 7.2 GameConfig 연결
1. RuntimeConfig 에셋이 선택된 상태에서 Inspector 확인
2. **Game Config** 필드를 찾음 (맨 아래쯤에 있을 것)
3. 우측의 **동그라미 아이콘** 클릭
4. Select AssetObject 창에서 **BattleGameConfig** 선택
5. 또는 Project 창에서 **BattleGameConfig** 에셋을 드래그 앤 드롭

### 7.3 저장
- **Ctrl + S** 저장

---

## 8. 테스트 실행

### 8.1 BattleGameConfig에 ChampionPrototype 연결
이제 모든 에셋이 준비되었으므로:

1. **BattleGameConfig** 에셋 선택
2. Inspector에서 **Champion Prototypes** 섹션
3. Element 0~2에 다음 연결:
   - Element 0: **ChampionPrototype_Warrior**
   - Element 1: **ChampionPrototype_Archer**
   - Element 2: **ChampionPrototype_Tank**
   - Element 3~11: 비워둠 (None) - 나중에 추가

### 8.2 테스트 스크립트 추가 (임시)

#### 8.2.1 QuantumMap 오브젝트에 컴포넌트 추가
1. Hierarchy에서 **QuantumMap** 선택
2. Inspector 하단: **Add Component**
3. 검색: **Quantum Runner** (Quantum 기본 컴포넌트)
   - 만약 이미 있다면 스킵

#### 8.2.2 GameMode 설정
1. QuantumMap 선택
2. Inspector에서 Quantum Runner 컴포넌트 찾기
3. **Game Mode**: **Multiplayer** 선택 (로컬 2인 테스트용)
4. **Player Count**: **2**

### 8.3 Play 버튼 클릭!
1. Unity 에디터 상단의 **▶ Play** 버튼 클릭
2. Console 창 확인:
   - 에러 없이 실행되는지 확인
   - Quantum 초기화 로그 확인

### 8.4 예상 동작
현재는 **View 레이어가 없으므로**:
- 화면에 아무것도 안 보일 수 있음
- Console에 다음 로그가 나오면 성공:
  ```
  [Quantum] Session started
  [Quantum] Frame 0
  PlayerManagementSystem.OnPlayerAdded (player 0)
  PlayerManagementSystem.OnPlayerAdded (player 1)
  GamePhaseSystem: Changed to Lobby
  ```

### 8.5 디버그 확인

#### 8.5.1 Quantum Inspector 열기
1. Play 모드 상태에서
2. **Window > Quantum > Inspector**
3. Frame 데이터 확인:
   - Globals → CurrentPhase (0이면 Lobby)
   - Entities → PlayerGameData 컴포넌트 2개 있는지 확인

#### 8.5.2 수동으로 Phase 테스트 (선택사항)
Play 모드에서 Console 창의 로그를 보면서:
- CharacterSelect 단계로 넘어가는지 확인
- (현재 입력이 없으므로 자동으로 넘어가지는 않을 것)

---

## 9. 트러블슈팅

### 9.1 "BattleGameConfig not found" 에러
**원인**: RuntimeConfig에 GameConfig가 연결 안 됨
**해결**: [7. RuntimeConfig 설정](#7-runtimeconfig-설정) 재확인

### 9.2 "ChampionPrototype is null" 에러
**원인**: BattleGameConfig의 ChampionPrototypes 배열이 비어있음
**해결**: [8.1](#81-battlegameconfig에-championprototype-연결) 재확인

### 9.3 NavMesh 에러
**원인**: NavMesh가 Bake되지 않음
**해결**:
- Ground 오브젝트 선택
- NavMesh Surface 컴포넌트에서 **Bake** 버튼 다시 클릭

### 9.4 "Quantum Session failed to start"
**원인**: Quantum Map 설정이 잘못됨
**해결**:
- QuantumMap 오브젝트 확인
- Map Data 에셋이 생성되었는지 확인
- 없다면 QuantumMap 오브젝트 선택 후 Inspector에서 **Bake Map** 버튼 클릭

### 9.5 CodeGen 에러
**증상**: Battle.qtn 수정 후 컴파일 에러
**해결**:
1. Project 창에서 Battle.qtn 우클릭 > Reimport
2. 여전히 에러 나면: **Quantum > Code Generation > Clear Cache**
3. 그 후: **Quantum > Code Generation > Run All**

---

## 10. 다음 단계 (View 레이어 구현)

테스트가 성공하면:
1. **Input 폴링 스크립트**: Unity Input을 Quantum Input으로 변환
2. **Signal 리스너**: Quantum 이벤트를 받아 UI 업데이트
3. **EntityView**: Quantum Entity를 Unity GameObject로 시각화
4. **UI 구현**: 캐릭터 선택 UI, 전투 순서 UI, 전투 HUD

---

## 요약 체크리스트

**필수 작업 순서:**
- [ ] 1. CodeGen 실행 (Battle.qtn Reimport)
- [ ] 2. 폴더 구조 생성 (Resources/DB/...)
- [ ] 3. BattleGameConfig 에셋 생성 및 설정
- [ ] 4. ChampionData 3개 생성 (Warrior, Archer, Tank)
- [ ] 5. EntityPrototype 3개 생성 및 컴포넌트 추가
- [ ] 6. ChampionData ↔ EntityPrototype 연결
- [ ] 7. BattleTestScene 생성 및 NavMesh Bake
- [ ] 8. RuntimeConfig에 GameConfig 연결
- [ ] 9. BattleGameConfig에 ChampionPrototypes 연결
- [ ] 10. Play 버튼 클릭 및 Console 확인

**성공 기준:**
- Play 모드에서 에러 없음
- Console에 "PlayerManagementSystem.OnPlayerAdded" 로그 2번 출력
- Quantum Inspector에서 PlayerGameData 엔티티 2개 확인

---

**작성일**: 2025-11-08
**프로젝트**: Battle Game (Photon Quantum 3)
**대상**: Unity 에디터 작업 가이드
