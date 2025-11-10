namespace Quantum
{
    using Photon.Deterministic;

    /// <summary>
    /// 전투 시스템 (라운드 관리, 스폰, 승패 판정)
    /// </summary>
    public unsafe class BattleSystem : SystemMainThread, ISignalOnPhaseChanged
    {
        public void OnPhaseChanged(Frame f, int newPhase)
        {
            if (newPhase == (int)GamePhaseSystem.Phase.Battle)
            {
                // 전투 시작 시 첫 라운드 스폰
                StartRound(f, 1);
            }
        }

        public override void Update(Frame f)
        {
            var globals = f.Globals;

            if (globals->CurrentPhase != (int)GamePhaseSystem.Phase.Battle)
                return;

            // 전투 타이머 감소
            globals->BattleTimer -= f.DeltaTime;

            // 타임아웃 또는 승부 결정 확인
            if (globals->BattleTimer <= FP._0 || IsRoundOver(f))
            {
                EndRound(f, globals);
            }
        }

        /// <summary>
        /// 라운드 시작
        /// </summary>
        void StartRound(Frame f, int round)
        {
            var globals = f.Globals;
            globals->CurrentRound = round;
            globals->BattleTimer = FP.FromFloat_UNSAFE(999);  // 999초 (테스트용 - 라운드가 자동으로 끝나지 않음)

            Log.Info($"⚔️ Round {round} Start!");
            f.Signals.OnBattleStart(round);

            // 챔피언 스폰
            if (round <= 3)
            {
                // 1v1 라운드
                SpawnChampionsForRound(f, round);
            }
            else if (round == 4)
            {
                // 3v3 단체전
                SpawnAllChampions(f);
            }
        }

        /// <summary>
        /// 라운드 종료
        /// </summary>
        void EndRound(Frame f, _globals_* globals)
        {
            int round = globals->CurrentRound;
            int winnerTeam = DetermineRoundWinner(f);

            Log.Info($"⚔️ Round {round} End! Winner: Team {winnerTeam}");

            // 점수 추가
            if (winnerTeam == 0)
                globals->PlayerAScore++;
            else if (winnerTeam == 1)
                globals->PlayerBScore++;

            // 모든 전투 엔티티 제거
            DestroyAllBattleEntities(f);

            // 다음 라운드 또는 게임 종료
            if (round < 4)
            {
                StartRound(f, round + 1);
            }
            else
            {
                // 게임 종료
                int finalWinner = globals->PlayerAScore > globals->PlayerBScore ? 0 : 1;
                Log.Info($"🏆 Game Over! Final Winner: Team {finalWinner}");
                f.Signals.OnBattleEnd(finalWinner);

                GamePhaseSystem.ChangePhase(f, globals, GamePhaseSystem.Phase.Result);
            }
        }

        /// <summary>
        /// 1v1 라운드용 챔피언 스폰
        /// </summary>
        void SpawnChampionsForRound(Frame f, int round)
        {
            // round 1~3: 각 플레이어의 전투 순서에 따라 1명씩 스폰
            int slotIndex = round - 1;  // 0, 1, 2

            var filter = f.Filter<PlayerGameData>();
            while (filter.NextUnsafe(out var entity, out var data))
            {
                var battleOrder = f.ResolveList(data->BattleOrder);
                var selectedChampions = f.ResolveList(data->SelectedChampions);

                // 디버그 로그
                Log.Info($"🐛 Round {round}: Player={data->PlayerRef}, TeamId={data->TeamId}");
                Log.Info($"   BattleOrder Count={battleOrder.Count}, SelectedChampions Count={selectedChampions.Count}");
                Log.Info($"   slotIndex={slotIndex}, BattleOrder[{slotIndex}]={battleOrder[slotIndex]}");

                int championIndex = battleOrder[slotIndex];

                // 경계 체크
                if (championIndex < 0 || championIndex >= selectedChampions.Count)
                {
                    Log.Error($"❌ Invalid championIndex={championIndex}, SelectedChampions.Count={selectedChampions.Count}");
                    continue;
                }

                int championId = selectedChampions[championIndex];
                int teamId = data->TeamId;

                SpawnChampion(f, championId, teamId, slotIndex);
            }
        }

        /// <summary>
        /// 3v3 단체전용 챔피언 스폰
        /// </summary>
        void SpawnAllChampions(Frame f)
        {
            var filter = f.Filter<PlayerGameData>();
            while (filter.NextUnsafe(out var entity, out var data))
            {
                int teamId = data->TeamId;
                var battleOrder = f.ResolveList(data->BattleOrder);
                var selectedChampions = f.ResolveList(data->SelectedChampions);

                for (int i = 0; i < 3; i++)
                {
                    // 경계 체크
                    if (i >= battleOrder.Count)
                    {
                        Log.Error($"❌ BattleOrder index out of range: i={i}, Count={battleOrder.Count}");
                        continue;
                    }

                    int championIndex = battleOrder[i];

                    if (championIndex < 0 || championIndex >= selectedChampions.Count)
                    {
                        Log.Error($"❌ Invalid championIndex={championIndex}, SelectedChampions.Count={selectedChampions.Count}");
                        continue;
                    }

                    int championId = selectedChampions[championIndex];

                    SpawnChampion(f, championId, teamId, i);
                }
            }
        }

        /// <summary>
        /// 챔피언 Entity 생성
        /// </summary>
        void SpawnChampion(Frame f, int championId, int teamId, int slotIndex)
        {
            var config = f.FindAsset<BattleGameConfig>(f.RuntimeConfig.GameConfig.Id);
            var prototype = config.GetChampionPrototype(championId);

            if (prototype.Id.IsValid == false)
            {
                Log.Error($"Invalid champion prototype: {championId}");
                return;
            }

            // Entity 생성
            var entity = f.Create(prototype);

            // Transform 설정
            var spawnPos = config.GetSpawnPosition(teamId, slotIndex);
            if (f.Unsafe.TryGetPointer<Transform3D>(entity, out var transform))
            {
                transform->Position = spawnPos;
                transform->Rotation = FPQuaternion.Identity;
            }

            // ChampionStats 컴포넌트 추가 및 설정
            f.Add<ChampionStats>(entity);
            ChampionStats* stats = f.Unsafe.GetPointer<ChampionStats>(entity);
            stats->ChampionId = championId;
            stats->Strength = FP._1;
            stats->Dexterity = FP._1;
            stats->Constitution = FP._1;
            stats->MaxHealth = FP._10;  // 임시값: 10 HP
            stats->AttackPower = FP._2;  // 2 데미지
            stats->AttackSpeed = FP._1;  // 1초당 1회 공격
            stats->MoveSpeed = FP._3;    // 이동속도 3

            // BattleState 컴포넌트 추가 및 설정
            f.Add<BattleState>(entity);
            BattleState* battleState = f.Unsafe.GetPointer<BattleState>(entity);
            battleState->TeamId = teamId;
            battleState->SlotIndex = slotIndex;
            battleState->IsAlive = true;
            battleState->AttackCooldown = FP._0;
            battleState->CurrentTarget = EntityRef.None;
            battleState->Health = stats->MaxHealth;

            // SimpleAI 컴포넌트 추가
            f.Add<SimpleAI>(entity);
            SimpleAI* ai = f.Unsafe.GetPointer<SimpleAI>(entity);
            ai->SearchRadius = FP.FromFloat_UNSAFE(20);  // 20 유닛 반경 내 적 탐색
            ai->AttackRange = FP._2;    // 2 유닛 거리에서 공격
            ai->ThinkTimer = FP._0;

            // NavMeshSteeringAgent 설정 (이동 속도)
            if (f.Unsafe.TryGetPointer<NavMeshSteeringAgent>(entity, out var steeringAgent))
            {
                steeringAgent->MaxSpeed = stats->MoveSpeed;  // ChampionStats의 MoveSpeed 사용
                steeringAgent->Acceleration = FP._10;  // 가속도
            }

            // SimpleAISystem.Filter 컴포넌트 확인
            bool hasSimpleAI = f.Has<SimpleAI>(entity);
            bool hasBattleState = f.Has<BattleState>(entity);
            bool hasTransform = f.Has<Transform3D>(entity);
            bool hasPathfinder = f.Has<NavMeshPathfinder>(entity);

            Log.Info($"✅ Champion spawned: ChampionId={championId}, Team={teamId}, Slot={slotIndex}, Entity={entity}, Health={battleState->Health}");
            Log.Info($"   Components: SimpleAI={hasSimpleAI}, BattleState={hasBattleState}, Transform3D={hasTransform}, NavMeshPathfinder={hasPathfinder}");
        }

        /// <summary>
        /// 라운드 종료 확인
        /// </summary>
        bool IsRoundOver(Frame f)
        {
            int teamAAlive = 0;
            int teamBAlive = 0;

            var filter = f.Filter<BattleState>();
            while (filter.NextUnsafe(out var entity, out var state))
            {
                if (state->IsAlive)
                {
                    if (state->TeamId == 0)
                        teamAAlive++;
                    else
                        teamBAlive++;
                }
            }

            // 한쪽이 전멸하면 라운드 종료
            return teamAAlive == 0 || teamBAlive == 0;
        }

        /// <summary>
        /// 라운드 승자 결정
        /// </summary>
        int DetermineRoundWinner(Frame f)
        {
            int teamAAlive = 0;
            int teamBAlive = 0;

            var filter = f.Filter<BattleState>();
            while (filter.NextUnsafe(out var entity, out var state))
            {
                if (state->IsAlive)
                {
                    if (state->TeamId == 0)
                        teamAAlive++;
                    else
                        teamBAlive++;
                }
            }

            if (teamAAlive > teamBAlive)
                return 0;  // Team A 승리
            else if (teamBAlive > teamAAlive)
                return 1;  // Team B 승리
            else
                return -1;  // 무승부 (드로우)
        }

        /// <summary>
        /// 모든 전투 엔티티 제거
        /// </summary>
        void DestroyAllBattleEntities(Frame f)
        {
            var filter = f.Filter<BattleState>();
            while (filter.NextUnsafe(out var entity, out var state))
            {
                f.Destroy(entity);
            }
        }
    }
}
