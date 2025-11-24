using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Quantum;

/// <summary>
/// 전투 순서 지정 UI (Quantum 호환)
/// 각 플레이어가 선택한 3개 캐릭터의 출전 순서를 지정
/// 상대방에게 순서는 비공개
/// </summary>
public class BattleOrderUI : MonoBehaviour
{
    public static BattleOrderUI Instance;

    [Header("References")]
    public ChampionDatabase championDB;

    [Header("UI References")]
    public GameObject orderPanel;                       // 전투 순서 지정 패널
    public GameObject characterSelectPanel;             // 캐릭터 선택 패널 (숨기기용)
    public TextMeshProUGUI instructionText;             // 안내 텍스트
    public UnityEngine.UI.Button confirmButton;         // 확인 버튼

    [Header("Selected Champions Display")]
    public BattleOrderSlot[] selectedChampionSlots = new BattleOrderSlot[3];  // 선택된 챔피언 (드래그 소스)

    [Header("Battle Order Slots")]
    public BattleOrderSlot[] battleOrderSlots = new BattleOrderSlot[3];       // 1번, 2번, 3번 슬롯

    [Header("Colors")]
    public Color normalSlotColor = Color.white;
    public Color highlightSlotColor = Color.yellow;
    public Color completeSlotColor = Color.green;

    [Header("Status")]
    public bool isInitialized = false;

    private QuantumGame _game;
    private int[] _selectedChampionIds = new int[3];  // 선택된 챔피언 ID
    private int[] currentOrder = new int[3] { -1, -1, -1 };
    private bool orderConfirmed = false;
    private bool _orderInputPending = false;  // Input 전송 대기 플래그

    /// <summary>
    /// Order Input이 대기 중인지 확인 (CharacterSelectUIController에서 호출)
    /// </summary>
    public bool IsOrderInputPending() => _orderInputPending;

    /// <summary>
    /// 현재 배치된 순서 반환 (CharacterSelectUIController에서 호출)
    /// </summary>
    public int[] GetCurrentOrder() => currentOrder;

    /// <summary>
    /// Order Input 전송 완료 처리 (CharacterSelectUIController에서 호출)
    /// </summary>
    public void ClearOrderInputPending()
    {
        _orderInputPending = false;
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            confirmButton.interactable = false;
        }

        orderPanel?.SetActive(false);

        // Quantum 초기화
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

        // Quantum Event 구독
        QuantumEvent.Subscribe<EventPhaseChangedEvent>(this, OnPhaseChangedEvent);

        // NOTE: Input Callback은 CharacterSelectUIController에서 통합 관리
        // BattleOrderUI에서 별도로 등록하면 CharacterSelect의 Input을 덮어씀

