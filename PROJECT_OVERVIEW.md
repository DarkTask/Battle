# Battle 프로젝트 개요

## 프로젝트 소개

**Battle**은 Unity 기반의 멀티플레이어 AI 대전 게임 프로젝트입니다. TopDown Engine을 활용하여 탑다운 뷰의 액션 게임을 구현하고 있으며, Mirror 네트워킹 라이브러리를 통해 멀티플레이어 기능을 지원합니다.

## 기술 스택

### 핵심 엔진 및 프레임워크
- **Unity Engine** - 게임 엔진
- **TopDown Engine** (MoreMountains) - 탑다운 액션 게임을 위한 통합 엔진
- **Mirror Networking** - 멀티플레이어 네트워킹 라이브러리

### 주요 Unity 패키지
- `com.unity.ai.navigation` (2.0.9) - AI 네비게이션 (NavMesh)
- `com.unity.cinemachine` (3.1.4) - 카메라 시스템
- `com.unity.inputsystem` (1.14.2) - 새로운 입력 시스템
- `com.unity.render-pipelines.universal` (17.2.0) - URP 렌더 파이프라인
- `com.unity.postprocessing` (3.5.0) - 포스트 프로세싱

### 서드파티 라이브러리
- **MoreMountains.TopDownEngine** - 메인 게임 로직
- **MoreMountains.Tools** - 유틸리티 도구
- **MoreMountains.InventoryEngine** - 인벤토리 시스템
- **Mirror** 및 관련 Transport (Telepathy, KCP, SimpleWeb 등)

## 프로젝트 구조

```
Battle/
├── Assets/
│   ├── Scripts/                    # 커스텀 스크립트 (현재 비어있음)
│   ├── Scenes/                     # 게임 씬들
│   │   ├── SELECT_CHAMPION.unity   # 챔피언 선택 씬
│   │   ├── Mirror/                 # 멀티플레이어 관련 씬
│   │   │   ├── RoomGame.unity      # 룸 게임 씬
│   │   │   ├── RoomOnline.unity    # 온라인 룸 씬
│   │   │   ├── RoomOffline.unity   # 오프라인 룸 씬
│   │   │   └── LTF_MirrorMultipleMatches.unity  # 멀티 매치 씬
│   │   ├── DemoScene_SurvivalClean.unity
│   │   └── MinimalScene3D.unity
│   ├── Mirror/                     # Mirror 네트워킹 라이브러리
│   ├── TopDownEngine/              # TopDown Engine 에셋
│   │   ├── Common/                 # 공통 스크립트 및 프리팹
│   │   └── Demos/                  # 데모 씬 및 프리팹
│   │       └── Loft3D/
│   │           └── Prefabs/AI/
│   │               └── PatrolSeekAndSwordAI.prefab  # AI 프리팹
│   ├── LOL/                        # 리그 오브 레전드 캐릭터 리소스
│   │   └── Aatrox/                 # 아트록스 모델 및 애니메이션
│   ├── Prefabs/
│   │   └── CharacterElement.prefab # 캐릭터 선택 UI 요소
│   ├── StarterAssets/              # Unity Starter Assets
│   └── Resources/                  # 리소스 파일들
├── AI_Battle_Setup_Guide.md        # AI 대전 설정 가이드
└── PROJECT_OVERVIEW.md             # 이 문서
```

## 주요 기능

### 1. AI 대전 시스템
- **1v1 AI 대전**: 두 AI 캐릭터가 서로 다른 팀으로 나뉘어 자동으로 전투
- **팀 시스템**: `TeamA`와 `TeamB` 레이어를 통한 팀 구분
- **AI 행동 패턴**: 
  - Patrol (순찰) → Seek (추적) → Destroy (공격) 상태 전환
  - NavMesh 기반 경로 탐색
  - 반경 기반 적 탐지 시스템

**핵심 AI 프리팹**: `PatrolSeekAndSwordAI`

### 2. 멀티플레이어 시스템
- **Mirror 네트워킹**: 클라이언트-서버 아키텍처
- **룸 시스템**: 
  - `NetworkRoomManager` 기반 룸 생성 및 참가
  - 온라인/오프라인 모드 지원
- **Transport 옵션**: 
  - Telepathy (기본)
  - KCP (고성능)
  - SimpleWeb (WebSocket)
  - Edgegap (호스팅)

### 3. 챔피언 선택 시스템
- **SELECT_CHAMPION 씬**: 플레이어가 사용할 캐릭터를 선택
- **리그 오브 레전드 캐릭터**: 현재 Aatrox 캐릭터 리소스 포함
  - FBX 모델
  - 애니메이션 (Idle, Attack, Death, Run 등)
  - 텍스처 및 머티리얼

## 현재 구현 상태

### 완료된 기능
✅ AI 대전 시스템 기본 구현 (1v1)
✅ 팀 기반 AI 인식 시스템
✅ NavMesh 경로 탐색
✅ Mirror 네트워킹 통합
✅ 챔피언 선택 씬 구조
✅ 기본 캐릭터 리소스 (Aatrox)

### 주요 씬 설명

#### LTF_MirrorMultipleMatches.unity
- **Mirror Multiple Matches 패턴** 기반 멀티플레이어 매칭 시스템
- 하나의 서버에서 여러 1v1 매치 동시 실행 가능
- **주요 기능**:
  - 로비 시스템 (매치 생성/참가)
  - 턴제 챔피언 선택 (각 플레이어 3개씩)
  - 1v1 AI 대전 (`TeamA` vs `TeamB`, Layer 24/25)
  - NetworkMatch로 매치 격리 (matchId 기반)
