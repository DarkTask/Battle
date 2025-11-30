using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace QuantumUser
{
    /// <summary>
    /// Quantum의 3D Transform을 Unity 2D Transform으로 매핑
    /// Quantum XZ -> Unity XY
    ///
    /// 2D 배틀 특화 기능:
    /// - Team 0 (북서쪽): 남동쪽을 바라봄 (SouthEast)
    /// - Team 1 (남동쪽): 북서쪽을 바라봄 (NorthWest)
    /// - 공격 시 애니메이션 재생 (AttackAttackSouthEast/NorthWest)
    /// </summary>
    public class Quantum2DEntityView : QuantumEntityViewComponent
    {
        private QuantumGame _game;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;
        private int _teamId = -1;
        private FP _lastAttackCooldown = FP._0;
        private FPVector3 _lastPosition;
        private int _activeCoroutineCount = 0;
        private UnityEngine.Coroutine _currentResetCoroutine = null;
        private string _currentDirection = "SouthEast";  // 현재 바라보는 방향

        // 발사체 관련
        private bool _isRanged = false;
        private GameObject _projectilePrefab;
        private Vector3 _lastTargetPosition;

        // HP 바
        private HealthBarUI _healthBar;

        // 8방향 이름 배열 (각도 순서)
        private static readonly string[] Directions = {
            "East", "NorthEast", "North", "NorthWest",
            "West", "SouthWest", "South", "SouthEast"
        };

        public override void OnActivate(Frame frame)
        {
            _game = QuantumRunner.Default?.Game;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();

            Debug.Log($"🔧 OnActivate: Animator={(_animator != null ? "Found" : "NULL")}, SpriteRenderer={(_spriteRenderer != null ? "Found" : "NULL")}");

            // AnimationController 비활성화 (이 컴포넌트가 방향을 매 프레임 덮어쓰므로)
            var animController = GetComponent<SmallScaleInc.TopDownPixelCharactersPack1.AnimationController>();
            if (animController != null)
            {
                animController.enabled = false;
                Debug.Log($"🚫 AnimationController disabled");
            }

            // TeamId 가져오기 및 HP 바 생성
            if (frame.TryGet<BattleState>(EntityView.EntityRef, out var battleState))
            {
                _teamId = battleState.TeamId;

                // 팀에 따라 초기 방향 설정
                // FlipX는 사용하지 않음 - 애니메이션 방향(isSouthEast/isNorthWest)으로만 제어

                // Animator 초기 설정 (AnimationController.Start()와 동일)
                if (_animator != null)
                {
                    StartCoroutine(InitializeAnimator());
                }

                // HP 바 생성 - ChampionStats에서 MaxHealth 가져오기
                _healthBar = HealthBarUI.CreateHealthBar(transform);
                float maxHealth = battleState.Health.AsFloat;  // 기본값은 현재 체력

                // ChampionStats에서 실제 MaxHealth 가져오기
                if (frame.TryGet<ChampionStats>(EntityView.EntityRef, out var championStats))
                {
                    maxHealth = championStats.MaxHealth.AsFloat;
                    Debug.Log($"❤️ HealthBar: MaxHP from ChampionStats = {maxHealth}");
                }

                _healthBar.Initialize(transform, maxHealth, _teamId);
                Debug.Log($"❤️ HealthBar created - MaxHP: {maxHealth}, CurrentHP: {battleState.Health.AsFloat}, Team: {_teamId}");

                // HP 바 오브젝트 활성화 확인
                if (_healthBar != null && _healthBar.gameObject != null)
                {
                    Debug.Log($"   HealthBar GameObject active: {_healthBar.gameObject.activeSelf}");
                    Debug.Log($"   HealthBar localPosition: {_healthBar.transform.localPosition}");
                    Debug.Log($"   HealthBar localScale: {_healthBar.transform.localScale}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ BattleState not found - TeamId and HealthBar not initialized!");
            }

            // 발사체 프리팹 가져오기 (PlayerController에서)
            var playerController = GetComponent<SmallScaleInc.TopDownPixelCharactersPack1.PlayerController>();
            if (playerController != null && playerController.projectilePrefab != null)
            {
                _isRanged = true;
                _projectilePrefab = playerController.projectilePrefab;
                Debug.Log($"🏹 Ranged character detected - Projectile: {_projectilePrefab.name}");
            }
        }

        public override void OnUpdateView()
        {
            if (_game == null || EntityView == null || EntityView.EntityRef == EntityRef.None)
                return;

            var frame = _game.Frames.Predicted;
            if (frame == null)
                return;

            // 사망 체크 - 죽은 캐릭터는 비활성화
            if (frame.TryGet<BattleState>(EntityView.EntityRef, out var deathCheck))
            {
                if (!deathCheck.IsAlive)
                {
                    // 이미 비활성화되어 있으면 스킵
                    if (gameObject.activeSelf)
                    {
                        Debug.Log($"💀 Entity {EntityView.EntityRef} died - deactivating view");
                        gameObject.SetActive(false);
                    }
                    return;
                }
            }

            // Transform3D 가져오기 (safe 방식)
            if (frame.TryGet<Transform3D>(EntityView.EntityRef, out var quantumTransform))
            {
                // Quantum XZ -> Unity XY 매핑
                var quantumPos = quantumTransform.Position;
                transform.position = new Vector3(
                    quantumPos.X.AsFloat,  // Quantum X -> Unity X
                    quantumPos.Z.AsFloat,  // Quantum Z -> Unity Y
                    0f                      // Unity Z는 항상 0 (2D)
                );

                // 이동 감지 (위치 변화 체크)
                bool isMoving = FPVector3.Distance(quantumPos, _lastPosition) > FP._0_01;
                _lastPosition = quantumPos;

                // 타겟 방향 계산 및 방향 업데이트
                if (frame.TryGet<BattleState>(EntityView.EntityRef, out var battleState))
                {
                    // HP 바 업데이트
                    if (_healthBar != null)
                    {
                        float currentHP = battleState.Health.AsFloat;

                        // 공격받았는지 디버그 (매 프레임은 너무 많으므로 1초마다)
                        if (Time.frameCount % 60 == 0)
                        {
                            _healthBar.DebugLogHP(currentHP);
                        }

                        _healthBar.SetHealth(currentHP);
                    }

                    // 타겟이 있으면 타겟 방향으로, 없으면 기본 방향 유지
                    if (battleState.CurrentTarget != EntityRef.None &&
                        frame.TryGet<Transform3D>(battleState.CurrentTarget, out var targetTransform))
                    {
                        _currentDirection = GetDirectionTo(quantumPos, targetTransform.Position);
                        // 타겟 위치 저장 (Quantum XZ -> Unity XY)
                        _lastTargetPosition = new Vector3(
                            targetTransform.Position.X.AsFloat,
                            targetTransform.Position.Z.AsFloat,
                            0f
                        );
                    }
                    else if (_currentDirection == null)
                    {
                        // 초기 방향 (타겟 없을 때)
                        _currentDirection = (_teamId == 0) ? "SouthEast" : "NorthWest";
                    }

                    // 방향 Bool 업데이트
                    UpdateDirectionBools(_currentDirection);

                    // 달리기 애니메이션 제어
                    if (_animator != null)
                    {
                        string moveParam = "Move" + _currentDirection;
                        _animator.SetBool(moveParam, isMoving);
                        _animator.SetBool("isRunning", isMoving);
                    }

                    // 공격 애니메이션 체크
                    if (_animator != null)
                    {
                        // AttackCooldown이 막 리셋되었을 때 = 공격이 방금 시작됨
                        if (_lastAttackCooldown <= FP._0 && battleState.AttackCooldown > FP._0)
                        {
                            PlayAttackAnimation();
                        }

                        _lastAttackCooldown = battleState.AttackCooldown;
                    }
                }
            }
        }

        /// <summary>
        /// 두 위치 사이의 방향을 8방향 문자열로 반환
        /// </summary>
        private string GetDirectionTo(FPVector3 from, FPVector3 to)
        {
            // Quantum XZ -> Unity XY 기준으로 방향 계산
            float dx = (to.X - from.X).AsFloat;
            float dy = (to.Z - from.Z).AsFloat;  // Quantum Z = Unity Y

            // 각도 계산 (라디안 -> 도)
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

            // -180 ~ 180을 0 ~ 360으로 변환
            if (angle < 0) angle += 360f;

            // 8방향으로 양자화 (각 방향은 45도 범위)
            // East = 0도, NorthEast = 45도, North = 90도, ...
            int index = Mathf.RoundToInt(angle / 45f) % 8;

            return Directions[index];
        }

        /// <summary>
        /// 방향 Bool 파라미터 업데이트
        /// </summary>
        private void UpdateDirectionBools(string direction)
        {
            if (_animator == null) return;

            // 모든 방향 초기화
            foreach (var dir in Directions)
            {
                _animator.SetBool("is" + dir, false);
            }

            // 현재 방향만 활성화
            _animator.SetBool("is" + direction, true);
        }

        private void PlayAttackAnimation()
        {
            if (_animator == null)
            {
                Debug.LogWarning($"❌ Animator is NULL! Cannot play attack animation.");
                return;
            }

            // 이전 리셋 코루틴이 실행 중이면 중단
            if (_currentResetCoroutine != null)
            {
                StopCoroutine(_currentResetCoroutine);
                _activeCoroutineCount--;
            }

            // 현재 바라보는 방향으로 공격 애니메이션 재생
            string attackParam = "AttackAttack" + _currentDirection;

            _activeCoroutineCount++;

            // 코루틴으로 공격 애니메이션 재생
            _currentResetCoroutine = StartCoroutine(PlayAttackAnimationCoroutine(attackParam));

            // 원거리 캐릭터인 경우 발사체 발사
            if (_isRanged && _projectilePrefab != null)
            {
                StartCoroutine(FireProjectileDelayed());
            }
        }

        /// <summary>
        /// 딜레이 후 발사체 발사 (공격 애니메이션과 동기화)
        /// </summary>
        private System.Collections.IEnumerator FireProjectileDelayed()
        {
            // 공격 애니메이션 시작 후 0.2초 대기 (활시위 당기는 동작)
            yield return new WaitForSeconds(0.2f);

            if (_projectilePrefab == null) yield break;

            // 발사 방향 계산
            Vector3 direction = (_lastTargetPosition - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 발사체 생성
            GameObject projectile = Instantiate(_projectilePrefab, transform.position, Quaternion.Euler(0, 0, angle));

            // Rigidbody2D로 이동
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float projectileSpeed = 10f;
                rb.linearVelocity = direction * projectileSpeed;
            }

            // 3초 후 자동 파괴
            Destroy(projectile, 3f);
        }

        private System.Collections.IEnumerator InitializeAnimator()
        {
            // Animator의 런타임 컨트롤러가 완전히 초기화될 때까지 대기
            yield return new WaitForEndOfFrame();

            string initialDirection = (_teamId == 0) ? "isSouthEast" : "isNorthWest";

            Debug.Log($"🔧 InitializeAnimator START - TeamId={_teamId}, Target={initialDirection}");

            // Animator Controller 정보 확인
            if (_animator.runtimeAnimatorController == null)
            {
                Debug.LogError($"❌ RuntimeAnimatorController is NULL!");
                yield break;
            }

            Debug.Log($"   Controller: {_animator.runtimeAnimatorController.name}");
            Debug.Log($"   Parameter count: {_animator.parameterCount}");

            // 모든 파라미터 출력 (디버깅용 - 주석 처리)
            // for (int i = 0; i < _animator.parameterCount; i++)
            // {
            //     var param = _animator.GetParameter(i);
            //     Debug.Log($"   Param[{i}]: {param.name} (Type: {param.type})");
            // }

            // 최대 10번 시도 (Animator가 준비될 때까지)
            bool success = false;
            for (int attempt = 0; attempt < 10 && !success; attempt++)
            {
                // 모든 방향 Bool을 false로 초기화
                _animator.SetBool("isWest", false);
                _animator.SetBool("isEast", false);
                _animator.SetBool("isSouth", false);
                _animator.SetBool("isSouthWest", false);
                _animator.SetBool("isNorthEast", false);
                _animator.SetBool("isSouthEast", false);
                _animator.SetBool("isNorth", false);
                _animator.SetBool("isNorthWest", false);

                // 이동 관련 Bool 초기화
                _animator.SetBool("isWalking", false);
                _animator.SetBool("isRunning", false);
                _animator.SetBool("isCrouchRunning", false);
                _animator.SetBool("isCrouchIdling", false);
                _animator.SetBool("isRunningBackwards", false);
                _animator.SetBool("isStrafingLeft", false);
                _animator.SetBool("isStrafingRight", false);

                // 초기 방향 설정
                _animator.SetBool(initialDirection, true);

                // Animator 업데이트 대기
                yield return null;
                yield return new WaitForEndOfFrame();

                // 검증
                success = _animator.GetBool(initialDirection);

                if (!success)
                {
                    Debug.LogWarning($"⚠️ Attempt {attempt + 1}/10: {initialDirection} is still False");
                }
            }

            // 최종 결과 로그
            Debug.Log($"🎬 Animator initialized for Team {_teamId} ({(success ? "✅ Success" : "❌ Failed")}):");
            Debug.Log($"   🎯 Target: {initialDirection} = {_animator.GetBool(initialDirection)}");
            Debug.Log($"   Direction Bools: E={_animator.GetBool("isEast")}, W={_animator.GetBool("isWest")}, N={_animator.GetBool("isNorth")}, S={_animator.GetBool("isSouth")}");

            if (!success)
            {
                Debug.LogError($"❌ CRITICAL: Failed to initialize Animator for Team {_teamId} after 10 attempts!");
            }
        }

        private System.Collections.IEnumerator PlayAttackAnimationCoroutine(string attackParam)
        {
            if (_animator == null) yield break;

            // Animator.Play()로 직접 애니메이션 상태 재생
            // 상태 이름 = 파라미터 이름과 동일 (예: "AttackAttackSouthEast")
            string stateName = attackParam;

            // 레이어 0에서 해당 상태를 0초(즉시)부터 재생, normalizedTime=0으로 처음부터 시작
            _animator.Play(stateName, 0, 0f);

            // 0.5초 후 Idle로 전환 (공격 애니메이션이 짧으므로)
            yield return new WaitForSeconds(0.5f);

            // Idle 상태로 전환하기 위해 공격 파라미터 리셋
            if (_animator != null)
            {
                _animator.SetBool(attackParam, false);
                _animator.SetBool("isAttackAttacking", false);
            }

            _activeCoroutineCount--;
            _currentResetCoroutine = null;
        }
    }
}
