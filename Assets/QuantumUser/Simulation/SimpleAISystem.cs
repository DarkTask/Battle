namespace Quantum
{
    using Photon.Deterministic;

    /// <summary>
    /// 간단한 AI 전투 시스템
    /// </summary>
    public unsafe class SimpleAISystem : SystemMainThreadFilter<SimpleAISystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public SimpleAI* AI;
            public BattleState* Battle;
            public ChampionStats* Stats;
            public Transform3D* Transform;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            // 죽은 캐릭터는 처리 안 함
            if (!filter.Battle->IsAlive)
                return;

            // Think Timer 감소
            filter.AI->ThinkTimer -= f.DeltaTime;
            if (filter.AI->ThinkTimer > FP._0)
                return;

            filter.AI->ThinkTimer = FP._0_50;  // 0.5초마다 Think

            // 타겟 찾기
            if (filter.Battle->CurrentTarget == EntityRef.None || !IsTargetValid(f, filter.Battle->CurrentTarget))
            {
                filter.Battle->CurrentTarget = FindNearestEnemy(f, filter.Entity, filter.Battle->TeamId, filter.Transform->Position);
            }

            // 타겟 공격
            if (filter.Battle->CurrentTarget != EntityRef.None)
            {
                AttackTarget(f, ref filter);
            }
        }

        /// <summary>
        /// 타겟이 유효한가?
        /// </summary>
        bool IsTargetValid(Frame f, EntityRef target)
        {
            if (!f.Exists(target))
                return false;

            if (f.Unsafe.TryGetPointer<BattleState>(target, out var state))
            {
                return state->IsAlive;
            }

            return false;
        }

        /// <summary>
        /// 가장 가까운 적 찾기
        /// </summary>
        EntityRef FindNearestEnemy(Frame f, EntityRef self, int myTeam, FPVector3 myPos)
        {
            EntityRef nearest = EntityRef.None;
            FP minDistance = FP.MaxValue;

            var filter = f.Filter<BattleState, Transform3D>();
            while (filter.NextUnsafe(out var entity, out var state, out var transform))
            {
                if (entity == self)
                    continue;

                if (state->TeamId == myTeam)
                    continue;

                if (!state->IsAlive)
                    continue;

                FP distance = FPVector3.Distance(myPos, transform->Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = entity;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 타겟 공격
        /// </summary>
        void AttackTarget(Frame f, ref Filter filter)
        {
            // 공격 쿨다운 확인
            if (filter.Battle->AttackCooldown > FP._0)
            {
                filter.Battle->AttackCooldown -= f.DeltaTime;
                return;
            }

            // 타겟 거리 확인
            if (!f.Unsafe.TryGetPointer<Transform3D>(filter.Battle->CurrentTarget, out var targetTransform))
                return;

            FP distance = FPVector3.Distance(filter.Transform->Position, targetTransform->Position);

            if (distance <= filter.AI->AttackRange)
            {
                // 공격!
                if (f.Unsafe.TryGetPointer<BattleState>(filter.Battle->CurrentTarget, out var targetState))
                {
                    FP damage = filter.Stats->AttackPower;
                    targetState->Health -= damage;

                    Log.Info($"⚔️ Attack! {filter.Entity} → {filter.Battle->CurrentTarget}, Damage: {damage}, Remaining HP: {targetState->Health}");

                    // 죽음 처리
                    if (targetState->Health <= FP._0)
                    {
                        targetState->IsAlive = false;
                        Log.Info($"💀 Champion died: {filter.Battle->CurrentTarget}");
                        f.Signals.OnChampionDeath(filter.Battle->CurrentTarget);
                    }

                    // 쿨다운 설정 (AttackSpeed 역수)
                    filter.Battle->AttackCooldown = FP._1 / filter.Stats->AttackSpeed;
                }
            }
            else
            {
                // 타겟에게 이동 (간단한 직선 이동)
                FPVector3 direction = (targetTransform->Position - filter.Transform->Position).Normalized;
                filter.Transform->Position += direction * filter.Stats->MoveSpeed * f.DeltaTime;
            }
        }
    }
}