        isInitialized = true;
        Debug.Log("✅ BattleOrderUI 초기화 완료 (Quantum 연동)");
    }

    void OnDestroy()
    {
        QuantumEvent.UnsubscribeListener(this);
        QuantumCallback.UnsubscribeListener(this);
    }

    /// <summary>
    /// Phase 변경 이벤트 콜백
    /// </summary>
    void OnPhaseChangedEvent(EventPhaseChangedEvent e)
    {
        Debug.Log($"🎮 [BattleOrderUI] PhaseChangedEvent: NewPhase={e.NewPhase}");

        if (e.NewPhase == (int)GamePhaseSystem.Phase.OrderSetup)
        {
            ShowOrderSetupUI();
        }
        else if (e.NewPhase == (int)GamePhaseSystem.Phase.Battle)
        {
            HideOrderSetupUI();
        }
    }

    /// <summary>
    /// 전투 순서 지정 UI 표시 (Quantum용)
    /// </summary>
    public void ShowOrderSetupUI()
    {
        orderConfirmed = false;
        currentOrder = new int[3] { -1, -1, -1 };

        // NOTE: characterSelectPanel은 CharacterSelectUIController.UpdateUI()에서 Phase 체크로 자동 숨김
        // 여기서 직접 숨기면 OrderPanel까지 같이 숨겨질 수 있음 (부모-자식 관계일 경우)
        // characterSelectPanel?.SetActive(false);

        orderPanel?.SetActive(true);

        // OrderPanel RectTransform 크기 조정 (프리팹이 100x100으로 설정되어 있을 수 있음)
        if (orderPanel != null)
        {
            RectTransform rt = orderPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                // 전체 화면으로 늘리기
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                Debug.Log($"🎯 OrderPanel RectTransform 조정: 전체 화면");
            }
        }

        Debug.Log($"🎯 OrderPanel 활성화: {orderPanel?.name}, active={orderPanel?.activeSelf}");

        if (instructionText != null)
        {
            instructionText.text = "출전 순서를 지정하세요 (1번, 2번, 3번)";
            instructionText.color = normalSlotColor;
        }

        // 선택된 챔피언 로드
        LoadSelectedChampions();

        // 버튼 콜백 설정
        InitializeSlots();

        UpdateConfirmButton();

        Debug.Log("🎯 전투 순서 지정 UI 활성화 (Quantum)");
    }

    /// <summary>
    /// CharacterSelectUIController에서 선택된 챔피언 정보 가져오기
    /// </summary>
    void LoadSelectedChampions()
    {
        // CharacterSelectUIController에서 선택된 챔피언 ID 가져오기
        var charSelectUI = FindObjectOfType<QuantumUser.CharacterSelectUIController>();
        if (charSelectUI != null)
        {
            int[] playerAChampions = charSelectUI.GetPlayerASelectedChampions();
            if (playerAChampions != null && playerAChampions.Length >= 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    _selectedChampionIds[i] = playerAChampions[i];

                    // 슬롯 UI 업데이트
                    if (i < selectedChampionSlots.Length && selectedChampionSlots[i] != null)
                    {
                        var champion = championDB?.GetChampion(playerAChampions[i]);
                        if (champion != null)
                        {
                            selectedChampionSlots[i].SetChampion(champion, i);
                        }
                    }
                }
                Debug.Log($"✅ 선택된 챔피언 로드: [{_selectedChampionIds[0]}, {_selectedChampionIds[1]}, {_selectedChampionIds[2]}]");
                return;
            }
        }

        Debug.LogWarning("⚠️ CharacterSelectUIController에서 선택된 챔피언을 가져올 수 없습니다!");
    }

    /// <summary>
    /// 슬롯 초기화 - 버튼 콜백 설정
    /// </summary>
    void InitializeSlots()
    {
        // 선택한 챔피언 슬롯에 클릭 콜백 설정
        for (int i = 0; i < selectedChampionSlots.Length; i++)
        {
            if (selectedChampionSlots[i] != null)
            {
                int championIndex = i;
                selectedChampionSlots[i].SetClickCallback(() => OnChampionSlotClicked(championIndex));
            }
        }

        // 전투 순서 슬롯 초기화 (빈 상태) 및 클릭 콜백 설정
        for (int i = 0; i < battleOrderSlots.Length; i++)
        {
            if (battleOrderSlots[i] != null)
            {
                battleOrderSlots[i].Clear();
                int slotIndex = i;
                battleOrderSlots[i].SetClickCallback(() => OnBattleOrderSlotClicked(slotIndex));
            }
        }

        Debug.Log($"✅ BattleOrderUI 슬롯 초기화 완료");
    }

    /// <summary>
    /// 선택한 챔피언 슬롯 클릭 (간단한 클릭 방식)
    /// </summary>
    void OnChampionSlotClicked(int championIndex)
    {
        if (orderConfirmed) return;

        // 이미 배치된 챔피언인지 확인
        for (int i = 0; i < currentOrder.Length; i++)
        {
            if (currentOrder[i] == championIndex)
            {
                Debug.Log($"⚠️ 해당 챔피언은 이미 {i + 1}번 슬롯에 배치되었습니다.");
                return;
            }
        }

        // 빈 전투 순서 슬롯 찾기
        for (int i = 0; i < battleOrderSlots.Length; i++)
        {
            if (currentOrder[i] == -1)
            {
                AssignChampionToSlot(championIndex, i);
                return;
            }
        }

        Debug.Log("⚠️ 모든 슬롯이 이미 배치되었습니다. 슬롯을 클릭해서 제거하세요.");
    }

    /// <summary>
    /// 전투 순서 슬롯 클릭 - 배치된 챔피언 제거
    /// </summary>
    void OnBattleOrderSlotClicked(int slotIndex)
    {
        if (orderConfirmed) return;

        if (currentOrder[slotIndex] != -1)
        {
            // 배치 제거
            currentOrder[slotIndex] = -1;
            battleOrderSlots[slotIndex].Clear();
            UpdateConfirmButton();
            Debug.Log($"❌ 슬롯 {slotIndex + 1} 배치 제거");
        }
    }

    /// <summary>
    /// 챔피언을 전투 순서 슬롯에 배치
    /// </summary>
    void AssignChampionToSlot(int championIndex, int slotIndex)
    {
        if (championIndex < 0 || championIndex >= 3) return;
        if (slotIndex < 0 || slotIndex >= 3) return;

        // 배치
        int championId = _selectedChampionIds[championIndex];
        var champion = championDB?.GetChampion(championId);

        if (champion != null)
        {
            currentOrder[slotIndex] = championIndex;
            battleOrderSlots[slotIndex].SetChampion(champion, championIndex);

            Debug.Log($"✅ {champion.championName} → {slotIndex + 1}번 슬롯 배치");
        }

        UpdateConfirmButton();
    }

    /// <summary>
    /// 확인 버튼 상태 업데이트
    /// </summary>
    void UpdateConfirmButton()
    {
        if (confirmButton == null) return;

        // 모든 슬롯이 배치되었는지 확인
        bool allSlotsFilled = currentOrder[0] != -1 && currentOrder[1] != -1 && currentOrder[2] != -1;
        confirmButton.interactable = allSlotsFilled && !orderConfirmed;

        if (allSlotsFilled && instructionText != null)
        {
            instructionText.text = "순서 지정 완료! 확인 버튼을 누르세요.";
            instructionText.color = completeSlotColor;
        }
    }

    /// <summary>
    /// 확인 버튼 클릭
    /// </summary>
    void OnConfirmButtonClicked()
    {
        if (orderConfirmed) return;

        // 순서 확인
        bool isValid = currentOrder[0] != -1 && currentOrder[1] != -1 && currentOrder[2] != -1;
        if (!isValid)
        {
            Debug.LogWarning("전투 순서가 완전하지 않습니다!");
            return;
        }

        orderConfirmed = true;
        _orderInputPending = true;  // Input 전송 대기
        confirmButton.interactable = false;

        if (instructionText != null)
        {
            instructionText.text = "순서 전송 완료! 상대방 대기 중...";
            instructionText.color = Color.cyan;
        }

        Debug.Log($"📤 전투 순서 전송: [{currentOrder[0]}, {currentOrder[1]}, {currentOrder[2]}]");
    }

    /// <summary>
    /// UI 숨기기
    /// </summary>
    public void HideOrderSetupUI()
    {
        orderPanel?.SetActive(false);
        Debug.Log("🚪 전투 순서 지정 UI 비활성화");
    }

    #region Legacy Mirror Support (Deprecated)
    // 이전 Mirror 방식 호환용 - 사용 안함
    public void ShowOrderSetupUI(int playerIndex)
    {
        ShowOrderSetupUI();
    }
    #endregion
}

