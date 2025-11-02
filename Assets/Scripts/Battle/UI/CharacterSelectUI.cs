using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CharacterSelectUI : MonoBehaviour
{
    public static CharacterSelectUI Instance;
    
    [Header("Data")]
    public ChampionDatabase championDB;
    
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
    /// 턴 표시 업데이트
    /// </summary>
    public void UpdateTurnDisplay(int turn)
    {
        int currentPlayer = turn % 2;
        string playerName = currentPlayer == 0 ? "Player A" : "Player B";
        int turnNumber = turn + 1;
        
        if (gameText != null)
        {
            gameText.text = $"{playerName}의 차례 ({turnNumber}/6)";
            gameText.color = currentPlayer == 0 ? playerAColor : playerBColor;
        }
        
        Debug.Log($"🎯 Turn {turnNumber}: {playerName}");
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
