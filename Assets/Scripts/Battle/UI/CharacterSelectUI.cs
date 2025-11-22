using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Quantum;
using Photon.Deterministic;

public class CharacterSelectUI : MonoBehaviour
{
    public static CharacterSelectUI Instance;

    [Header("Data")]
    public ChampionDatabase championDB;

    // Quantum 연동
    private QuantumGame _game;
    private int _pendingChampionSelection = 0;  // 0 = 선택 없음, 1~8 = championId 0~7
    private bool _quantumInitialized = false;
    
    [Header("Character Elements (12개)")]
    public CharacterElement[] characterElements = new CharacterElement[12];
    
    [Header("UI Elements")]
    public TextMeshProUGUI gameText;           // "Player A의 차례"
    public TextMeshProUGUI timerText;          // "03초" (선택사항)
    
    [Header("Side Panels")]
    public Transform playerAPanel;             // Panel_Left
    public Transform playerBPanel;             // Panel_Right
    
    [Header("Player Slots")]
    public GameObject[] playerASlots = new GameObject[3];  // Card_Red x3
    public GameObject[] playerBSlots = new GameObject[3];  // Card_Blue x3
    
    [Header("UI Colors")]
    public Color playerAColor = Color.red;
    public Color playerBColor = Color.blue;
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    void Start()
    {
        InitializeChampions();
        StartCoroutine(InitializeQuantum());
    }

