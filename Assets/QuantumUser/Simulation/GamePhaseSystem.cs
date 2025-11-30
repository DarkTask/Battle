namespace Quantum
{
    using Photon.Deterministic;
    using System;

    /// <summary>
    /// 게임 페이즈 관리 시스템
    /// 0=Lobby, 1=CharSelect, 2=OrderSetup, 3=Battle, 4=Result
    /// </summary>
    public unsafe class GamePhaseSystem : SystemMainThread
    {
        public enum Phase
        {
            Lobby = 0,
            CharacterSelect = 1,
            OrderSetup = 2,
            Battle = 3,
            Result = 4
        }

        public override void OnInit(Frame f)
        {
            // 게임 시작 시 초기화
            var globals = f.Globals;
            globals->CurrentPhase = (int)Phase.Lobby;
            globals->SelectTurn = 0;
            globals->SelectTimer = FP._0;
            globals->CurrentRound = 0;
            globals->PlayerAScore = 0;
            globals->PlayerBScore = 0;
        }

        public override void Update(Frame f)
        {
            var globals = f.Globals;
            var phase = (Phase)globals->CurrentPhase;

            switch (phase)
            {
                case Phase.Lobby:
                    UpdateLobby(f, globals);
                    break;

                case Phase.CharacterSelect:
                    UpdateCharacterSelect(f, globals);
                    break;

                case Phase.OrderSetup:
                    UpdateOrderSetup(f, globals);
                    break;

                case Phase.Battle:
                    UpdateBattle(f, globals);
                    break;

                case Phase.Result:
                    UpdateResult(f, globals);
                    break;
            }
        }

        void UpdateLobby(Frame f, _globals_* globals)
        {
            // 플레이어 2명이 접속하면 캐릭터 선택 페이즈로 전환
            int playerCount = f.PlayerCount;
            if (playerCount >= 2)
            {
                ChangePhase(f, globals, Phase.CharacterSelect);
            }
        }

        void UpdateCharacterSelect(Frame f, _globals_* globals)
        {
            // CharacterSelectSystem에서 처리
            // 6턴 완료 시 OrderSetup으로 전환
        }

        void UpdateOrderSetup(Frame f, _globals_* globals)
        {
            // OrderSetupSystem에서 처리
            // 양쪽 플레이어가 순서 제출하면 Battle로 전환
        }

        void UpdateBattle(Frame f, _globals_* globals)
        {
            // BattleSystem에서 처리
            // 라운드 종료 시 다음 라운드 또는 Result로 전환
        }

        void UpdateResult(Frame f, _globals_* globals)
        {
            // 결과 화면 타이머 감소
            globals->ResultTimer -= f.DeltaTime;

            // 5초 후 캐릭터 선택 화면으로 복귀
            if (globals->ResultTimer <= FP._0)
            {
                Log.Info("🔄 Restarting game - returning to Character Select");
                ResetGameState(f, globals);
                ChangePhase(f, globals, Phase.CharacterSelect);
            }
        }

        /// <summary>
        /// 게임 상태 초기화 (재대결용)
        /// </summary>
        void ResetGameState(Frame f, _globals_* globals)
        {
            // 글로벌 상태 초기화
            globals->SelectTurn = 0;
            globals->SelectTimer = FP._0;
            globals->CurrentRound = 0;
            globals->BattleTimer = FP._0;
            globals->PlayerAScore = 0;
            globals->PlayerBScore = 0;
            globals->ResultTimer = FP._0;

            // 모든 PlayerGameData 초기화
            var filter = f.Filter<PlayerGameData>();
            while (filter.NextUnsafe(out var entity, out var data))
            {
                // 선택한 챔피언 목록 클리어
                var selectedList = f.ResolveList(data->SelectedChampions);
                selectedList.Clear();
                data->SelectedCount = 0;

                // 전투 순서 클리어
                var battleOrder = f.ResolveList(data->BattleOrder);
                battleOrder.Clear();
                data->OrderSubmitted = false;

                Log.Info($"🔄 Reset PlayerGameData for Player {data->PlayerRef}");
            }

            // 전투 엔티티 제거 (챔피언들)
            var battleFilter = f.Filter<BattleState>();
            while (battleFilter.NextUnsafe(out var entity, out var state))
            {
                f.Destroy(entity);
                Log.Info($"🗑️ Destroyed battle entity {entity}");
            }
        }

        /// <summary>
        /// 모든 PlayerGameData 상태 로그 출력
        /// </summary>
        static void LogAllPlayerData(Frame f)
        {
            Log.Info("🔍 ========== Battle Start - PlayerGameData Status ==========");

            var filter = f.Filter<PlayerGameData>();
            while (filter.NextUnsafe(out var entity, out var data))
            {
                var selectedChampions = f.ResolveList(data->SelectedChampions);
                var battleOrder = f.ResolveList(data->BattleOrder);

                // SelectedChampions 목록
                string selectedStr = "[";
                for (int i = 0; i < selectedChampions.Count; i++)
                {
                    selectedStr += selectedChampions[i] + (i < selectedChampions.Count - 1 ? ", " : "");
                }
                selectedStr += "]";

                // BattleOrder 목록
                string orderStr = "[";
                for (int i = 0; i < battleOrder.Count; i++)
                {
                    orderStr += battleOrder[i] + (i < battleOrder.Count - 1 ? ", " : "");
                }
                orderStr += "]";

                Log.Info($"Player {data->PlayerRef} (Team {data->TeamId}):");
                Log.Info($"   SelectedCount = {data->SelectedCount}");
                Log.Info($"   SelectedChampions ({selectedChampions.Count}) = {selectedStr}");
                Log.Info($"   BattleOrder ({battleOrder.Count}) = {orderStr}");
                Log.Info($"   OrderSubmitted = {data->OrderSubmitted}");
            }

            Log.Info("🔍 ============================================================");
        }

        public static void ChangePhase(Frame f, _globals_* globals, Phase newPhase)
        {
            var oldPhase = globals->CurrentPhase;
            globals->CurrentPhase = (int)newPhase;

            Log.Info($"🎮 Phase Changed: {(Phase)oldPhase} → {newPhase}");
            f.Signals.OnPhaseChanged((int)newPhase);
            f.Events.PhaseChangedEvent((int)newPhase);  // View 레이어로 이벤트 전달

            // 페이즈 전환 시 초기화 작업
            switch (newPhase)
            {
                case Phase.CharacterSelect:
                    globals->SelectTurn = 1;
                    globals->SelectTimer = FP.FromFloat_UNSAFE(10f);  // 10초 (테스트용)
                    break;

                case Phase.OrderSetup:
                    // 전투 순서 지정 시작
                    break;

                case Phase.Battle:
                    globals->CurrentRound = 1;
                    globals->BattleTimer = FP.FromFloat_UNSAFE(60);  // 60초
                    // Battle 시작 전 모든 PlayerGameData 상태 확인
                    LogAllPlayerData(f);
                    break;

                case Phase.Result:
                    // 5초 후 캐릭터 선택 화면으로 복귀
                    globals->ResultTimer = FP.FromFloat_UNSAFE(5f);
                    Log.Info("⏱️ Result screen - returning to Character Select in 5 seconds");
                    break;
            }
        }
    }
}
