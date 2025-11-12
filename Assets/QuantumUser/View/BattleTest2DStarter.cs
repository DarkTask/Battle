using UnityEngine;
using Quantum;

namespace QuantumUser
{
    /// <summary>
    /// 2D 전투 테스트를 위한 간단한 시작 스크립트
    ///
    /// 사용 방법:
    /// 1. BattleTestScene_2D 씬에 이 컴포넌트를 가진 GameObject 추가
    /// 2. Play 버튼 클릭 (autoStart=true인 경우 자동 시작)
    ///
    /// 참고:
    /// - 실제 게임 데이터는 Quantum Simulation 레이어에서 관리
    /// - 이 스크립트는 단순히 게임 시작을 대기하고 로그를 출력하는 역할
    /// - 캐릭터 스폰 및 전투는 BattleSystem과 SimpleAISystem에서 자동 처리
    /// </summary>
    public class BattleTest2DStarter : MonoBehaviour
    {
        [Header("Test Settings")]
        [Tooltip("자동으로 게임 시작 감시 (Play 버튼 누르면 로그 출력)")]
        public bool autoStart = true;

        [Tooltip("시작 지연 시간 (초)")]
        public float startDelay = 1f;

        [Header("Test Info")]
        [Tooltip("Player A Champions: 0=Knight, 1=Paladin, 2=DeathKnight")]
        public int playerAChampion1 = 0;
        public int playerAChampion2 = 1;
        public int playerAChampion3 = 2;

        [Tooltip("Player B Champions: 3=DarkLord, 4=Archer, 5=CamoArcher")]
        public int playerBChampion1 = 3;
        public int playerBChampion2 = 4;
        public int playerBChampion3 = 5;

        [Header("Status")]
        public bool isQuantumReady = false;

        private QuantumGame _game;
        private bool _started = false;

        void Start()
        {
            if (autoStart)
            {
                Invoke(nameof(StartBattleTest), startDelay);
            }
        }

        void Update()
        {
            // Space 키로 수동 시작
            if (!_started && UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                StartBattleTest();
            }
        }

        public void StartBattleTest()
        {
            if (_started)
                return;

            _started = true;
            Debug.Log("🎮 [BattleTest2D] Waiting for Quantum to start...");
            Debug.Log($"   Player A Champions: [{playerAChampion1}, {playerAChampion2}, {playerAChampion3}]");
            Debug.Log($"   Player B Champions: [{playerBChampion1}, {playerBChampion2}, {playerBChampion3}]");

            StartCoroutine(WaitForQuantumAndStart());
        }

        System.Collections.IEnumerator WaitForQuantumAndStart()
        {
            // QuantumRunner가 준비될 때까지 대기
            while (QuantumRunner.Default == null || QuantumRunner.Default.Game == null)
            {
                yield return null;
            }

            _game = QuantumRunner.Default.Game;
            isQuantumReady = true;

            Debug.Log("✅ [BattleTest2D] Quantum is ready!");
            Debug.Log("   Note: BattleSystem will automatically handle champion spawning");
            Debug.Log("   Note: You need to manually create PlayerGameData entities in Quantum");
            Debug.Log("   ");
            Debug.Log("   💡 TIP: Use CharacterSelectSystem or create a custom init system");
            Debug.Log("   💡 TIP: Or test with existing 3D BattleTestScene setup");
        }
    }
}