/// <summary>
/// 전투 순서 슬롯 (개별 슬롯)
/// </summary>
[System.Serializable]
public class BattleOrderSlot
{
    public GameObject slotObject;
    public Image championIcon;
    public TextMeshProUGUI championName;
    public UnityEngine.UI.Button button;

    private System.Action clickCallback;

    /// <summary>
    /// CardElement에서 챔피언 정보 가져와서 설정 (Mirror 호환용)
    /// </summary>
    public void SetChampionFromCard(CardElement card, int index)
    {
        if (card == null)
        {
            Debug.LogWarning("CardElement가 null입니다!");
            return;
        }

        if (championIcon != null && card.image != null && card.image.sprite != null)
        {
            championIcon.sprite = card.image.sprite;
            championIcon.enabled = true;
            championIcon.color = UnityEngine.Color.white;
        }

        if (championName != null && card.name != null)
        {
            championName.text = card.name.text;
        }
    }

    /// <summary>
    /// ChampionData에서 챔피언 정보 설정 (Quantum용)
    /// </summary>
    public void SetChampion(ChampionData champion, int index)
    {
        if (champion == null)
        {
            Debug.LogWarning("ChampionData가 null입니다!");
            return;
        }

        if (championIcon != null)
        {
            // characterImage 또는 icon 사용
            championIcon.sprite = champion.characterImage ?? champion.icon;
            championIcon.enabled = championIcon.sprite != null;
            championIcon.color = UnityEngine.Color.white;
        }

        if (championName != null)
        {
            championName.text = champion.championName;
        }
    }

    public void Clear()
    {
        if (championIcon != null)
        {
            championIcon.sprite = null;
            championIcon.enabled = false;
        }

        if (championName != null)
        {
            championName.text = "?";
        }
    }

    public void SetClickCallback(System.Action callback)
    {
        clickCallback = callback;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => clickCallback?.Invoke());
        }
    }
}
