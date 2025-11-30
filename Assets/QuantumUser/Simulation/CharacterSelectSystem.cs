namespace Quantum
{
    using Photon.Deterministic;
    using System;

    /// <summary>
    /// 캐릭터 선택 시스템 (6턴 교대 선택)
    /// 턴 순서: A → B → B → A → A → B
    /// </summary>
    public unsafe class CharacterSelectSystem : SystemMainThread
    {
        public override void Update(Frame f)
        {
            var globals = f.Globals;

            if (globals->CurrentPhase != (int)GamePhaseSystem.Phase.CharacterSelect)
                return;

            // 6턴 완료 시 다음 페이즈로
            if (globals->SelectTurn > 6)
            {
                GamePhaseSystem.ChangePhase(f, globals, GamePhaseSystem.Phase.OrderSetup);
            }
        }

        /// <summary>
        /// 플레이어가 챔피언을 선택했을 때 (Input으로부터 호출)
        /// </summary>
        public static bool SelectChampion(Frame f, PlayerRef player, int championId)
        {
            var globals = f.Globals;

            if (globals->CurrentPhase != (int)GamePhaseSystem.Phase.CharacterSelect)
            {
                Log.Error($"Cannot select champion: wrong phase {globals->CurrentPhase}");
                return false;
            }

            // 현재 턴의 플레이어 가져오기 (싱글 플레이어 테스트용)
            // 입력을 보낸 플레이어가 아닌, 현재 턴의 플레이어로 처리
            PlayerRef currentTurnPlayer = GetCurrentTurnPlayer(f, globals);
            if (currentTurnPlayer == PlayerRef.None)
            {
                Log.Error($"Invalid turn: {globals->SelectTurn}");
                return false;
            }

            // 싱글 플레이어 테스트: 현재 턴의 플레이어로 강제 설정
            player = currentTurnPlayer;

            // 플레이어 데이터 가져오기
            var playerData = GetPlayerData(f, player);
            if (playerData == null)
            {
                Log.Error($"PlayerData not found for {player}");
                return false;
            }

            // 이미 3개 선택했는지 확인
            if (playerData->SelectedCount >= BattleGameConfig.MAX_SELECTION)
            {
                Log.Error($"Already selected {playerData->SelectedCount} champions");
                return false;
            }

            // 중복 선택 확인 (테스트용으로 주석 처리)
            // if (IsChampionAlreadySelected(f, championId))
            // {
            //     Log.Error($"Champion {championId} already selected");
            //     return false;
            // }

            // 선택 처리
            var selectedList = f.ResolveList(playerData->SelectedChampions);
            selectedList.Add(championId);
            playerData->SelectedCount++;

            // 현재 선택된 챔피언 목록 로그
            string selectedStr = "[";
            for (int i = 0; i < selectedList.Count; i++)
            {
                selectedStr += selectedList[i] + (i < selectedList.Count - 1 ? ", " : "");
            }
            selectedStr += "]";
            Log.Info($"✅ Champion selected: Player={player}, ChampionId={championId}, Count={playerData->SelectedCount}");
            Log.Info($"   SelectedChampions now = {selectedStr}");

            // 시그널 발생 (Simulation 내부)
            f.Signals.OnChampionSelected(player, championId);

            // 이벤트 발생 (View 레이어로 전달)
            int playerIndex = GetCurrentPlayerIndex(globals->SelectTurn);
            Log.Info($"📤 Firing Event: Turn={globals->SelectTurn}, PlayerIndex={playerIndex}, ChampionId={championId}");
            f.Events.ChampionSelectedEvent(player, championId, globals->SelectTurn, playerIndex);

            // 다음 턴으로
            NextTurn(f, globals);

            return true;
        }

        /// <summary>
        /// 자동 선택 (타이머 만료 시)
        /// </summary>
        static void AutoSelectChampion(Frame f, _globals_* globals)
        {
            var currentPlayer = GetCurrentTurnPlayer(f, globals);
            if (currentPlayer == PlayerRef.None)
            {
                Log.Error("No current player for auto select");
                NextTurn(f, globals);
                return;
            }

            // 랜덤으로 선택되지 않은 챔피언 찾기
            int championId = FindRandomUnselectedChampion(f);
            if (championId < 0)
            {
                // 모든 챔피언이 선택됨 - 완전 랜덤 선택
                championId = f.RNG->Next(0, BattleGameConfig.TOTAL_CHAMPIONS);
            }

            Log.Info($"⏰ Auto selecting champion {championId} for player {currentPlayer}");
            SelectChampion(f, currentPlayer, championId);
        }

        /// <summary>
        /// 랜덤으로 선택되지 않은 챔피언 찾기
        /// </summary>
        static int FindRandomUnselectedChampion(Frame f)
        {
            // 선택되지 않은 챔피언 목록 수집
            int unselectedCount = 0;
            Span<int> unselected = stackalloc int[BattleGameConfig.TOTAL_CHAMPIONS];

            for (int i = 0; i < BattleGameConfig.TOTAL_CHAMPIONS; i++)
            {
                if (!IsChampionAlreadySelected(f, i))
                {
                    unselected[unselectedCount++] = i;
                }
            }

            if (unselectedCount == 0)
                return -1;

            // 랜덤 선택
            int randomIndex = f.RNG->Next(0, unselectedCount);
            return unselected[randomIndex];
        }

        /// <summary>
        /// 다음 턴으로
        /// </summary>
        static void NextTurn(Frame f, _globals_* globals)
        {
            globals->SelectTurn++;
            globals->SelectTimer = FP.FromFloat_UNSAFE(10f);  // 10초 리셋 (테스트용)

            // 턴 변경 이벤트 발생 (View 레이어로 전달)
            int currentPlayer = GetCurrentPlayerIndex(globals->SelectTurn);
            f.Events.TurnChangedEvent(globals->SelectTurn, currentPlayer);
        }

        /// <summary>
        /// 턴에 해당하는 플레이어 인덱스 (0=A, 1=B)
        /// 턴 순서: A→B→A→B→A→B (번갈아가며)
        /// </summary>
        static int GetCurrentPlayerIndex(int turn)
        {
            // 홀수 턴 = Player A (0), 짝수 턴 = Player B (1)
            return (turn - 1) % 2;
        }

        /// <summary>
        /// 현재 턴의 플레이어인가?
        /// </summary>
        static bool IsCurrentTurnPlayer(Frame f, _globals_* globals, PlayerRef player)
        {
            var currentPlayer = GetCurrentTurnPlayer(f, globals);
            return currentPlayer == player;
        }

        /// <summary>
        /// 현재 턴의 플레이어 가져오기
        /// 턴 순서: A→B→A→B→A→B (번갈아가며)
        /// </summary>
        static PlayerRef GetCurrentTurnPlayer(Frame f, _globals_* globals)
        {
            int turn = globals->SelectTurn;

            // 턴이 유효 범위 (1~6) 밖이면 None
            if (turn < 1 || turn > 6)
                return PlayerRef.None;

            // 홀수 턴 = Player A (0), 짝수 턴 = Player B (1)
            // NOTE: 싱글 플레이어 테스트에서 f.PlayerCount가 1이어도
            // PlayerGameData 엔티티는 2개 존재하므로 턴 기반으로만 판단
            int playerIndex = (turn - 1) % 2;
            return playerIndex == 0 ? (PlayerRef)0 : (PlayerRef)1;
        }

        /// <summary>
        /// 플레이어 데이터 가져오기
        /// </summary>
        static PlayerGameData* GetPlayerData(Frame f, PlayerRef player)
        {
            var filter = f.Filter<PlayerGameData>();
            while (filter.NextUnsafe(out var entity, out var data))
            {
                if (data->PlayerRef == player)
                    return data;
            }
            return null;
        }

        /// <summary>
        /// 챔피언이 이미 선택되었는가?
        /// </summary>
        static bool IsChampionAlreadySelected(Frame f, int championId)
        {
            var filter = f.Filter<PlayerGameData>();
            while (filter.NextUnsafe(out var entity, out var data))
            {
                for (int i = 0; i < data->SelectedCount; i++)
                {
                    if (f.ResolveList(data->SelectedChampions)[i] == championId)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 아직 선택되지 않은 챔피언 찾기
        /// </summary>
        static int FindUnselectedChampion(Frame f)
        {
            for (int i = 0; i < BattleGameConfig.TOTAL_CHAMPIONS; i++)
            {
                if (!IsChampionAlreadySelected(f, i))
                    return i;
            }
            return -1;
        }
    }
}
