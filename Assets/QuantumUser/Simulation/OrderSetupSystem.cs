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

            // 양쪽 플레이어가 모두 제출했는지 확인
            if (AreBothPlayersReady(f))
            {
                // 전투 시작
                GamePhaseSystem.ChangePhase(f, globals, GamePhaseSystem.Phase.Battle);
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

                    Log.Info($"✅ Order submitted: Player={player}, Order=[{slot1}, {slot2}, {slot3}]");

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
