# Battle 프로젝트

Unity 기반의 멀티플레이어 AI 대전 게임 프로젝트입니다.

## 프로젝트 개요

이 프로젝트는 TopDown Engine과 Mirror 네트워킹을 활용하여 구현된 멀티플레이어 액션 게임입니다. 플레이어는 챔피언을 선택하고 다른 플레이어나 AI와 전투할 수 있습니다.

## 주요 기능

- ✅ **AI 대전 시스템**: 1v1 자동 대전
- ✅ **멀티플레이어 지원**: Mirror 네트워킹 기반
- ✅ **챔피언 선택**: 리그 오브 레전드 스타일 캐릭터 선택
- ✅ **탑다운 액션**: TopDown Engine 기반 게임플레이

## 기술 스택

- **Unity Engine** + **URP**
- **TopDown Engine** (MoreMountains)
- **Mirror Networking**
- **C#**

## 빠른 시작

### 🚀 개발 시작하기
**지금 바로 개발을 시작하려면?**
👉 **[QUICK_START.md](./QUICK_START.md)** - 10분 안에 개발 환경 구축!

### 필수 설정

1. **NavMesh 설정** (전투 시스템 개발 시 필요)
   ```
   Window > AI > Navigation
   ```
   - 바닥 오브젝트에 `NavMeshSurface` 컴포넌트 추가
   - Bake 실행

2. **AI 대전 설정** (전투 시스템 개발 시 필요)
   - `AI_Battle_Setup_Guide.md` 참고

### 주요 씬

- `SELECT_CHAMPION.unity` - 챔피언 선택 화면
- `Mirror/LTF_MirrorMultipleMatches.unity` - AI 대전 테스트 씬
- `Mirror/RoomOnline.unity` - 온라인 멀티플레이어 룸

## 프로젝트 구조

```
Assets/
├── Scripts/              # 커스텀 스크립트
├── Scenes/              # 게임 씬들
├── Mirror/              # Mirror 네트워킹 라이브러리
├── TopDownEngine/       # TopDown Engine 에셋
├── LOL/                 # 캐릭터 리소스 (Aatrox 등)
└── Prefabs/             # 프리팹들
```

## 문서

### 📖 프로젝트 문서
- **[프로젝트 상세 개요](./PROJECT_OVERVIEW.md)** - 전체 프로젝트 구조 및 기술 상세
- **[실전 개발 가이드 (12개 챔피언)](./실전_개발_가이드_12개챔피언.md)** - 즉시 개발 시작 가이드 🔥⭐
- **[개발 요구사항 및 구현 가이드](./개발_요구사항_및_구현_가이드.md)** - 게임 시스템 개발 계획 (20개 챔피언)

### 🔧 기술 문서
- **[LTF_MirrorMultipleMatches 구조 분석](./LTF_MirrorMultipleMatches_구조분석.md)** - 멀티플레이어 매칭 시스템 분석
- **[MatchControllerEx UI 분석](./MatchControllerEx_UI분석.md)** - 기존 챔피언 선택 UI 구조 및 활용 가이드 ⭐
- **[AI 대전 설정 가이드](./AI_Battle_Setup_Guide.md)** - AI 대전 시스템 설정 방법

## 개발 상태

현재 프로젝트는 개발 중입니다.

### 🎯 현재 개발 목표
**1v1 턴제 배틀 시스템** 구현 중
- ✅ Mirror 멀티플레이어 기반 구조 완료
- 🔄 캐릭터 선택 시스템 개발 중
- 🔄 전투 시스템 개발 중
- ⏳ 점수 및 결과 시스템 대기

자세한 개발 계획은 [`개발_요구사항_및_구현_가이드.md`](./개발_요구사항_및_구현_가이드.md) 참고

## 문제 해결

### AI가 움직이지 않을 때
→ NavMesh를 Bake해야 합니다.

### AI가 적을 인식하지 못할 때
→ 레이어 설정 (`TeamA`/`TeamB`)과 `AIBrain`의 타겟 레이어 마스크를 확인하세요.

## 라이선스

(라이선스 정보 추가 필요)

---

더 자세한 정보는 [PROJECT_OVERVIEW.md](./PROJECT_OVERVIEW.md)를 참고하세요.


