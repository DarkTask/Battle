namespace Quantum
{
    using Photon.Deterministic;

    /// <summary>
    /// 전투 순서 지정 시스템
    /// </summary>
    public unsafe class OrderSetupSystem : SystemMainThread
    {
        public override void Update(Frame f)
        {
            var globals = f.Globals;

            if (globals->CurrentPhase != (int)GamePhaseSystem.Phase.OrderSetup)
                return;

            // 싱글 플레이어 테스트: Player B (상대방) 자동 제출
            AutoSubmitForOpponent(f);

            // 양쪽 플레이어가 모두 제출했는지 확인
            if (AreBothPlayersReady(f))
            {
                // 전투 시작
                Log.Info("✅ Both players submitted orders! Starting battle...");
                GamePhaseSystem.ChangePhase(f, globals, GamePhaseSystem.Phase.Battle);
            }
        }

        /// <summary>
        /// 싱글 플레이어 테스트용: Player B (상대방) 만 자동 제출
        /// Player A는 BattleOrderUI에서 직접 제출
        /// </summary>
        static void AutoSubmitForOpponent(Frame f)
        {
            var filter = f.Filter<PlayerGameData>();
            while (filter.NextUnsafe(out var entity, out var data))
            {
                // Player B (PlayerRef == 1) 만 자동 제출
                // Player A (PlayerRef == 0) 는 UI에서 제출
                if (!data->OrderSubmitted && data->PlayerRef == 1)
                {
                    SubmitOrder(f, data->PlayerRef, 0, 1, 2);
                    Log.Info($"⏰ Auto submitted order for Player B (opponent)");
                }
            }
        }

        /// <summary>
        /// 자동 Order 제출 (테스트용)
        /// </summary>
        static void AutoSubmitOrders(Frame f)
        {
            var filter = f.Filter<PlayerGameData>();
            while (filter.NextUnsafe(out var entity, out var data))
            {
                if (!data->OrderSubmitted)
                {
                    // 기본 순서: 0, 1, 2 (선택한 순서대로)
                    SubmitOrder(f, data->PlayerRef, 0, 1, 2);
                    Log.Info($"⏰ Auto submitted order for player {data->PlayerRef}");
                }
            }
        }

        /// <summary>
        /// 전투 순서 제출
        /// </summary>
        public static bool SubmitOrder(Frame f, PlayerRef player, int slot1, int slot2, int slot3)
        {
            var globals = f.Globals;

            if (globals->CurrentPhase != (int)GamePhaseSystem.Phase.OrderSetup)
            {
                Log.Error("Cannot submit order: wrong phase");
                return false;
            }

            // 플레이어 데이터 가져오기
            var filter = f.Filter<PlayerGameData>();
            while (filter.NextUnsafe(out var entity, out var data))
            {
                if (data->PlayerRef == player)
                {
                    // 유효성 검증
                    if (slot1 < 0 || slot1 >= 3 || slot2 < 0 || slot2 >= 3 || slot3 < 0 || slot3 >= 3)
                    {
                        Log.Error($"Invalid slot values: {slot1}, {slot2}, {slot3}");
                        return false;
                    }

                    // 중복 검사
                    if (slot1 == slot2 || slot2 == slot3 || slot1 == slot3)
                    {
                        Log.Error($"Duplicate slot values: {slot1}, {slot2}, {slot3}");
                        return false;
                    }

                    // 순서 저장 (QList는 Clear 후 Add 사용)
                    var battleOrder = f.ResolveList(data->BattleOrder);
                    battleOrder.Clear();
                    battleOrder.Add(slot1);
                    battleOrder.Add(slot2);
                    battleOrder.Add(slot3);
                    data->OrderSubmitted = true;

                    // SelectedChampions 현재 상태 확인
                    var selectedChampions = f.ResolveList(data->SelectedChampions);
                    string selectedStr = "[";
                    for (int i = 0; i < selectedChampions.Count; i++)
                    {
                        selectedStr += selectedChampions[i] + (i < selectedChampions.Count - 1 ? ", " : "");
                    }
                    selectedStr += "]";

                    Log.Info($"✅ Order submitted: Player={player}, Order=[{slot1}, {slot2}, {slot3}]");
                    Log.Info($"   BattleOrder.Count={battleOrder.Count}, SelectedChampions={selectedStr}");

                    f.Signals.OnOrderSubmitted(player);
                    return true;
                }
            }

            Log.Error($"Player data not found: {player}");
            return false;
        }

        /// <summary>
        /// 양쪽 플레이어가 모두 준비되었는가?
        /// </summary>
        static bool AreBothPlayersReady(Frame f)
        {
            int readyCount = 0;

            var filter = f.Filter<PlayerGameData>();
            while (filter.NextUnsafe(out var entity, out var data))
            {
                if (data->OrderSubmitted)
                    readyCount++;
            }

            return readyCount >= 2;
        }
    }
}