- **상세 분석**: [`LTF_MirrorMultipleMatches_구조분석.md`](./LTF_MirrorMultipleMatches_구조분석.md) 참고

#### RoomGame.unity / RoomOnline.unity / RoomOffline.unity
- Mirror의 `NetworkRoomManager`를 활용한 룸 시스템
- 플레이어들이 방에 모여 게임을 시작할 수 있는 구조

#### SELECT_CHAMPION.unity
- 챔피언 선택 화면
- 플레이어가 사용할 캐릭터를 선택하는 UI

## 설정 가이드

### AI 대전 설정 방법
자세한 설정 방법은 `AI_Battle_Setup_Guide.md` 파일을 참고하세요.

**간단 요약:**
1. 레이어 설정: `TeamA`, `TeamB` 레이어 생성
2. AI 프리팹 레이어 할당
3. `AIBrain` 컴포넌트에서 타겟 레이어 마스크 설정
   - Patrolling → Seeking 전환: 상대 팀 레이어
   - Seeking → Destroying 전환: 상대 팀 레이어
4. 무기 `DamageOnTouch` 컴포넌트의 타겟 레이어 설정
5. NavMesh Bake 필수

## 개발 환경

- **Unity 버전**: (버전 정보 확인 필요)
- **프로젝트 타입**: 3D URP 프로젝트
- **플랫폼 타겟**: (확인 필요)
- **언어**: C#

## 현재 개발 중인 기능

### 🎮 1v1 턴제 배틀 시스템
**목표**: 완전한 게임 루프 구현

#### Phase 1: 캐릭터 선택 (개발 중)
- ✅ 20개 챔피언 리스트
- 🔄 턴제 선택 시스템 (A→B→A→B→A→B)
- 🔄 3초 타이머 + 자동 선택
- ⏳ 1인 테스트 모드

#### Phase 2: 전투 순서 지정 (설계 완료)
- ⏳ 드래그 앤 드롭 UI
- ⏳ 비공개 슬롯 배치 (1, 2, 3번)
- ⏳ 순서 확인 시스템

#### Phase 3: 전투 시스템 (설계 완료)
- ⏳ Round 1-3: 1v1 전투 (각 +1점)
- ⏳ Final Round: 3v3 단체전 (+2점)
- ⏳ 승패 판정 시스템
- ⏳ 챔피언 스폰 및 AI 제어

#### Phase 4: 결과 및 반복 (설계 완료)
- ⏳ 점수 집계
- ⏳ 우승자 발표
- ⏳ 캐릭터 선택으로 복귀

**상세 계획**: [`개발_요구사항_및_구현_가이드.md`](./개발_요구사항_및_구현_가이드.md)

---

## 향후 개발 방향

### 단기 목표 (1-2개월)
1. ✅ 1인 테스트 모드 완성
2. 🔄 전체 게임 루프 구현
3. ⏳ 기본 UI/UX 완성
4. ⏳ 20개 챔피언 프리팹 준비

### 중기 목표 (3-6개월)
1. **챔피언 시스템 확장**
   - 챔피언별 스탯 차별화
   - 스킬 시스템 구현
   - 밸런스 조정

2. **멀티플레이어 완성**
   - 2인 온라인 플레이 테스트
   - 네트워크 동기화 최적화
   - 매칭 시스템

3. **게임플레이 개선**
   - 다양한 맵
   - 게임 모드 추가
   - 리플레이 시스템

### 장기 목표 (6개월+)
1. **서비스 준비**
   - 서버 호스팅 설정
   - 계정 시스템
   - 랭킹 시스템

2. **콘텐츠 확장**
   - 추가 챔피언 (40개 이상)
   - 시즌 시스템
   - 이벤트 모드

## 참고 문서

### 프로젝트 문서
- **[개발 요구사항 및 구현 가이드](./개발_요구사항_및_구현_가이드.md)** ⭐ - 게임 시스템 개발 계획 및 상세 구현 가이드
- **[LTF_MirrorMultipleMatches 구조 분석](./LTF_MirrorMultipleMatches_구조분석.md)** - 멀티플레이어 매칭 시스템 상세 분석
- **[AI 대전 설정 가이드](./AI_Battle_Setup_Guide.md)** - AI 팀 설정 방법

### 외부 문서
- [TopDown Engine 문서](https://topdown-engine-docs.moremountains.com/)
- [Mirror Networking 문서](https://mirror-networking.gitbook.io/docs/)

## 주의사항

1. **NavMesh 필수**: AI가 제대로 작동하려면 NavMesh를 Bake해야 합니다.
   - `Window > AI > Navigation` 또는 AI Navigation 패키지 사용
   - 바닥 오브젝트에 `NavMeshSurface` 컴포넌트 추가 후 Bake

2. **레이어 설정 중요**: AI가 적을 인식하려면 레이어 설정이 정확해야 합니다.

3. **네트워크 동기화**: Mirror를 사용할 때는 모든 네트워크 오브젝트에 `NetworkIdentity` 컴포넌트가 필요합니다.

## 문제 해결

### AI가 움직이지 않을 때
- NavMesh가 Bake되어 있는지 확인
- AI Navigation 패키지가 설치되어 있는지 확인

### AI가 적을 인식하지 못할 때
- 레이어 설정 확인 (`TeamA`/`TeamB`)
- `AIBrain`의 타겟 레이어 마스크 확인
- `DamageOnTouch`의 타겟 레이어 마스크 확인

### 네트워크 연결 문제
- Transport 설정 확인
- 방화벽 설정 확인
- Mirror 버전 호환성 확인

---

**프로젝트 상태**: 개발 중
**최종 업데이트**: 2025년


