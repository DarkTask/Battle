namespace Quantum
{
    using Photon.Deterministic;

    /// <summary>
    /// 플레이어 추가/제거 관리 시스템
    /// </summary>
    public unsafe class PlayerManagementSystem : SystemSignalsOnly,
        ISignalOnPlayerAdded,
        ISignalOnPlayerRemoved
    {
        public void OnPlayerAdded(Frame f, PlayerRef player, bool firstTime)
        {
            Log.Info($"🎮 Player Added: {player}, FirstTime: {firstTime}");

            // PlayerGameData Entity 생성
            var entity = f.Create();

            // PlayerGameData Component 추가
            f.Add<PlayerGameData>(entity);
            var data = f.Unsafe.GetPointer<PlayerGameData>(entity);
            data->PlayerRef = player;
            data->SelectedCount = 0;
            data->OrderSubmitted = false;
            data->TeamId = GetTeamId(f, player);

            Log.Info($"✅ PlayerGameData created for {player}, TeamId: {data->TeamId}");

            // 플레이어가 2명이 되면 게임 시작
            if (f.PlayerCount >= 2)
            {
                var globals = f.Globals;
                if (globals->CurrentPhase == (int)GamePhaseSystem.Phase.Lobby)
                {
                    GamePhaseSystem.ChangePhase(f, globals, GamePhaseSystem.Phase.CharacterSelect);
                }
            }
        }

        public void OnPlayerRemoved(Frame f, PlayerRef player)
        {
            Log.Info($"🎮 Player Removed: {player}");

            // PlayerGameData Entity 제거
            var filter = f.Filter<PlayerGameData>();
            while (filter.NextUnsafe(out var entity, out var data))
            {
                if (data->PlayerRef == player)
                {
                    f.Destroy(entity);
                    Log.Info($"✅ PlayerGameData destroyed for {player}");
                    break;
                }
            }
        }

        /// <summary>
        /// 플레이어의 팀 ID 결정 (0번째 = Team A, 1번째 = Team B)
        /// </summary>
        int GetTeamId(Frame f, PlayerRef player)
        {
            // 연결된 플레이어 수를 세서 팀 ID 결정
            int teamId = 0;
            for (int i = 0; i < f.PlayerCount; i++)
            {
                if (i == player)
                    return teamId;
                teamId++;
            }
            return 0; // 기본값
        }
    }
}
