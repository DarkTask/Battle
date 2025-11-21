using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Quantum;
using Photon.Deterministic;

namespace QuantumUser
{
    /// <summary>
    /// Quantum CharacterSelect Phase의 Unity UI 컨트롤러
    ///
    /// 역할:
    /// - Quantum의 CharacterSelectSystem 상태를 UI에 반영
    /// - 플레이어 입력을 Quantum에 전달
    /// - 8개 챔피언 카드 표시 (CharacterElement 0~7)
    /// - 선택된 챔피언을 좌우 패널에 표시
    ///
    /// 구조:
    /// - ScrollRect/Content/CharacterElement 0~7: 8개 캐릭터 카드
    /// - Panels/Choose_Character/Panel_Left: Player A 선택 슬롯 (빨간색)
    /// - Panels/Choose_Character/Panel_Right: Player B 선택 슬롯 (파란색)
    /// - Panels/GameText: 턴 표시
    /// </summary>
    public class CharacterSelectUIController : MonoBehaviour
    {
        [Header("References")]
        public ChampionDatabase championDB;

        [Header("UI Panels")]
        public GameObject characterSelectPanel;

        [Header("Character Elements (8개)")]
        public CharacterElement[] characterElements = new CharacterElement[8];

        [Header("Player Panels")]
        public Transform playerAPanel;  // Panel_Left
        public Transform playerBPanel;  // Panel_Right

        [Header("UI Text")]
        public TextMeshProUGUI turnText;  // GameText
        public TextMeshProUGUI timerText; // (없으면 null)

        [Header("Status")]
        public bool isInitialized = false;

        private QuantumGame _game;
        private int _lastTurn = -1;

        void Start()
        {
            // Quantum 준비될 때까지 대기
            StartCoroutine(WaitForQuantumAndInitialize());
        }

        System.Collections.IEnumerator WaitForQuantumAndInitialize()
        {
            // QuantumRunner 대기
            while (QuantumRunner.Default == null || QuantumRunner.Default.Game == null)
            {
                yield return null;
            }

            _game = QuantumRunner.Default.Game;

            // ChampionDatabase 확인
            if (championDB == null)
            {
                Debug.LogError("❌ ChampionDatabase가 할당되지 않았습니다!");
                yield break;
            }

            // UI 초기화
            InitializeCharacterElements();

            // Quantum Event 구독
            QuantumEvent.Subscribe<EventChampionSelectedEvent>(this, OnChampionSelectedEvent);
            QuantumEvent.Subscribe<EventTurnChangedEvent>(this, OnTurnChangedEvent);

            isInitialized = true;
            Debug.Log("✅ CharacterSelectUIController 초기화 완료");
        }

        void OnDestroy()
        {
            // Event 구독 해제
            QuantumEvent.UnsubscribeListener(this);
        }

        /// <summary>
        /// 챔피언 선택 이벤트 콜백
        /// </summary>
        void OnChampionSelectedEvent(EventChampionSelectedEvent e)
        {
            int playerIndex = e.Player._index;  // 0 = PlayerA, 1 = PlayerB
            int championId = e.ChampionId;
            int turn = e.Turn;

            Debug.Log($"🎯 ChampionSelectedEvent: Player={playerIndex}, ChampionId={championId}, Turn={turn}");

            // 선택된 카드 비활성화
            if (championId >= 0 && championId < characterElements.Length)
            {
                var element = characterElements[championId];
                if (element != null)
                {
                    element.SetSelectedState(true, playerIndex);
                }
            }

            // 플레이어 패널 업데이트
            // TODO: 선택된 챔피언을 좌우 패널에 표시
        }

        /// <summary>
        /// 턴 변경 이벤트 콜백
        /// </summary>
        void OnTurnChangedEvent(EventTurnChangedEvent e)
        {
            int turn = e.Turn;
            int currentPlayer = e.CurrentPlayer;

            Debug.Log($"🔄 TurnChangedEvent: Turn={turn}, CurrentPlayer={currentPlayer}");

            _lastTurn = turn;
            UpdateTurnDisplay(turn);
        }

