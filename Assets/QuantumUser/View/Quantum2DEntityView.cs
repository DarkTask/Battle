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
    /// - Team 0 (왼쪽): 오른쪽을 바라봄 (FlipX = false)
    /// - Team 1 (오른쪽): 왼쪽을 바라봄 (FlipX = true)
    /// - 공격 시 애니메이션 재생 (AttackAttackEast/West)
    /// </summary>
    public class Quantum2DEntityView : QuantumEntityViewComponent
    {
        private QuantumGame _game;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;
        private int _teamId = -1;
        private FP _lastAttackCooldown = FP._0;

        public override void OnActivate(Frame frame)
        {
            _game = QuantumRunner.Default?.Game;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();

            Debug.Log($"🔧 OnActivate: Animator={(_animator != null ? "Found" : "NULL")}, SpriteRenderer={(_spriteRenderer != null ? "Found" : "NULL")}");

            // TeamId 가져오기
            if (frame.TryGet<BattleState>(EntityView.EntityRef, out var battleState))
            {
                _teamId = battleState.TeamId;

                // 팀에 따라 초기 방향 설정
                if (_spriteRenderer != null)
                {
                    // Team 0 (왼쪽): 오른쪽 보기 (FlipX = false)
                    // Team 1 (오른쪽): 왼쪽 보기 (FlipX = true)
                    _spriteRenderer.flipX = (_teamId == 1);

                    Debug.Log($"💡 Sprite Flip set: TeamId={_teamId}, FlipX={_spriteRenderer.flipX}");
                }
            }

            if (_animator != null)
            {
                Debug.Log($"   🎮 Animator has {_animator.parameters.Length} parameters");
                // Animator 파라미터 확인 (처음 5개만 출력)
                int count = 0;
                foreach (var param in _animator.parameters)
                {
                    if (param.name.Contains("Attack") && count < 5)
                    {
                        Debug.Log($"   Animator param: {param.name} (type: {param.type})");
                        count++;
                    }
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

                // Quantum Rotation은 무시 (2D는 회전 없음 또는 Z축 회전만 사용)
            }

            // 공격 애니메이션 체크
            if (_animator != null && frame.TryGet<BattleState>(EntityView.EntityRef, out var battleState))
            {
                // 디버그: 쿨다운 변화 추적 (주석 처리 - 로그가 너무 많음)
                // if (_lastAttackCooldown != battleState.AttackCooldown)
                // {
                //     Debug.Log($"🔍 Cooldown changed: {_lastAttackCooldown.AsFloat:F2} -> {battleState.AttackCooldown.AsFloat:F2} (Entity={EntityView.EntityRef})");
                // }

                // AttackCooldown이 막 리셋되었을 때 = 공격이 방금 시작됨
                if (_lastAttackCooldown <= FP._0 && battleState.AttackCooldown > FP._0)
                {
                    Debug.Log($"🎯 Attack detected! Cooldown: 0 -> {battleState.AttackCooldown.AsFloat:F2}");
                    PlayAttackAnimation();
                }

                _lastAttackCooldown = battleState.AttackCooldown;
            }
        }

        private void PlayAttackAnimation()
        {
            Debug.Log($"🚀 PlayAttackAnimation CALLED! Animator={(_animator != null)}, TeamId={_teamId}");

            if (_animator == null)
            {
                Debug.LogWarning($"❌ Animator is NULL! Cannot play attack animation.");
                return;
            }

            // Animator 상태 확인
            bool hasController = _animator.runtimeAnimatorController != null;
            string controllerName = hasController ? _animator.runtimeAnimatorController.name : "NULL";
            Debug.Log($"🎬 Animator: enabled={_animator.enabled}, controller={controllerName}, gameObject={gameObject.name}");

            // Team 0 (왼쪽 → 오른쪽 공격): AttackAttackEast
            // Team 1 (오른쪽 → 왼쪽 공격): AttackAttackWest
            string attackTrigger = (_teamId == 0) ? "AttackAttackEast" : "AttackAttackWest";
            Debug.Log($"   Looking for parameter: {attackTrigger}");

            // 파라미터 존재 여부 확인
            bool parameterExists = false;
            foreach (var param in _animator.parameters)
            {
                if (param.name == attackTrigger)
                {
                    parameterExists = true;
                    Debug.Log($"   ✅ Found parameter: {attackTrigger} (type: {param.type})");
                    break;
                }
            }

            if (!parameterExists)
            {
                Debug.LogError($"❌ Animator parameter '{attackTrigger}' NOT FOUND!");
                return;
            }

            // 현재 Animator 상태 확인
            if (_animator.layerCount > 0)
            {
                var currentState = _animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"   Current state: {currentState.fullPathHash}, normalizedTime: {currentState.normalizedTime}");
            }

            // 공격 애니메이션이 재생되려면 다른 상태들을 먼저 false로 설정해야 함
            // 이동 관련 Bool 파라미터들을 모두 false로 설정
            string[] movementParams = { "isWalking", "isRunning", "isRunningBackwards",
                                       "isStrafingLeft", "isStrafingRight", "isIdle" };
            foreach (string param in movementParams)
            {
                if (HasParameter(_animator, param))
                {
                    _animator.SetBool(param, false);
                }
            }

            // Bool 파라미터이므로 SetBool 사용 (Trigger가 아님!)
            _animator.SetBool(attackTrigger, true);
            Debug.Log($"⚔️ SetBool({attackTrigger}, true) EXECUTED!");

            // Animator 강제 업데이트
            _animator.Update(0f);

            // 업데이트 후 상태 확인
            if (_animator.layerCount > 0)
            {
                var afterState = _animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"   After SetBool: {afterState.fullPathHash}, normalizedTime: {afterState.normalizedTime}");
            }

            // 0.5초 후 자동으로 false로 리셋 (애니메이션이 끝나도록)
            StartCoroutine(ResetAttackBool(attackTrigger));
        }

        private System.Collections.IEnumerator ResetAttackBool(string parameterName)
        {
            yield return new WaitForSeconds(0.5f);
            if (_animator != null)
            {
                _animator.SetBool(parameterName, false);
            }
        }

        private bool HasParameter(Animator animator, string parameterName)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == parameterName)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