    System.Collections.IEnumerator InitializeQuantum()
    {
        // QuantumRunner 대기
        while (QuantumRunner.Default == null || QuantumRunner.Default.Game == null)
        {
            yield return null;
        }

        _game = QuantumRunner.Default.Game;

        // Quantum Event 구독
        QuantumEvent.Subscribe<EventChampionSelectedEvent>(this, OnQuantumChampionSelected);
        QuantumEvent.Subscribe<EventTurnChangedEvent>(this, OnQuantumTurnChanged);

        // Quantum Input Callback 등록
        QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));

        _quantumInitialized = true;
        Debug.Log("✅ CharacterSelectUI - Quantum 연동 완료!");
    }

    void OnDestroy()
    {
        QuantumEvent.UnsubscribeListener(this);
        QuantumCallback.UnsubscribeListener(this);
    }

    /// <summary>
    /// Quantum Input Callback - 매 프레임 호출
    /// </summary>
    void PollInput(CallbackPollInput callback)
    {
        Quantum.Input input = new Quantum.Input();

        if (_pendingChampionSelection > 0)
        {
            input.SelectedChampionId = _pendingChampionSelection;
            Debug.Log($"📤 Sending Input: SelectedChampionId={_pendingChampionSelection}");
            _pendingChampionSelection = 0;
        }

        callback.SetInput(input, DeterministicInputFlags.Repeatable);
    }

    /// <summary>
    /// Quantum 챔피언 선택 이벤트 콜백
    /// </summary>
    void OnQuantumChampionSelected(EventChampionSelectedEvent e)
    {
        // 이벤트에서 직접 PlayerIndex 사용 (Quantum에서 계산된 값)
        int playerIndex = e.PlayerIndex;
        int championId = e.ChampionId;

        Debug.Log($"🎯 Quantum ChampionSelected: Turn={e.Turn}, PlayerIndex={playerIndex}, ChampionId={championId}");

        // 카드 UI 업데이트 (색상 변경)
        if (championId >= 0 && championId < characterElements.Length)
        {
            if (characterElements[championId] != null)
            {
                characterElements[championId].SetSelectedState(true, playerIndex);
            }
        }

        // Side Panel 업데이트 (Quantum 데이터 기반)
        UpdateSidePanelFromQuantum(playerIndex, championId);
    }

    /// <summary>
    /// Quantum 데이터 기반으로 Side Panel 업데이트
    /// </summary>
    void UpdateSidePanelFromQuantum(int playerIndex, int championId)
    {
        GameObject[] slots = playerIndex == 0 ? playerASlots : playerBSlots;
        string playerName = playerIndex == 0 ? "Player A" : "Player B";

        Debug.Log($"📌 UpdateSidePanelFromQuantum: {playerName}, championId={championId}");

        // 슬롯 배열 유효성 검사
        if (slots == null || slots.Length == 0)
        {
            Debug.LogWarning($"⚠️ {playerName} slots 배열이 null이거나 비어있습니다!");
            return;
        }

        // 해당 플레이어의 선택 카운트 찾기 (간단히 빈 슬롯 찾기)
        int slotIndex = -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                Debug.LogWarning($"⚠️ {playerName} slots[{i}]가 null입니다!");
                continue;
            }

            Image iconImage = FindImageInChildren(slots[i], "Icon");
            if (iconImage == null)
            {
                // Icon을 못 찾으면 sprite가 null인지 확인
                Image directImage = slots[i].GetComponentInChildren<Image>();
                if (directImage != null && directImage.sprite == null)
                {
                    slotIndex = i;
                    Debug.Log($"   Slot[{i}]: 직접 Image 사용 (sprite=null)");
                    break;
                }
                Debug.LogWarning($"⚠️ {playerName} slots[{i}]에서 Icon을 찾을 수 없습니다!");
                continue;
            }

            Debug.Log($"   Slot[{i}]: Icon.enabled={iconImage.enabled}, sprite={iconImage.sprite?.name ?? "null"}");

            // 빈 슬롯 조건: enabled가 false이거나, sprite가 null
            if (!iconImage.enabled || iconImage.sprite == null)
            {
                slotIndex = i;
                break;
            }
        }

        if (slotIndex < 0)
        {
            Debug.LogWarning($"⚠️ {playerName}: 빈 슬롯을 찾을 수 없습니다! (모든 슬롯이 이미 사용됨)");
            return;
        }

        // 챔피언 데이터 가져오기
        ChampionData champion = championDB?.GetChampion(championId);
        if (champion == null)
        {
            Debug.LogWarning($"⚠️ ChampionData[{championId}]를 찾을 수 없습니다!");
            return;
        }

        // 아이콘 업데이트
        Image icon = FindImageInChildren(slots[slotIndex], "Icon");
        if (icon == null)
        {
            icon = slots[slotIndex].GetComponentInChildren<Image>();
        }

        if (icon != null && champion.icon != null)
        {
            icon.sprite = champion.icon;
            icon.enabled = true;
            icon.color = Color.white;
            Debug.Log($"✅ {playerName} Slot[{slotIndex}] 아이콘 설정: {champion.championName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ {playerName} Slot[{slotIndex}]: icon={icon != null}, champion.icon={champion.icon != null}");
        }

        // 이름 업데이트
        TextMeshProUGUI nameText = FindTextInChildren(slots[slotIndex], "Text_Name");
        if (nameText != null)
        {
            nameText.text = champion.championName;
        }

        Debug.Log($"📌 {playerName} Slot {slotIndex + 1}: {champion.championName}");
    }

    /// <summary>
    /// Quantum 턴 변경 이벤트 콜백
    /// </summary>
    void OnQuantumTurnChanged(EventTurnChangedEvent e)
    {
        Debug.Log($"🔄 Quantum TurnChanged: Turn={e.Turn}, CurrentPlayer={e.CurrentPlayer}");
        UpdateTurnDisplay(e.Turn, e.CurrentPlayer);
    }

    /// <summary>
    /// 챔피언 선택 요청 (UI에서 호출)
    /// </summary>
    public void RequestSelectChampion(int championIndex)
    {
        if (!_quantumInitialized)
        {
            Debug.LogWarning("⚠️ Quantum이 아직 초기화되지 않았습니다!");
            return;
        }

        Debug.Log($"🖱️ RequestSelectChampion: {championIndex}");
        _pendingChampionSelection = championIndex + 1;  // 0은 "선택 없음"
    }
    
    /// <summary>
    /// 12개 챔피언 카드 초기화 (BattleGameManager 연동)
    /// </summary>
    void InitializeChampions()
    {
        if (championDB == null)
        {
            Debug.LogError("ChampionDatabase가 할당되지 않았습니다!");
            return;
        }
        
        int championCount = championDB.GetChampionCount();
        Debug.Log($"📊 ChampionDatabase: {championCount}개 챔피언");
        
        for (int i = 0; i < characterElements.Length && i < championCount; i++)
        {
            if (characterElements[i] == null)
            {
                Debug.LogWarning($"CharacterElement[{i}]가 null입니다!");
                continue;
            }
            
            ChampionData data = championDB.GetChampion(i);
            if (data != null)
            {
                characterElements[i].InitializeWithChampionData(data, i);
            }
            else
            {
                Debug.LogWarning($"ChampionData[{i}]가 null입니다!");
            }
        }
        
        Debug.Log("✅ CharacterSelectUI 초기화 완료");
    }
    
    /// <summary>
    /// Turn 값에서 플레이어 인덱스 계산
    /// Turn 순서: A→B→A→B→A→B (번갈아가며)
    /// </summary>
    int GetPlayerIndexFromTurn(int turn)
    {
        // 홀수 턴 = Player A (0), 짝수 턴 = Player B (1)
        return (turn - 1) % 2;
    }

    /// <summary>
    /// 턴 표시 업데이트
    /// </summary>
    public void UpdateTurnDisplay(int turn, int currentPlayer)
    {
        string playerName = currentPlayer == 0 ? "Player A" : "Player B";

        if (gameText != null)
        {
            gameText.text = $"{playerName}의 차례 ({turn}/6)";
            gameText.color = currentPlayer == 0 ? playerAColor : playerBColor;
        }

        Debug.Log($"🎯 Turn {turn}: {playerName}");
    }
    
    /// <summary>
    /// 타이머 업데이트
    /// </summary>
    public void UpdateTimer(float time)
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(time);
            timerText.text = $"{seconds:D2}초";
            
            // 1초 이하면 빨간색 경고
            if (time <= 1f)
                timerText.color = Color.red;
            else if (time <= 2f)
                timerText.color = Color.yellow;
            else
                timerText.color = Color.white;
        }
    }
    
    /// <summary>
    /// 챔피언 선택 완료 시 호출 (네트워크 동기화)
    /// </summary>
    public void OnChampionSelected(int championIndex, int playerIndex)
    {
        // 카드 UI 업데이트
        if (championIndex >= 0 && championIndex < characterElements.Length)
        {
            if (characterElements[championIndex] != null)
            {
                characterElements[championIndex].SetSelectedState(true, playerIndex);
            }
        }
        
        // Side Panel 업데이트
        UpdateSidePanel(playerIndex);
    }
    
    /// <summary>
    /// 좌우 패널 업데이트 (선택된 챔피언 3개 표시)
    /// </summary>
    void UpdateSidePanel(int playerIndex)
    {
        if (BattleGameManager.Instance == null) return;
        
        PlayerGameData playerData = BattleGameManager.Instance.GetPlayerData(playerIndex);
        if (playerData == null) return;
        
        GameObject[] slots = playerIndex == 0 ? playerASlots : playerBSlots;
        List<ChampionData> selectedChampions = playerData.selectedChampions;
        
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            
            if (i < selectedChampions.Count)
            {
                ChampionData champion = selectedChampions[i];
                
                // 아이콘 업데이트
                Image iconImage = FindImageInChildren(slots[i], "Icon");
                if (iconImage != null && champion.icon != null)
                {
                    iconImage.sprite = champion.icon;
                    iconImage.enabled = true;
                    iconImage.color = Color.white;
                }
                
                // 이름 업데이트
                TextMeshProUGUI nameText = FindTextInChildren(slots[i], "Text_Name");
                if (nameText != null)
                {
                    nameText.text = champion.championName;
                }
                
                Debug.Log($"📌 Player {(playerIndex == 0 ? "A" : "B")} Slot {i + 1}: {champion.championName}");
            }
            else
            {
                // 빈 슬롯
                Image iconImage = FindImageInChildren(slots[i], "Icon");
                if (iconImage != null)
                {
                    iconImage.sprite = null;
                    iconImage.enabled = false;
                }
            }
        }
    }
    
    /// <summary>
    /// 자식 오브젝트에서 Image 찾기
    /// </summary>
    Image FindImageInChildren(GameObject parent, string childName)
    {
        Transform child = parent.transform.Find(childName);
        if (child != null)
            return child.GetComponent<Image>();
        
        // 재귀 탐색
        foreach (Transform t in parent.transform)
        {
            Image img = t.GetComponent<Image>();
            if (img != null && t.name == childName)
                return img;
        }
        
        return null;
    }
    
    /// <summary>
    /// 자식 오브젝트에서 TextMeshProUGUI 찾기
    /// </summary>
    TextMeshProUGUI FindTextInChildren(GameObject parent, string childName)
    {
        Transform child = parent.transform.Find(childName);
        if (child != null)
            return child.GetComponent<TextMeshProUGUI>();
        
        // 재귀 탐색
        foreach (Transform t in parent.transform)
        {
            TextMeshProUGUI text = t.GetComponent<TextMeshProUGUI>();
            if (text != null && t.name == childName)
                return text;
        }
        
        return null;
    }
    
    /// <summary>
    /// 에디터에서 Grid 하위 CharacterElement 자동 연결
    /// </summary>
#if UNITY_EDITOR
    [ContextMenu("Auto Find Character Elements")]
    void AutoFindCharacterElements()
    {
        // Grid 찾기
        Transform grid = transform.Find("Panels/Grid");
        if (grid == null)
        {
            Debug.LogWarning("Panels/Grid를 찾을 수 없습니다!");
            return;
        }
        
        List<CharacterElement> elements = new List<CharacterElement>();
        
        foreach (Transform child in grid)
        {
            CharacterElement element = child.GetComponent<CharacterElement>();
            if (element != null)
            {
                elements.Add(element);
            }
        }
        
        if (elements.Count > 0)
        {
            characterElements = elements.ToArray();
            Debug.Log($"✅ {elements.Count}개 CharacterElement 자동 연결 완료!");
        }
        else
        {
            Debug.LogWarning("CharacterElement를 찾을 수 없습니다!");
        }
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
#endif
}