        void Update()
        {
            if (!isInitialized || _game == null)
                return;

            UpdateUI();
        }

        /// <summary>
        /// 8개 CharacterElement 초기화
        /// Inspector에서 직접 연결된 characterElements 배열 사용
        /// </summary>
        void InitializeCharacterElements()
        {
            int championCount = Mathf.Min(characterElements.Length, championDB.GetChampionCount());

            for (int i = 0; i < championCount; i++)
            {
                if (characterElements[i] == null)
                {
                    Debug.LogWarning($"⚠️ CharacterElement[{i}]가 할당되지 않았습니다!");
                    continue;
                }

                CharacterElement element = characterElements[i];
                ChampionData data = championDB.GetChampion(i);

                if (data != null)
                {
                    element.InitializeWithChampionData(data, i);

                    // 클릭 이벤트 연결
                    int championIndex = i;
                    UnityEngine.UI.Button btn = element.GetComponent<UnityEngine.UI.Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnChampionClicked(championIndex));
                    }
                }
                else
                {
                    Debug.LogWarning($"⚠️ ChampionData[{i}]가 null입니다!");
                }
            }

            Debug.Log($"✅ {championCount}개 챔피언 카드 초기화 완료");
        }

        /// <summary>
        /// UI 업데이트 (매 프레임)
        /// </summary>
        unsafe void UpdateUI()
        {
            var frame = _game.Frames.Predicted;
            if (frame == null)
                return;

            // Quantum의 Global 데이터에 포인터로 접근
            var globals = frame.Globals;

            // CharacterSelect Phase가 아니면 UI 숨김
            if (globals->CurrentPhase != (int)GamePhaseSystem.Phase.CharacterSelect)
            {
                if (characterSelectPanel != null && characterSelectPanel.activeSelf)
                {
                    characterSelectPanel.SetActive(false);
                }
                return;
            }

            // UI 표시
            if (characterSelectPanel != null && !characterSelectPanel.activeSelf)
            {
                characterSelectPanel.SetActive(true);
            }

            // 턴 업데이트
            if (globals->SelectTurn != _lastTurn)
            {
                _lastTurn = globals->SelectTurn;
                UpdateTurnDisplay(globals->SelectTurn);
            }

            // 타이머 업데이트
            UpdateTimerDisplay(globals->SelectTimer);
        }

        /// <summary>
        /// 턴 표시 업데이트
        /// </summary>
        void UpdateTurnDisplay(int turn)
        {
            if (turnText == null)
                return;

            // 턴 순서: A → B → B → A → A → B
            string playerName = "";
            switch (turn)
            {
                case 1: playerName = "Player A"; break;
                case 2: playerName = "Player B"; break;
                case 3: playerName = "Player B"; break;
                case 4: playerName = "Player A"; break;
                case 5: playerName = "Player A"; break;
                case 6: playerName = "Player B"; break;
                default: playerName = "???"; break;
            }

            turnText.text = $"{playerName}의 차례 ({turn}/6)";

            // 색상 변경
            bool isPlayerA = (turn == 1 || turn == 4 || turn == 5);
            turnText.color = isPlayerA ? Color.red : Color.blue;

            Debug.Log($"🎯 Turn {turn}: {playerName}");
        }

        /// <summary>
        /// 타이머 표시 업데이트
        /// </summary>
        void UpdateTimerDisplay(FP timer)
        {
            if (timerText == null)
                return;

            float seconds = timer.AsFloat;
            timerText.text = $"{Mathf.CeilToInt(seconds):D2}초";

            // 색상 경고
            if (seconds <= 0.1f)
                timerText.color = Color.red;
            else if (seconds <= 0.2f)
                timerText.color = Color.yellow;
            else
                timerText.color = Color.white;
        }

        /// <summary>
        /// 챔피언 카드 클릭 시 호출
        /// </summary>
        void OnChampionClicked(int championIndex)
        {
            if (_game == null)
            {
                Debug.LogWarning("⚠️ Quantum Game이 준비되지 않았습니다!");
                return;
            }

            Debug.Log($"🖱️ Champion {championIndex} clicked");

            // Quantum Input으로 전달
            // TODO: QuantumInput을 통해 선택 전달
            // 현재는 직접 호출 (테스트용)
            var frame = _game.Frames.Predicted;
            if (frame != null)
            {
                // 로컬 플레이어 찾기 (테스트용으로 PlayerRef 0 사용)
                PlayerRef localPlayer = 0; // TODO: 실제 로컬 플레이어 찾기

                CharacterSelectSystem.SelectChampion(frame, localPlayer, championIndex);
            }
        }

        // TODO: Quantum Signal 구독 추가
        // Quantum 코드 생성 후 EventOnChampionSelected 타입 사용
        // void OnChampionSelectedCallback(EventOnChampionSelected e)
        // {
        //     Debug.Log($"✅ Champion selected signal: Player={e.Player}, ChampionId={e.ChampionId}");
        //     int playerIndex = e.Player;
        //     if (e.ChampionId >= 0 && e.ChampionId < characterElements.Length)
        //     {
        //         if (characterElements[e.ChampionId] != null)
        //         {
        //             characterElements[e.ChampionId].SetSelectedState(true, playerIndex);
        //         }
        //     }
        //     UpdatePlayerPanel(e.Player);
        // }

        /// <summary>
        /// 챔피언 선택 시 UI 업데이트 (수동 호출용)
        /// </summary>
        public void OnChampionSelected(PlayerRef player, int championId)
        {
            Debug.Log($"✅ Champion selected: Player={player}, ChampionId={championId}");

            // UI 업데이트
            int playerIndex = player;

            if (championId >= 0 && championId < characterElements.Length)
            {
                if (characterElements[championId] != null)
                {
                    characterElements[championId].SetSelectedState(true, playerIndex);
                }
            }

            // Side Panel 업데이트
            UpdatePlayerPanel(player);
        }

        /// <summary>
        /// 플레이어 패널 업데이트 (선택된 챔피언 3개 표시)
        /// </summary>
        void UpdatePlayerPanel(PlayerRef player)
        {
            if (_game == null)
                return;

            var frame = _game.Frames.Predicted;
            if (frame == null)
                return;

            // PlayerGameData 찾기 (unsafe 제거)
            // TODO: Quantum API를 통해 안전하게 데이터 접근하도록 개선 필요
            // 현재는 임시로 빈 리스트 사용
            var tempList = new System.Collections.Generic.List<int>();
            UpdatePlayerPanelUI(player, 0, tempList);
        }

        /// <summary>
        /// 플레이어 패널 UI 업데이트
        /// </summary>
        void UpdatePlayerPanelUI(PlayerRef player, int selectedCount, System.Collections.Generic.List<int> selectedChampions)
        {
            Transform panel = (player == 0) ? playerAPanel : playerBPanel;
            if (panel == null)
                return;

            Debug.Log($"📌 Player {player} selected champions: {selectedCount}개");

            // 패널의 슬롯 업데이트 (최대 3개)
            for (int i = 0; i < 3 && i < panel.childCount; i++)
            {
                Transform slot = panel.GetChild(i);
                if (slot == null)
                    continue;

                if (i < selectedCount && i < selectedChampions.Count)
                {
                    int championId = selectedChampions[i];
                    ChampionData champion = championDB.GetChampion(championId);

                    // 아이콘 업데이트
                    Image iconImage = slot.Find("Icon")?.GetComponent<Image>();
                    if (iconImage != null && champion != null && champion.icon != null)
                    {
                        iconImage.sprite = champion.icon;
                        iconImage.enabled = true;
                        iconImage.color = Color.white;
                    }

                    // 이름 업데이트
                    TextMeshProUGUI nameText = slot.Find("Text_Name")?.GetComponent<TextMeshProUGUI>();
                    if (nameText != null && champion != null)
                    {
                        nameText.text = champion.championName;
                    }
                }
                else
                {
                    // 빈 슬롯
                    Image iconImage = slot.Find("Icon")?.GetComponent<Image>();
                    if (iconImage != null)
                    {
                        iconImage.sprite = null;
                        iconImage.enabled = false;
                    }
                }
            }
        }
    }
}
