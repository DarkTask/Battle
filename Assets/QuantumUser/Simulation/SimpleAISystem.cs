using Photon.Deterministic;

namespace Quantum
{
    public unsafe class SimpleAISystem : SystemMainThreadFilter<SimpleAISystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public SimpleAI* AI;
            public BattleState* BattleState;
            public Transform3D* Transform;
            public NavMeshPathfinder* Pathfinder;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            // 디버그: IsAlive 체크
            if (!filter.BattleState->IsAlive)
            {
                // Log.Info($"❌ {filter.Entity} is dead, skipping AI");
                return;
            }

            // ThinkTimer는 타겟 찾기에만 사용 (0.5초마다)
            filter.AI->ThinkTimer -= f.DeltaTime;
            if (filter.AI->ThinkTimer <= FP._0)
            {
                // Log.Info($"🧠 {filter.Entity} thinking... (ThinkTimer expired)");
                filter.AI->ThinkTimer = FP._0_50;

                // 타겟이 없거나 죽었으면 새로운 타겟 찾기
                if (filter.BattleState->CurrentTarget == EntityRef.None ||
                    !f.Exists(filter.BattleState->CurrentTarget) ||
                    !IsAlive(f, filter.BattleState->CurrentTarget))
                {
                    FindNewTarget(f, ref filter);
                }
            }

            // 전투 처리는 매 프레임 실행 (AttackCooldown이 자체적으로 관리됨)
            if (filter.BattleState->CurrentTarget != EntityRef.None)
            {
                ProcessCombat(f, ref filter);
            }
        }

        void FindNewTarget(Frame f, ref Filter filter)
        {
            EntityRef closestEnemy = EntityRef.None;
            FP closestDistance = filter.AI->SearchRadius;

            // Log.Info($"🔍 {filter.Entity} searching for enemies... (MyTeam={filter.BattleState->TeamId}, SearchRadius={filter.AI->SearchRadius})");

            var enemyFilter = f.Filter<BattleState, Transform3D>();
            // int totalEnemies = 0;
            // int sameTeam = 0;
            // int deadEnemies = 0;
            // int tooFar = 0;

            while (enemyFilter.NextUnsafe(out var enemyEntity, out var enemyState, out var enemyTransform))
            {
                // totalEnemies++;
                // Log.Info($"   Found entity: {enemyEntity}, Team={enemyState->TeamId}, IsAlive={enemyState->IsAlive}");

                if (enemyState->TeamId == filter.BattleState->TeamId)
                {
                    // sameTeam++;
                    continue;
                }

                if (!enemyState->IsAlive)
                {
                    // deadEnemies++;
                    continue;
                }

                FP distance = FPVector3.Distance(filter.Transform->Position, enemyTransform->Position);
                // Log.Info($"   Distance to {enemyEntity}: {distance} (SearchRadius: {filter.AI->SearchRadius})");

                if (distance <= closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemyEntity;
                }
                // else
                // {
                //     tooFar++;
                // }
            }

            // Log.Info($"   Search results: Total={totalEnemies}, SameTeam={sameTeam}, Dead={deadEnemies}, TooFar={tooFar}");

            filter.BattleState->CurrentTarget = closestEnemy;

            if (closestEnemy != EntityRef.None)
            {
                Log.Info($"🎯 {filter.Entity} found target: {closestEnemy} (distance: {closestDistance})");
            }
            // else
            // {
            //     Log.Info($"❌ {filter.Entity} found NO target!");
            // }
        }

        void ProcessCombat(Frame f, ref Filter filter)
        {
            var target = filter.BattleState->CurrentTarget;

            if (!f.TryGet<Transform3D>(target, out var targetTransform))
            {
                // Log.Info($"❌ ProcessCombat: Target {target} has no Transform3D!");
                return;
            }

            FP distance = FPVector3.Distance(filter.Transform->Position, targetTransform.Position);
            // Log.Info($"📏 {filter.Entity} ProcessCombat: Distance={distance}, AttackRange={filter.AI->AttackRange}, Cooldown={filter.BattleState->AttackCooldown}");

            if (distance <= filter.AI->AttackRange)
            {
                // Log.Info($"💥 {filter.Entity} IN ATTACK RANGE!");
                if (filter.BattleState->AttackCooldown <= FP._0)
                {
                    // Log.Info($"⚡ {filter.Entity} Cooldown expired, ATTACKING!");
                    AttackTarget(f, filter.Entity, target);

                    if (f.Unsafe.TryGetPointer<ChampionStats>(filter.Entity, out var stats))
                    {
                        filter.BattleState->AttackCooldown = FP._1 / stats->AttackSpeed;
                        // Log.Info($"⏱️ {filter.Entity} Set cooldown to {filter.BattleState->AttackCooldown} (1 / AttackSpeed={stats->AttackSpeed})");
                    }
                    else
                    {
                        filter.BattleState->AttackCooldown = FP._1;
                        // Log.Info($"⏱️ {filter.Entity} Set cooldown to 1 (no stats)");
                    }
                }
                else
                {
                    // Log.Info($"⏳ {filter.Entity} Waiting for cooldown... ({filter.BattleState->AttackCooldown} remaining)");
                    filter.BattleState->AttackCooldown -= f.DeltaTime;
                }
            }
            else
            {
                // Log.Info($"🏃 {filter.Entity} MOVING to target (distance {distance} > range {filter.AI->AttackRange})");
                // 타겟 방향으로 직접 이동 (2D)
                FPVector3 direction = (targetTransform.Position - filter.Transform->Position).Normalized;
                FP moveSpeed = FP._3;  // 이동 속도 (10 -> 3으로 감소)
                FPVector3 newPosition = filter.Transform->Position + direction * moveSpeed * f.DeltaTime;

                // 2D 모드: Y축을 0으로 고정
                newPosition.Y = FP._0;

                filter.Transform->Position = newPosition;
            }
        }

        void AttackTarget(Frame f, EntityRef attacker, EntityRef target)
        {
            if (!f.Unsafe.TryGetPointer<ChampionStats>(attacker, out var attackerStats))
                return;

            if (!f.Unsafe.TryGetPointer<BattleState>(target, out var targetState))
                return;

            FP attackDamage = attackerStats->AttackPower;
            targetState->Health -= attackDamage;

            Log.Info($"⚔️ {attacker} attacks {target} for {attackDamage} damage! (HP: {targetState->Health})");

            if (targetState->Health <= FP._0)
            {
                targetState->IsAlive = false;
                Log.Info($"💀 {target} died!");

                f.Signals.OnChampionDeath(target);

                if (f.Unsafe.TryGetPointer<BattleState>(attacker, out var attackerState))
                {
                    attackerState->CurrentTarget = EntityRef.None;
                }
            }
        }

        bool IsAlive(Frame f, EntityRef entity)
        {
            if (!f.TryGet<BattleState>(entity, out var state))
                return false;

            return state.IsAlive;
        }
    }
}
