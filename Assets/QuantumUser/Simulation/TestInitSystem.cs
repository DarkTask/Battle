namespace Quantum
{
    using Photon.Deterministic;

    /// <summary>
    /// 2D 테스트용 초기화 시스템
    /// 게임 시작 시 자동으로 Player A, B 데이터 생성 및 Battle 단계 시작
    /// </summary>
    public unsafe class TestInitSystem : SystemMainThreadFilter<TestInitSystem.Filter>
    {
        public struct Filter
        {
            // Dummy filter (항상 한 번만 실행하기 위함)
        }

        private bool _initialized = false;

        public override void Update(Frame f, ref Filter filter)
        {
            // 이미 초기화되었거나 Phase가 0이 아니면 스킵
            if (_initialized || f.Global->CurrentPhase != 0)
                return;

            _initialized = true;

            Log.Info("🎮 [TestInitSystem] Initializing 2D Battle Test...");

            // Player A 데이터 생성
            CreatePlayerData(f, 0, 0, new int[] { 0, 1, 2 });  // Knight, Paladin, DeathKnight

            // Player B 데이터 생성
            CreatePlayerData(f, 1, 1, new int[] { 3, 4, 5 });  // DarkLord, Archer, CamoArcher

            // Battle 단계로 직접 시작
            f.Global->CurrentPhase = (int)GamePhaseSystem.Phase.Battle;
            f.Global->SelectTurn = 0;
            f.Global->SelectTimer = FP._0;
            f.Global->CurrentRound = 0;  // BattleSystem.OnPhaseChanged에서 1로 설정하고 스폰함
            f.Global->BattleTimer = FP.FromFloat_UNSAFE(999f);

            Log.Info("✅ [TestInitSystem] Initialization complete! Battle will start...");
        }

        void CreatePlayerData(Frame f, int playerRef, int teamId, int[] champions)
        {
            var entity = f.Create();
            f.Add<PlayerGameData>(entity);

            var data = f.Unsafe.GetPointer<PlayerGameData>(entity);
            data->PlayerRef = playerRef;
            data->TeamId = teamId;
            data->SelectedCount = champions.Length;
            data->OrderSubmitted = true;

            // 선택된 챔피언 리스트 생성
            data->SelectedChampions = f.AllocateList<int>();
            var selectedChampions = f.ResolveList(data->SelectedChampions);
            foreach (var champ in champions)
            {
                selectedChampions.Add(champ);
            }

            // 전투 순서 생성 (순서대로)
            data->BattleOrder = f.AllocateList<int>();
            var battleOrder = f.ResolveList(data->BattleOrder);
            for (int i = 0; i < champions.Length; i++)
            {
                battleOrder.Add(i);
            }

            Log.Info($"✅ Player {playerRef} (Team {teamId}) created with champions: [{champions[0]}, {champions[1]}, {champions[2]}]");
        }
    }
}
