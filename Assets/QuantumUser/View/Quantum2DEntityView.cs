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

            // TeamId 가져오기
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

                // 달리기 애니메이션 제어
                if (_animator != null)
                {
                    string direction = (_teamId == 0) ? "SouthEast" : "NorthWest";
                    string moveParam = "Move" + direction;

                    _animator.SetBool(moveParam, isMoving);
                    _animator.SetBool("isRunning", isMoving);
                }
            }

            // 공격 애니메이션 체크
            if (_animator != null && frame.TryGet<BattleState>(EntityView.EntityRef, out var battleState))
            {
                // AttackCooldown이 막 리셋되었을 때 = 공격이 방금 시작됨
                if (_lastAttackCooldown <= FP._0 && battleState.AttackCooldown > FP._0)
                {
                    PlayAttackAnimation();
                }

                _lastAttackCooldown = battleState.AttackCooldown;
            }
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

            // Team 0 (북서쪽 → 남동쪽): SouthEast
            // Team 1 (남동쪽 → 북서쪽): NorthWest
            string direction = (_teamId == 0) ? "SouthEast" : "NorthWest";
            string isDirection = (_teamId == 0) ? "isSouthEast" : "isNorthWest";
            string attackParam = "AttackAttack" + direction;

            _activeCoroutineCount++;

            // 코루틴으로 공격 애니메이션 재생
            _currentResetCoroutine = StartCoroutine(PlayAttackAnimationCoroutine(isDirection, attackParam));
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

        private System.Collections.IEnumerator PlayAttackAnimationCoroutine(string isDirection, string attackParam)
        {
            if (_animator == null) yield break;

            // Animator.Play()로 직접 애니메이션 상태 재생
            // 상태 이름 = 파라미터 이름과 동일 (예: "AttackAttackSouthEast")
            string stateName = attackParam;

            // 레이어 0에서 해당 상태를 0초(즉시)부터 재생, normalizedTime=0으로 처음부터 시작
            _animator.Play(stateName, 0, 0f);

            // 1.5초 후 Idle로 전환 (또는 파라미터 리셋)
            yield return new WaitForSeconds(1.5f);

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
