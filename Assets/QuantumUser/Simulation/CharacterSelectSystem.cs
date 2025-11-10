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

            // 타이머 감소
            globals->SelectTimer -= f.DeltaTime;

            // 타이머 종료 시 자동 선택
            if (globals->SelectTimer <= FP._0)
            {
                AutoSelectChampion(f, globals);
            }

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

            // 현재 턴의 플레이어인지 확인
            if (!IsCurrentTurnPlayer(f, globals, player))
            {
                Log.Error($"Not your turn: player={player}, turn={globals->SelectTurn}");
                return false;
            }

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
            f.ResolveList(playerData->SelectedChampions).Add(championId);
            playerData->SelectedCount++;

            Log.Info($"✅ Champion selected: Player={player}, ChampionId={championId}, Count={playerData->SelectedCount}");

            // 시그널 발생
            f.Signals.OnChampionSelected(player, championId);

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

            // 테스트용: 항상 첫 번째 챔피언 (championId 0) 선택
            // 중복 선택이 허용되므로 계속 같은 챔피언 선택 가능
            int championId = 0;  // Warrior

            Log.Info($"⏰ Auto selecting champion {championId} for player {currentPlayer}");
            SelectChampion(f, currentPlayer, championId);
        }

        /// <summary>
        /// 다음 턴으로
        /// </summary>
        static void NextTurn(Frame f, _globals_* globals)
        {
            globals->SelectTurn++;
            globals->SelectTimer = FP.FromFloat_UNSAFE(0.3f);  // 0.3초 리셋
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
        /// 턴 순서: A → B → B → A → A → B
        /// </summary>
        static PlayerRef GetCurrentTurnPlayer(Frame f, _globals_* globals)
        {
            int turn = globals->SelectTurn;

            // 플레이어 목록 (PlayerRef는 0부터 시작)
            if (f.PlayerCount < 2)
                return PlayerRef.None;

            PlayerRef playerA = 0;
            PlayerRef playerB = 1;

            // 턴 매핑
            switch (turn)
            {
                case 1: return playerA;
                case 2: return playerB;
                case 3: return playerB;
                case 4: return playerA;
                case 5: return playerA;
                case 6: return playerB;
                default: return PlayerRef.None;
            }
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
