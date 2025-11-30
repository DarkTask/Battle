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

        [Header("Player A Slots (Card_Red 0~2 -> Character_Mask -> Character)")]
        public Image[] playerASlotImages = new Image[3];

        [Header("Player B Slots (Card_Blue 0~2 -> Character_Mask -> Character)")]
        public Image[] playerBSlotImages = new Image[3];

        [Header("UI Text")]
        public TextMeshProUGUI turnText;  // GameText
        public TextMeshProUGUI timerText; // (없으면 null)

        [Header("Battle Order UI")]
        [Tooltip("BattleOrderUI 인스턴스 또는 프리팹 (Resources/Prefabs/UI/BattleOrderUI)")]
        public BattleOrderUI battleOrderUI;

        [Header("Result UI")]
        public GameObject resultPanel;
        public TextMeshProUGUI resultText;
        public TextMeshProUGUI scoreText;

        [Header("Status")]
        public bool isInitialized = false;

        private QuantumGame _game;
        private int _lastTurn = -1;

        // 각 플레이어의 선택 카운트 추적
        private int _playerASelectCount = 0;
        private int _playerBSelectCount = 0;

        // 각 플레이어가 선택한 챔피언 ID 저장
        private int[] _playerASelectedChampions = new int[3] { -1, -1, -1 };
        private int[] _playerBSelectedChampions = new int[3] { -1, -1, -1 };

        /// <summary>
        /// Player A가 선택한 챔피언 ID 배열 반환
        /// </summary>
        public int[] GetPlayerASelectedChampions() => _playerASelectedChampions;

        /// <summary>
        /// Player B가 선택한 챔피언 ID 배열 반환
        /// </summary>
        public int[] GetPlayerBSelectedChampions() => _playerBSelectedChampions;

        void Start()
        {
            // Result UI 초기 비활성화
            HideResultUI();

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

            // BattleOrderUI 자동 로드 (없으면)
            LoadBattleOrderUI();

            // UI 초기화
            InitializeCharacterElements();

            // Quantum Event 구독
            QuantumEvent.Subscribe<EventChampionSelectedEvent>(this, OnChampionSelectedEvent);
            QuantumEvent.Subscribe<EventTurnChangedEvent>(this, OnTurnChangedEvent);
            QuantumEvent.Subscribe<EventPhaseChangedEvent>(this, OnPhaseChangedEvent);
            QuantumEvent.Subscribe<EventBattleEndEvent>(this, OnBattleEndEvent);

            // Quantum Input Callback 등록
            QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));

            isInitialized = true;
            Debug.Log("✅ CharacterSelectUIController 초기화 완료 (Input Callback 등록됨)");
        }

        void OnDestroy()
        {
            // Event 및 Callback 구독 해제
            QuantumEvent.UnsubscribeListener(this);
            QuantumCallback.UnsubscribeListener(this);
        }

        /// <summary>
        /// BattleOrderUI 자동 로드
        /// </summary>
        void LoadBattleOrderUI()
        {
            // 이미 할당되어 있으면 스킵
            if (battleOrderUI != null)
            {
                Debug.Log("✅ BattleOrderUI 이미 연결됨");
                return;
            }

            // 씬에서 찾기
            battleOrderUI = FindObjectOfType<BattleOrderUI>();
            if (battleOrderUI != null)
            {
                Debug.Log("✅ BattleOrderUI 씬에서 찾음");
                SetupBattleOrderUI();
                return;
            }

            // Resources에서 로드
            GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/BattleOrderUI");
            if (prefab != null)
            {
                // Root Canvas 찾기 (최상위 Canvas)
                Canvas rootCanvas = null;
                Canvas[] allCanvases = FindObjectsOfType<Canvas>();
                foreach (var c in allCanvases)
                {
                    // Root Canvas = 부모가 없거나 부모에 Canvas가 없는 Canvas
                    if (c.isRootCanvas)
                    {
                        rootCanvas = c;
                        break;
                    }
                }

                if (rootCanvas != null)
                {
                    // Root Canvas 직접 자식으로 생성 (최상위 레벨)
                    GameObject instance = Instantiate(prefab, rootCanvas.transform);

                    // 가장 위에 렌더링되도록 sibling index 조정
                    instance.transform.SetAsLastSibling();

                    // 프리팹에 별도의 Canvas가 있으면 제거 (Root Canvas 사용)
                    Canvas childCanvas = instance.GetComponent<Canvas>();
                    if (childCanvas != null)
                    {
                        // Canvas 제거하면 CanvasScaler, GraphicRaycaster도 제거해야 함
                        var scaler = instance.GetComponent<CanvasScaler>();
                        var raycaster = instance.GetComponent<GraphicRaycaster>();
                        if (scaler != null) Destroy(scaler);
                        if (raycaster != null) Destroy(raycaster);
                        Destroy(childCanvas);
                        Debug.Log("🔧 BattleOrderUI의 중복 Canvas 제거됨");
                    }

                    battleOrderUI = instance.GetComponent<BattleOrderUI>();
                    if (battleOrderUI != null)
                    {
                        SetupBattleOrderUI();
                        Debug.Log($"✅ BattleOrderUI 프리팹에서 인스턴스화 완료 (Root Canvas: {rootCanvas.name})");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ Root Canvas를 찾을 수 없어 BattleOrderUI를 인스턴스화할 수 없습니다!");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Resources/Prefabs/UI/BattleOrderUI 프리팹을 찾을 수 없습니다!");
            }
        }

        /// <summary>
        /// BattleOrderUI 초기 설정
        /// </summary>
        void SetupBattleOrderUI()
        {
            if (battleOrderUI == null) return;

            // ChampionDB 연결
            if (battleOrderUI.championDB == null)
            {
                battleOrderUI.championDB = championDB;
            }

            // CharacterSelectPanel 연결
            if (battleOrderUI.characterSelectPanel == null)
            {
                battleOrderUI.characterSelectPanel = characterSelectPanel;
            }

            Debug.Log("✅ BattleOrderUI 설정 완료");
        }

        /// <summary>
        /// 전투 종료 이벤트 콜백
        /// </summary>
        void OnBattleEndEvent(EventBattleEndEvent e)
        {
            Debug.Log($"🏆 BattleEndEvent: Winner=Team{e.WinnerTeam}, Score={e.PlayerAScore}-{e.PlayerBScore}");

            // 결과 UI 표시
            ShowResultUI(e.WinnerTeam, e.PlayerAScore, e.PlayerBScore);
        }

        /// <summary>
        /// 결과 UI 표시
        /// </summary>
        void ShowResultUI(int winnerTeam, int playerAScore, int playerBScore)
        {
            if (resultPanel == null)
            {
                Debug.LogWarning("⚠️ ResultPanel이 할당되지 않았습니다!");
                return;
            }

            // 결과 패널 활성화
            resultPanel.SetActive(true);

            // 승자 텍스트 설정
            if (resultText != null)
            {
                if (winnerTeam == -1)
                {
                    // 무승부
                    resultText.text = "<color=#FFCC00>무승부!</color>";
                }
                else
                {
                    string winnerName = winnerTeam == 0 ? "Player A" : "Player B";
                    string winnerColor = winnerTeam == 0 ? "#FF4444" : "#4444FF";
                    resultText.text = $"<color={winnerColor}>{winnerName}</color> 승리!";
                }
            }

            // 점수 텍스트 설정
            if (scoreText != null)
            {
                scoreText.text = $"<color=#FF4444>A: {playerAScore}</color> - <color=#4444FF>B: {playerBScore}</color>";
            }

            Debug.Log($"🎉 결과 UI 표시됨: {(winnerTeam == -1 ? "무승부" : $"Team{winnerTeam} 승리")}!");
        }

        /// <summary>
        /// 결과 UI 숨기기
        /// </summary>
        void HideResultUI()
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Phase 변경 이벤트 콜백
        /// </summary>
        void OnPhaseChangedEvent(EventPhaseChangedEvent e)
        {
            Debug.Log($"🎮 [CharacterSelectUI] PhaseChangedEvent: NewPhase={e.NewPhase}");

            if (e.NewPhase == (int)GamePhaseSystem.Phase.CharacterSelect)
            {
                // 캐릭터 선택 화면으로 복귀 시 UI 초기화
                ResetCharacterSelectUI();
                characterSelectPanel?.SetActive(true);
                HideResultUI();  // 결과 UI 숨기기
                Debug.Log("🔄 CharacterSelect UI 복원됨");
            }
            else if (e.NewPhase == (int)GamePhaseSystem.Phase.OrderSetup)
            {
                // CharacterSelect UI의 내부 컨텐츠만 숨기기 (Choose_Character 패널)
                HideCharacterSelectContent();

                // BattleOrderUI가 없으면 로드
                if (battleOrderUI == null)
                {
                    LoadBattleOrderUI();
                }
            }
            else if (e.NewPhase == (int)GamePhaseSystem.Phase.Battle)
            {
                // Battle Phase로 전환 시 전체 UI 숨기기
                characterSelectPanel?.SetActive(false);
            }
        }

        /// <summary>
        /// 캐릭터 선택 UI 초기화 (재대결용)
        /// </summary>
        void ResetCharacterSelectUI()
        {
            // 선택 카운트 초기화
            _playerASelectCount = 0;
            _playerBSelectCount = 0;
            _lastTurn = -1;

            // 선택한 챔피언 ID 초기화
            for (int i = 0; i < 3; i++)
            {
                _playerASelectedChampions[i] = -1;
                _playerBSelectedChampions[i] = -1;
            }

            // 플레이어 슬롯 이미지 초기화
            ResetPlayerSlots(playerASlotImages);
            ResetPlayerSlots(playerBSlotImages);

            // 캐릭터 카드 선택 상태 초기화
            foreach (var element in characterElements)
            {
                if (element != null)
                {
                    element.ResetSelectedState();
                }
            }

            Debug.Log("🔄 CharacterSelect UI 상태 초기화 완료");
        }

        /// <summary>
        /// 플레이어 슬롯 이미지 초기화
        /// </summary>
        void ResetPlayerSlots(Image[] slotImages)
        {
            if (slotImages == null) return;

            foreach (var slot in slotImages)
            {
                if (slot != null)
                {
                    slot.sprite = null;
                    slot.color = new Color(1f, 1f, 1f, 0f);  // 투명하게
                }
            }
        }

        /// <summary>
        /// CharacterSelect 관련 UI 숨기기 (Panels 전체)
        /// BattleOrderUI는 별도의 오브젝트이므로 영향 없음
        /// </summary>
        void HideCharacterSelectContent()
        {
            if (characterSelectPanel == null) return;

            // Panels 전체 숨기기 (BattleOrderUI는 별도 오브젝트)
            characterSelectPanel.SetActive(false);
            Debug.Log("🚪 CharacterSelect 패널(Panels) 숨김");
        }

        /// <summary>
        /// 챔피언 선택 이벤트 콜백
        /// </summary>
        void OnChampionSelectedEvent(EventChampionSelectedEvent e)
        {
            // e.PlayerIndex 사용 (Quantum에서 직접 계산된 값)
            int playerIndex = e.PlayerIndex;  // 0 = PlayerA, 1 = PlayerB
            int championId = e.ChampionId;
            int turn = e.Turn;

            Debug.Log($"🎯 ChampionSelectedEvent: Turn={turn}, PlayerIndex={playerIndex}, ChampionId={championId}");

            // 선택된 카드 비활성화
            if (championId >= 0 && championId < characterElements.Length)
            {
                var element = characterElements[championId];
                if (element != null)
                {
                    element.SetSelectedState(true, playerIndex);
                }
            }

            // 플레이어 패널 슬롯 이미지 업데이트
            UpdatePlayerSlot(playerIndex, championId);
        }

        /// <summary>
        /// 플레이어 슬롯에 선택된 챔피언 이미지 표시
        /// </summary>
        void UpdatePlayerSlot(int playerIndex, int championId)
        {
            Debug.Log($"🔧 UpdatePlayerSlot 호출: playerIndex={playerIndex}, championId={championId}");

            // 챔피언 데이터 가져오기
            if (championDB == null)
            {
                Debug.LogWarning("⚠️ ChampionDatabase가 없습니다!");
                return;
            }

            ChampionData champion = championDB.GetChampion(championId);
            if (champion == null)
            {
                Debug.LogWarning($"⚠️ Champion {championId}를 찾을 수 없습니다!");
                return;
            }

            Debug.Log($"🔧 Champion 찾음: {champion.championName}, characterImage={(champion.characterImage != null ? "있음" : "없음")}");

            // 플레이어별 슬롯 배열과 카운트 선택
            Image[] slotImages;
            int slotIndex;

            if (playerIndex == 0)
            {
                // Player A (Panel_Left)
                slotImages = playerASlotImages;
                slotIndex = _playerASelectCount;
                // 선택한 챔피언 ID 저장
                if (slotIndex < 3) _playerASelectedChampions[slotIndex] = championId;
                _playerASelectCount++;
                Debug.Log($"🔧 Player A: slotImages.Length={slotImages?.Length ?? 0}, slotIndex={slotIndex}");
            }
            else
            {
                // Player B (Panel_Right)
                slotImages = playerBSlotImages;
                slotIndex = _playerBSelectCount;
                // 선택한 챔피언 ID 저장
                if (slotIndex < 3) _playerBSelectedChampions[slotIndex] = championId;
                _playerBSelectCount++;
                Debug.Log($"🔧 Player B: slotImages.Length={slotImages?.Length ?? 0}, slotIndex={slotIndex}");
            }

            // 슬롯 범위 체크
            if (slotImages == null || slotImages.Length == 0)
            {
                Debug.LogWarning($"⚠️ Player {(playerIndex == 0 ? "A" : "B")} slotImages 배열이 비어있습니다! Inspector에서 연결하세요.");
                return;
            }

            if (slotIndex >= 3 || slotIndex >= slotImages.Length)
            {
                Debug.LogWarning($"⚠️ Player {(playerIndex == 0 ? "A" : "B")} 슬롯이 가득 찼습니다!");
                return;
            }

            // 슬롯 이미지 설정
            Image slotImage = slotImages[slotIndex];
            if (slotImage != null)
            {
                slotImage.sprite = champion.characterImage;
                slotImage.enabled = true;
                // 알파값을 255로 설정하여 이미지가 보이도록 함
                slotImage.color = new Color(slotImage.color.r, slotImage.color.g, slotImage.color.b, 1f);
                Debug.Log($"📌 Player {(playerIndex == 0 ? "A" : "B")} Slot {slotIndex}: {champion.championName} - 이미지 설정 완료!");
            }
            else
            {
                Debug.LogWarning($"⚠️ Player {(playerIndex == 0 ? "A" : "B")} Slot {slotIndex} Image가 null입니다! Inspector에서 연결하세요.");
            }
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
            UpdateTurnDisplay(turn, currentPlayer);
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

            // CharacterSelect Phase가 아니면 업데이트 스킵 (UI는 숨기지 않음)
            // NOTE: characterSelectPanel을 직접 숨기면 OrderPanel까지 같이 숨겨질 수 있음
            // UI 숨기기는 각 패널에서 개별적으로 처리
            if (globals->CurrentPhase != (int)GamePhaseSystem.Phase.CharacterSelect)
            {
                return;
            }

            // 턴 업데이트
            if (globals->SelectTurn != _lastTurn)
            {
                _lastTurn = globals->SelectTurn;
                // 홀수 턴 = Player A (0), 짝수 턴 = Player B (1)
                int currentPlayer = (globals->SelectTurn - 1) % 2;
                UpdateTurnDisplay(globals->SelectTurn, currentPlayer);
            }

            // 타이머 업데이트
            UpdateTimerDisplay(globals->SelectTimer);
        }

        /// <summary>
        /// 턴 표시 업데이트
        /// </summary>
        void UpdateTurnDisplay(int turn, int currentPlayer)
        {
            if (turnText == null)
                return;

            // currentPlayer: 0 = Player A, 1 = Player B
            string playerName = currentPlayer == 0 ? "Player A" : "Player B";

            turnText.text = $"{playerName}의 차례 ({turn}/6)";

            // 색상 변경
            turnText.color = currentPlayer == 0 ? Color.red : Color.blue;

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
            Debug.Log($"🖱️ Champion {championIndex} clicked - setting pending selection");

            // 선택을 보류하고, PollInput에서 전달
            _pendingChampionSelection = championIndex + 1;  // 0은 "선택 없음"이므로 +1
        }

        // Pending 선택 (0 = 선택 없음, 1~8 = championId 0~7)
        private int _pendingChampionSelection = 0;

        /// <summary>
        /// Quantum Input Callback - 매 프레임 호출되어 Input 전달
        /// CharacterSelect와 OrderSetup 모두 이 콜백에서 처리
        /// </summary>
        public void PollInput(CallbackPollInput callback)
        {
            Quantum.Input input = new Quantum.Input();

            // 1. 캐릭터 선택 Input (CharacterSelect Phase)
            if (_pendingChampionSelection > 0)
            {
                input.SelectedChampionId = _pendingChampionSelection;
                Debug.Log($"📤 Sending Input: SelectedChampionId={_pendingChampionSelection}");
                _pendingChampionSelection = 0;  // 한 번만 전달
            }

            // 2. 전투 순서 Input (OrderSetup Phase)
            if (battleOrderUI != null && battleOrderUI.IsOrderInputPending())
            {
                int[] order = battleOrderUI.GetCurrentOrder();
                input.OrderSlot1 = order[0];
                input.OrderSlot2 = order[1];
                input.OrderSlot3 = order[2];
                input.OrderSubmit = true;

                Debug.Log($"📤 Sending Order Input: [{order[0]}, {order[1]}, {order[2]}]");
                battleOrderUI.ClearOrderInputPending();  // 한 번만 전달
            }

            callback.SetInput(input, DeterministicInputFlags.Repeatable);
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
