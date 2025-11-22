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

            // Player A, B 데이터 생성 (빈 상태로 - CharacterSelect에서 선택)
            CreateEmptyPlayerData(f, 0, 0);  // Player A, Team 0
            CreateEmptyPlayerData(f, 1, 1);  // Player B, Team 1

            // CharacterSelect 단계로 시작
            f.Global->CurrentPhase = (int)GamePhaseSystem.Phase.CharacterSelect;
            f.Global->SelectTurn = 1;  // 턴 1부터 시작 (Player A)
            f.Global->SelectTimer = FP.FromFloat_UNSAFE(10f);  // 10초
            f.Global->CurrentRound = 0;
            f.Global->BattleTimer = FP._0;

            // 첫 번째 턴 이벤트 발생 (UI 초기화용)
            f.Events.TurnChangedEvent(1, 0);  // Turn=1, CurrentPlayer=0 (Player A)

            Log.Info("✅ [TestInitSystem] Initialization complete! CharacterSelect will start...");
        }

        /// <summary>
        /// CharacterSelect용 빈 Player 데이터 생성
        /// </summary>
        void CreateEmptyPlayerData(Frame f, int playerRef, int teamId)
        {
            var entity = f.Create();
            f.Add<PlayerGameData>(entity);

            var data = f.Unsafe.GetPointer<PlayerGameData>(entity);
            data->PlayerRef = playerRef;
            data->TeamId = teamId;
            data->SelectedCount = 0;
            data->OrderSubmitted = false;

            // 빈 리스트 생성
            data->SelectedChampions = f.AllocateList<int>();
            data->BattleOrder = f.AllocateList<int>();

            Log.Info($"✅ Player {playerRef} (Team {teamId}) created (empty - waiting for selection)");
        }

        /// <summary>
        /// Battle 테스트용 - 미리 챔피언이 선택된 Player 데이터 생성
        /// </summary>
        void CreatePlayerDataWithChampions(Frame f, int playerRef, int teamId, int[] champions)
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
