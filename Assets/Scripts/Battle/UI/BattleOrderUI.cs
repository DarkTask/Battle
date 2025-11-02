using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 전투 순서 지정 UI
/// 각 플레이어가 선택한 3개 캐릭터의 출전 순서를 지정
/// 상대방에게 순서는 비공개
/// </summary>
public class BattleOrderUI : MonoBehaviour
{
    public static BattleOrderUI Instance;
    
    [Header("UI References")]
    public GameObject orderPanel;                       // 전투 순서 지정 패널
    public TextMeshProUGUI instructionText;             // 안내 텍스트
    public Button confirmButton;                        // 확인 버튼
    
    [Header("Selected Champions Display")]
    public BattleOrderSlot[] selectedChampionSlots = new BattleOrderSlot[3];  // 선택된 챔피언 (드래그 소스)
    
    [Header("Battle Order Slots")]
    public BattleOrderSlot[] battleOrderSlots = new BattleOrderSlot[3];       // 1번, 2번, 3번 슬롯
    
    [Header("Colors")]
    public Color normalSlotColor = Color.white;
    public Color highlightSlotColor = Color.yellow;
    public Color completeSlotColor = Color.green;
    
    private int localPlayerIndex = -1;
    private int[] currentOrder = new int[3] { -1, -1, -1 };
    private bool orderConfirmed = false;
    
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
    }
    
    /// <summary>
    /// 전투 순서 지정 UI 표시
    /// </summary>
    public void ShowOrderSetupUI(int playerIndex)
    {
        localPlayerIndex = playerIndex;
        orderConfirmed = false;
        currentOrder = new int[3] { -1, -1, -1 };
        
        orderPanel?.SetActive(true);
        
        if (instructionText != null)
        {
            instructionText.text = "Assign your champions to slots 1, 2, 3";
        }
        
        InitializeSlots();
        UpdateConfirmButton();
        
        Debug.Log($"🎯 전투 순서 지정 UI 활성화 (Player {(playerIndex == 0 ? "A" : "B")})");
    }
    
    /// <summary>
    /// 슬롯 초기화 - 선택한 챔피언 표시
    /// </summary>
    void InitializeSlots()
    {
        var matchController = Mirror.Examples.MultipleMatch.MatchController.Instance;
        if (matchController == null)
        {
            Debug.LogError("MatchController.Instance를 찾을 수 없습니다!");
            return;
        }
        
        // DicCardElement에서 선택된 카드 가져오기
        if (!matchController.DicCardElement.ContainsKey(localPlayerIndex))
        {
            Debug.LogError($"DicCardElement에 playerIndex={localPlayerIndex} 데이터가 없습니다!");
            return;
        }
        
        var selectedCards = matchController.DicCardElement[localPlayerIndex];
        var setupCards = new System.Collections.Generic.List<CardElement>();
        
        // isSetup이 true인 카드만 가져오기
        foreach (var card in selectedCards)
        {
            if (card.isSetup)
                setupCards.Add(card);
        }
        
        if (setupCards.Count != 3)
        {
            Debug.LogError($"선택된 카드가 3개가 아닙니다! (현재 {setupCards.Count}개)");
            return;
        }
        
        // 선택한 챔피언 표시 (드래그 소스)
        for (int i = 0; i < selectedChampionSlots.Length && i < setupCards.Count; i++)
        {
            if (selectedChampionSlots[i] != null)
            {
                selectedChampionSlots[i].SetChampionFromCard(setupCards[i], i);
                int championIndex = i;
                selectedChampionSlots[i].SetClickCallback(() => OnChampionSlotClicked(championIndex));
            }
        }
        
        // 전투 순서 슬롯 초기화 (빈 상태)
        for (int i = 0; i < battleOrderSlots.Length; i++)
        {
            if (battleOrderSlots[i] != null)
            {
                battleOrderSlots[i].Clear();
                int slotIndex = i;
                battleOrderSlots[i].SetClickCallback(() => OnBattleOrderSlotClicked(slotIndex));
            }
        }
        
        Debug.Log($"✅ BattleOrderUI 슬롯 초기화 완료: {setupCards.Count}개 챔피언");
    }
    
    /// <summary>
    /// 선택한 챔피언 슬롯 클릭 (간단한 클릭 방식)
    /// </summary>
    void OnChampionSlotClicked(int championIndex)
    {
        if (orderConfirmed) return;
        
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
        
        // 이미 배치된 챔피언인지 확인
        for (int i = 0; i < currentOrder.Length; i++)
        {
            if (currentOrder[i] == championIndex)
            {
                Debug.Log($"⚠️ 해당 챔피언은 이미 {i + 1}번 슬롯에 배치되었습니다.");
                return;
            }
        }
        
        // MatchController에서 선택된 카드 가져오기
        var matchController = Mirror.Examples.MultipleMatch.MatchController.Instance;
        if (matchController == null || !matchController.DicCardElement.ContainsKey(localPlayerIndex))
        {
            Debug.LogError("MatchController 데이터를 찾을 수 없습니다!");
            return;
        }
        
        var setupCards = new System.Collections.Generic.List<CardElement>();
        foreach (var card in matchController.DicCardElement[localPlayerIndex])
        {
            if (card.isSetup)
                setupCards.Add(card);
        }
        
        if (championIndex >= setupCards.Count)
        {
            Debug.LogError($"championIndex={championIndex}가 범위를 벗어났습니다!");
            return;
        }
        
        // 배치
        CardElement selectedCard = setupCards[championIndex];
        currentOrder[slotIndex] = championIndex;
        battleOrderSlots[slotIndex].SetChampionFromCard(selectedCard, championIndex);
        
        Debug.Log($"✅ {selectedCard.name.text} → {slotIndex + 1}번 슬롯 배치");
        
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
            instructionText.text = "All slots filled! Click Confirm button.";
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
        confirmButton.interactable = false;
        
        if (instructionText != null)
        {
            instructionText.text = "Order sent! Waiting for opponent...";
            instructionText.color = Color.cyan;
        }
        
        Debug.Log($"📤 전투 순서 전송: [{currentOrder[0]}, {currentOrder[1]}, {currentOrder[2]}]");
        
        // MatchController로 순서 전송 (비공개)
        if (Mirror.Examples.MultipleMatch.MatchController.Instance != null)
        {
            Mirror.Examples.MultipleMatch.MatchController.Instance.CmdSubmitBattleOrder(currentOrder[0], currentOrder[1], currentOrder[2]);
        }
    }
    
    /// <summary>
    /// UI 숨기기
    /// </summary>
    public void HideOrderSetupUI()
    {
        orderPanel?.SetActive(false);
        Debug.Log("🚪 전투 순서 지정 UI 비활성화");
    }
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
    public Button button;
    
    private System.Action clickCallback;
    
    /// <summary>
    /// CardElement에서 챔피언 정보 가져와서 설정
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
    
    public void SetChampion(ChampionData champion, int index)
    {
        if (championIcon != null && champion.icon != null)
        {
            championIcon.sprite = champion.icon;
            championIcon.enabled = true;
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

